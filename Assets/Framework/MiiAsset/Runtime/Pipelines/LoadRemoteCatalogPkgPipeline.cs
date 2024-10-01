using System;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadRemoteCatalogPkgPipeline : ILoadTextAssetPipeline
	{
		public string RemoteCatalogUri;
		public string InternalCatalogUri;

		public LoadRemoteCatalogPkgPipeline Init(string remoteCatalogUri, string internalCatalogUri)
		{
			this.RemoteCatalogUri = remoteCatalogUri;
			this.InternalCatalogUri = internalCatalogUri;
			this.Build();
			return this;
		}

		public void Dispose()
		{
		}

		public void Build()
		{
		}

		public async Task Run()
		{
			if (!IsCached() && InternalCatalogUri != null)
			{
				using var downloadPipeline = new DownloadPipeline().Init(RemoteCatalogUri, InternalCatalogUri);
				var task = downloadPipeline.Run();
				await task;
			}

			using var loadPipeline = new LoadCatalogPkgPipeline().Init(InternalCatalogUri);
			await loadPipeline.Run();
			Text = loadPipeline.Text;
		}

		public bool IsCached()
		{
			return IOManager.LocalIOProto.Exists(InternalCatalogUri);
		}

		public string Text { get; set; }
	}
}