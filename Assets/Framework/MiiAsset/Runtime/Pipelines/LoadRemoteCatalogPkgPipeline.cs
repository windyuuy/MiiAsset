using System;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadRemoteCatalogPkgPipeline : ILoadTextAssetPipeline
	{
		public string RemoteCatalogUri;
		public string ExternalCatalogUri;
		public bool Overwrite;

		public LoadRemoteCatalogPkgPipeline Init(string remoteCatalogUri, string externalCatalogUri, bool overwrite)
		{
			this.RemoteCatalogUri = remoteCatalogUri;
			this.ExternalCatalogUri = externalCatalogUri;
			this.Overwrite = overwrite;
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
			if (!IsCached() && ExternalCatalogUri != null)
			{
				using var downloadPipeline = new DownloadPipeline().Init(RemoteCatalogUri, ExternalCatalogUri, Overwrite);
				Result = await downloadPipeline.Run();
				isCached = false;
			}

			if (isCached || Result.IsOk)
			{
				using var loadPipeline = new LoadCatalogPkgPipeline().Init(ExternalCatalogUri);
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
			return !this.Overwrite && IOManager.LocalIOProto.Exists(ExternalCatalogUri);
		}

		public PipelineProgress GetProgress()
		{
			return new PipelineProgress().Set01Progress(Result.IsOk);
		}

		public string Text { get; set; }
	}
}