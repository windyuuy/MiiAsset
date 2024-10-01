using System.Threading.Tasks;
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

		public void Build()
		{
			DownloadStream = new WebDownloadPumpStream().Init(Uri);
			WriteStream = new WriteFileStream().Init(WriteUri);
			DownloadStream.BindReadStream(WriteStream);
		}

		public Task Run()
		{
			if (WriteStream is ICacheableStream cacheableStream && cacheableStream.Exist())
			{
				return Task.CompletedTask;
			}
			else
			{
				DownloadStream.Start();
				return WriteStream.WaitDone();
			}
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