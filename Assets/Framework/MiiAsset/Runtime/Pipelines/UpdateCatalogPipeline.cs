using System;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class UpdateCatalogPipeline : IPipeline
	{
		public string CatalogName;
		public string InternalBaseUri;
		public string ExternalBaseUri;
		public string RemoteBaseUri;

		public UpdateCatalogPipeline Init(string catalogName, string internalBaseUri, string externalBaseUri, string remoteBaseUri)
		{
			CatalogName = catalogName;
			InternalBaseUri = internalBaseUri;
			ExternalBaseUri = externalBaseUri;
			RemoteBaseUri = remoteBaseUri;
			this.Build();
			return this;
		}

		public CatalogConfig InternalCatalog;
		public CatalogConfig ExternalCatalog;
		public string SourceUri;

		protected Task<PipelineResult> LoadCatalogTask;

		async Task<PipelineResult> LoadCatalogTaskInternal()
		{
			var catalogName = CatalogName;
			var internalCatalogUri = InternalBaseUri + catalogName;
			var remoteCatalogUri = RemoteBaseUri + catalogName;
			var externalCatalogUri = ExternalBaseUri + catalogName;
			var externalHashUri = ToHashFileName(externalCatalogUri);
			using ILoadTextAssetPipeline loadInternalHashPipeline = AssetBundlePipelineHelper.IsWebUri(internalCatalogUri)
				? new LoadRemoteTextFilePipeline().Init(ToHashFileName(internalCatalogUri), null)
				: new LoadTextFilePipeline().Init(ToHashFileName(internalCatalogUri));
			;
			using var loadRemoteHashPipeline = new LoadRemoteTextFilePipeline().Init(ToHashFileName(remoteCatalogUri), externalHashUri);

			using ILoadTextAssetPipeline loadInternalCatalogPipeline = AssetBundlePipelineHelper.IsWebUri(internalCatalogUri)
				? new LoadRemoteCatalogPkgPipeline().Init(internalCatalogUri, null)
				: new LoadCatalogPkgPipeline().Init(internalCatalogUri);

			LoadTextFilePipeline loadExternalHashPipeline = null;
			Task loadHashPipelinesTask;
			if (IOManager.LocalIOProto.Exists(externalHashUri) && IOManager.LocalIOProto.Exists(externalCatalogUri))
			{
				loadExternalHashPipeline = new LoadTextFilePipeline().Init(externalHashUri);
				loadHashPipelinesTask = Task.WhenAll(
					loadInternalHashPipeline.Run(),
					loadExternalHashPipeline.Run(),
					loadRemoteHashPipeline.Run()
				);
			}
			else
			{
				loadHashPipelinesTask = Task.WhenAll(
					loadInternalHashPipeline.Run(),
					loadRemoteHashPipeline.Run()
				);
			}

			var loadInternalCatalogTask = loadInternalCatalogPipeline.Run();

			await loadHashPipelinesTask;

			if (loadInternalHashPipeline.Result.IsOk == false)
			{
				Result = loadInternalHashPipeline.Result;
				return Result;
			}

			if (loadRemoteHashPipeline.Result.IsOk == false)
			{
				Result = loadRemoteHashPipeline.Result;
				return Result;
			}

			if (loadExternalHashPipeline != null && loadExternalHashPipeline.Result.IsOk == false)
			{
				Result = loadExternalHashPipeline.Result;
				return Result;
			}

			ILoadTextAssetPipeline loadExternalCatalogPipeline;
			string sourceUri;
			if ((loadExternalHashPipeline == null && loadInternalHashPipeline.Text != loadRemoteHashPipeline.Text) ||
			    (loadExternalHashPipeline != null && loadExternalHashPipeline.Text != loadRemoteHashPipeline.Text))
			{
				// load from remote
				sourceUri = RemoteBaseUri;
				loadExternalCatalogPipeline = new LoadRemoteCatalogPkgPipeline().Init(remoteCatalogUri, externalCatalogUri);
			}
			else if (loadExternalHashPipeline != null && loadExternalHashPipeline.Text == loadRemoteHashPipeline.Text)
			{
				// load from cache
				sourceUri = ExternalBaseUri;
				loadExternalCatalogPipeline = new LoadCatalogPkgPipeline().Init(externalCatalogUri);
			}
			else
			{
				// load from internal
				sourceUri = InternalBaseUri;
				loadExternalCatalogPipeline = null;
			}

			if (loadExternalCatalogPipeline != null)
			{
				await Task.WhenAll(loadInternalCatalogTask, loadExternalCatalogPipeline.Run());
			}
			else
			{
				await loadInternalCatalogTask;
			}

			if (loadInternalCatalogPipeline.Result.IsOk == false)
			{
				Result = loadInternalCatalogPipeline.Result;
				return Result;
			}

			if (loadExternalCatalogPipeline != null && loadExternalCatalogPipeline.Result.IsOk == false)
			{
				Result = loadExternalCatalogPipeline.Result;
				return Result;
			}

			CatalogConfig internalCatalog;
			try
			{
				internalCatalog = JsonUtility.FromJson<CatalogConfig>(loadInternalCatalogPipeline.Text);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				Result = new()
				{
					Exception = exception,
					Msg = "invalid json format",
					ErrorType = PipelineErrorType.DataIncorrect,
				};
				return Result;
			}

			CatalogConfig externalCatalog;
			if (loadExternalCatalogPipeline != null)
			{
				try
				{
					externalCatalog = JsonUtility.FromJson<CatalogConfig>(loadExternalCatalogPipeline.Text);
				}
				catch (Exception exception)
				{
					Result = new()
					{
						Exception = exception,
						Msg = "invalid json format",
						ErrorType = PipelineErrorType.DataIncorrect,
					};
					return Result;
				}
			}
			else
			{
				externalCatalog = null;
			}

			this.HandleCatalog(internalCatalog, externalCatalog, sourceUri);

			loadExternalHashPipeline?.Dispose();
			loadExternalCatalogPipeline?.Dispose();

			Result = new()
			{
				IsOk = true,
			};
			return Result;
		}

		public Task<PipelineResult> UpdateCatalog(string remoteBaseUri, string catalogName)
		{
			if (LoadCatalogTask == null)
			{
				this.RemoteBaseUri = remoteBaseUri;
				this.CatalogName = catalogName;

				LoadCatalogTask = LoadCatalogTaskInternal();
			}

			return LoadCatalogTask;
		}

		private static string ToHashFileName(string internalCatalogUri)
		{
			return internalCatalogUri.Replace(".json", ".hash").Replace(".zip", ".hash");
		}

		private void HandleCatalog(CatalogConfig internalCatalog, CatalogConfig externalCatalog, string sourceUri)
		{
			this.InternalCatalog = internalCatalog;
			this.ExternalCatalog = externalCatalog;
			this.SourceUri = sourceUri;
		}

		public void Dispose()
		{
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
		}

		public async Task<PipelineResult> Run()
		{
			var updateCatalog = this.UpdateCatalog(RemoteBaseUri, CatalogName);
			return await updateCatalog;
		}

		public bool IsCached()
		{
			return false;
		}
	}
}