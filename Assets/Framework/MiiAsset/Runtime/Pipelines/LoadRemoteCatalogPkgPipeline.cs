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
			this.Result = new();
			this.Build();
			return this;
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
			var isCached = true;
			if (!IsCached() && InternalCatalogUri != null)
			{
				using var downloadPipeline = new DownloadPipeline().Init(RemoteCatalogUri, InternalCatalogUri);
				Result = await downloadPipeline.Run();
				isCached = false;
			}

			if (isCached || Result.IsOk)
			{
				using var loadPipeline = new LoadCatalogPkgPipeline().Init(InternalCatalogUri);
				Result = await loadPipeline.Run();
				Text = loadPipeline.Text;

				if (string.IsNullOrWhiteSpace(Text))
				{
					Result.IsOk = false;
					Result.ErrorType = PipelineErrorType.DataIncorrect;
				}
			}

			return Result;
		}

		public bool IsCached()
		{
			return IOManager.LocalIOProto.Exists(InternalCatalogUri);
		}

		public string Text { get; set; }
	}
}