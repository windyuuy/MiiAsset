using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using Lang.Encoding;
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
			IsHashLoaded = false;
			this.Build();
			return this;
		}

		protected bool IsHashLoaded;
		protected IPipeline LoadInternalCatalogPipeline;
		protected IPipeline LoadExternalCatalogPipeline;

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
			using ILoadTextAssetPipeline loadInternalHashPipeline = IOManager.LocalIOProto.IsWebUri(internalCatalogUri)
				? new LoadRemoteTextFilePipeline().Init(ToHashFileName(internalCatalogUri), null)
				: new LoadTextFilePipeline().Init(ToHashFileName(internalCatalogUri));
			;
			using var loadRemoteHashPipeline = new LoadRemoteTextFilePipeline().Init(ToHashFileName(remoteCatalogUri), null);

			using ILoadTextAssetPipeline loadInternalCatalogPipeline = IOManager.LocalIOProto.IsWebUri(internalCatalogUri)
				? new LoadRemoteCatalogPkgFromMemoryPipeline().Init(internalCatalogUri)
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

			LoadInternalCatalogPipeline = loadInternalCatalogPipeline;
			var loadInternalCatalogTask = loadInternalCatalogPipeline.Run();

			await loadHashPipelinesTask;

			IsHashLoaded = true;

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
			sourceUri = RemoteBaseUri;
			var remoteHash = loadRemoteHashPipeline.Text;
			var needUpdateCatalog = false;
			if ((loadExternalHashPipeline == null && loadInternalHashPipeline.Text != remoteHash) ||
			    (loadExternalHashPipeline != null && loadExternalHashPipeline.Text != remoteHash))
			{
				// load from remote
				needUpdateCatalog = true;
				loadExternalCatalogPipeline = new LoadRemoteCatalogPkgPipeline().Init(remoteCatalogUri, externalCatalogUri, true);
			}
			else if (loadExternalHashPipeline != null && loadExternalHashPipeline.Text == remoteHash)
			{
				// load from cache
				loadExternalCatalogPipeline = new LoadCatalogPkgPipeline().Init(externalCatalogUri);
			}
			else
			{
				// load from internal
				loadExternalCatalogPipeline = null;
			}

			LoadExternalCatalogPipeline = loadExternalCatalogPipeline;

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

			// update hash file
			if (needUpdateCatalog)
			{
				_ = IOManager.LocalIOProto.WriteAllTextAsync(externalHashUri, remoteHash, EncodingExt.UTF8WithoutBom);
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
			var hashFileName = internalCatalogUri.Replace(".json", ".hash").Replace(".zip", ".hash");
			Debug.Log($"ToHashFileName: {internalCatalogUri}->{hashFileName}");
			return hashFileName;
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

		public PipelineProgress GetProgress()
		{
			if (LoadInternalCatalogPipeline == null || LoadExternalCatalogPipeline == null)
			{
				var pipelineProgress = new PipelineProgress().Set01Progress(false);
				if (IsHashLoaded)
				{
					pipelineProgress.Count = 1;
				}

				return pipelineProgress;
			}
			else
			{
				return LoadInternalCatalogPipeline.CombineProgress(LoadExternalCatalogPipeline);
			}
		}
	}
}