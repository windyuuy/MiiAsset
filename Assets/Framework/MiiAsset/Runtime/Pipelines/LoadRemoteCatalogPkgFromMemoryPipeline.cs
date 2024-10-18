using System.Threading.Tasks;
using MiiAsset.Runtime.IOStreams;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadRemoteCatalogPkgFromMemoryPipeline : ILoadTextAssetPipeline
	{
		public string RemoteCatalogUri;

		public LoadRemoteCatalogPkgFromMemoryPipeline Init(string remoteCatalogUri)
		{
			this.RemoteCatalogUri = remoteCatalogUri;
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
			using var downloadPipeline = new WebDownloadToMemoryPipeline().Init(RemoteCatalogUri);
			Result = await downloadPipeline.Run();

			if (Result.IsOk)
			{
				using var loadPipeline = new LoadCatalogPkgFromMemoryPipeline().Init(downloadPipeline.Bytes);
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
			return false;
		}

		public PipelineProgress GetProgress()
		{
			return new PipelineProgress().Set01Progress(Result.IsOk);
		}

		public string Text { get; set; }
	}
}