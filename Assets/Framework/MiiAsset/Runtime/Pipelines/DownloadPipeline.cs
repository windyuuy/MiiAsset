using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using Framework.MiiAsset.Runtime.IOStreams;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class DownloadPipeline : IPipeline
	{
		protected IPumpStream DownloadStream;
		protected IWriteStream WriteStream;

		protected string Uri;
		protected string WriteUri;

		public DownloadPipeline Init(string uri, string writeUri)
		{
			Uri = uri;
			WriteUri = writeUri;
			this.Build();
			return this;
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
			if (!IOManager.LocalIOProto.Exists(WriteUri))
			{
				WriteStream = new WriteFileStream().Init(WriteUri);
				DownloadStream = new WebDownloadPumpStream().Init(Uri);
				DownloadStream.BindReadStream(WriteStream);
			}
		}

		public async Task<PipelineResult> Run()
		{
			Result = await DownloadStream.Start();
			if (Result.IsOk)
			{
				Result = await WriteStream.WaitDone();
			}

			return Result;
		}

		public bool IsCached()
		{
			return false;
		}

		public void Dispose()
		{
			if (DownloadStream != null)
			{
				this.DownloadStream.UnBindReadStream(WriteStream);
				this.DownloadStream.Dispose();
				this.DownloadStream = null;
			}

			if (WriteStream != null)
			{
				WriteStream.Dispose();
				this.WriteStream = null;
			}
		}
	}
}