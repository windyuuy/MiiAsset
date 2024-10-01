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

		protected TaskCompletionSource<bool> LoadCatalogTask;

		public Task UpdateCatalog(string remoteBaseUri, string catalogName)
		{
			if (LoadCatalogTask == null)
			{
				this.RemoteBaseUri = remoteBaseUri;
				this.CatalogName = catalogName;

				this.LoadCatalogTask = new();

				async Task LoadCatalogTask()
				{
					var internalCatalogUri = InternalBaseUri + catalogName;
					var remoteCatalogUri = RemoteBaseUri + catalogName;
					var externalCatalogUri = ExternalBaseUri + catalogName;
					var externalHashUri = ToHashFileName(externalCatalogUri);
					using ILoadTextAssetPipeline loadInternalHashPipeline = AssetBundlePipelineHelper.IsWebUri(internalCatalogUri)
						? new LoadRemoteTextFilePipeline().Init(ToHashFileName(internalCatalogUri), null)
						: new LoadTextFilePipeline().Init(ToHashFileName(internalCatalogUri));
					;
					using var loadRemoteHashPipeline = new LoadRemoteTextFilePipeline().Init(ToHashFileName(remoteCatalogUri), externalHashUri);

					using ILoadTextAssetPipeline loadInternalCatalogPipeline = AssetBundlePipelineHelper.IsWebUri(externalCatalogUri)
						? new LoadRemoteCatalogPkgPipeline().Init(remoteCatalogUri, null)
						: new LoadCatalogPkgPipeline().Init(internalCatalogUri);

					LoadTextFilePipeline loadExternalHashPipeline = null;
					Task loadHashPipelines;
					if (IOManager.LocalIOProto.Exists(externalHashUri) && IOManager.LocalIOProto.Exists(externalCatalogUri))
					{
						loadExternalHashPipeline = new LoadTextFilePipeline().Init(externalHashUri);
						loadHashPipelines = Task.WhenAll(
							loadInternalHashPipeline.Run(),
							loadExternalHashPipeline.Run(),
							loadRemoteHashPipeline.Run()
						);
					}
					else
					{
						loadHashPipelines = Task.WhenAll(
							loadInternalHashPipeline.Run(),
							loadRemoteHashPipeline.Run()
						);
					}

					var loadInternalCatalogTask = loadInternalCatalogPipeline.Run();

					await loadHashPipelines;

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

					CatalogConfig internalCatalog = JsonUtility.FromJson<CatalogConfig>(loadInternalCatalogPipeline.Text);
					CatalogConfig externalCatalog;
					if (loadExternalCatalogPipeline != null)
					{
						externalCatalog = JsonUtility.FromJson<CatalogConfig>(loadExternalCatalogPipeline.Text);
					}
					else
					{
						externalCatalog = null;
					}

					this.HandleCatalog(internalCatalog, externalCatalog, sourceUri);

					loadExternalHashPipeline?.Dispose();
					loadExternalCatalogPipeline?.Dispose();
					this.LoadCatalogTask.SetResult(true);
				}

				_ = LoadCatalogTask();
			}

			return LoadCatalogTask.Task;
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

		public void Build()
		{
		}

		public async Task Run()
		{
			var updateCatalog = this.UpdateCatalog(RemoteBaseUri, CatalogName);
			await updateCatalog;
		}

		public bool IsCached()
		{
			return false;
		}
	}
}