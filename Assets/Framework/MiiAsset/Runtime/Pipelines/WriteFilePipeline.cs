using System;
using System.IO;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class WriteFilePipeline : IPipeline
	{
		// protected IPumpStream ReadStream;
		protected string Uri;
		protected byte[] Bytes;
		protected Task<PipelineResult> Task;

		public WriteFilePipeline Init(string uri, byte[] bytes)
		{
			this.Uri = uri;
			Bytes = bytes;
			this.Result = new();
			return this;
		}

		public void Dispose()
		{
			
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
			
		}

		public Task<PipelineResult> Run()
		{
			if (Task == null)
			{
				Task = RunInternal();
			}

			return Task;
		}

		private async Task<PipelineResult> RunInternal()
		{
			Result.Status = PipelineStatus.Running;
			try
			{
				await IOManager.LocalIOProto.WriteAllBytesAsync(this.Uri, this.Bytes);
				Result.IsOk = true;
			}
			catch (Exception exception)
			{
				Result.Exception = exception;
			}

			Result.Status = PipelineStatus.Done;
			return Result;
		}

		public bool IsCached()
		{
			return IOManager.LocalIOProto.Exists(Uri);
		}

		public PipelineProgress GetProgress()
		{
			return new PipelineProgress().SetDownloadedProgress(Result.IsOk);
		}
	}
}