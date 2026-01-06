using System;
using System.Threading.Tasks;
using MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadRemoteCatalogPkgPipeline : ILoadTextAssetPipeline
	{
		public string RemoteCatalogUri;
		public string ExternalCatalogUri;
		public bool Overwrite;
		protected ulong PredictFileSize;
		protected string RemoteCatalogHash;

		public LoadRemoteCatalogPkgPipeline Init(string remoteCatalogUri, string externalCatalogUri, string remoteHash,
			bool overwrite,
			ulong predictFileSize)
		{
			this.RemoteCatalogUri = remoteCatalogUri;
			this.ExternalCatalogUri = externalCatalogUri;
			this.RemoteCatalogHash = remoteHash;
			this.Overwrite = overwrite;
			this.PredictFileSize = predictFileSize;
			this.Result = new();
			this.Build();
			return this;
		}

		public void Dispose()
		{
			Reset();
		}

		private void Reset()
		{
			Text = null;
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
				using var downloadPipeline =
					new DownloadPipeline().Init(RemoteCatalogUri, ExternalCatalogUri, Overwrite, PredictFileSize);
				Result = await downloadPipeline.Run();
				isCached = false;
			}

			if (isCached || Result.IsOk)
			{
				using var loadPipeline = new LoadCatalogPkgPipeline().Init(ExternalCatalogUri, RemoteCatalogHash);
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

		public void Invalidate()
		{
			Reset();
			Build();
		}

		public string Text { get; set; }
	}
}