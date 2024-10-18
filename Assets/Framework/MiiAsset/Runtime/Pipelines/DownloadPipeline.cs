using System;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using Framework.MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class DownloadPipeline : IDownloadPipeline
	{
		protected IPumpStream DownloadStream;
		protected IWriteStream WriteStream;

		protected string Uri;
		protected string WriteUri;
		public bool Overwrite;

		protected bool UseCache = false;

		public DownloadPipeline Init(string uri, string writeUri, bool overwrite)
		{
			Debug.Assert(writeUri != null, "writeUri!=null");
			Uri = uri;
			WriteUri = writeUri;
			Overwrite = overwrite;
			this.Build();
			return this;
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
			if (Overwrite || !IOManager.LocalIOProto.Exists(WriteUri))
			{
				DownloadStream = new WebDownloadPumpStream().Init(Uri);
				WriteStream = new WriteFileStream().Init(WriteUri);
				DownloadStream.BindReadStream(WriteStream);
			}
			else
			{
				UseCache = true;
				Result = new PipelineResult
				{
					IsOk = true,
					Status = PipelineStatus.Done,
				};
			}
		}

		public async Task<PipelineResult> Run()
		{
			if (Result is not { Status: PipelineStatus.Done })
			{
				var result = await DownloadStream.Start();
				if (result.IsOk)
				{
					if (Result is not { Status: PipelineStatus.Done })
					{
						Result = await WriteStream.WaitDone();
						UpdateProgress();
					}
				}
				else
				{
					Result = result;
				}
			}
			else
			{
				Progress = new PipelineProgress().SetDownloadedProgress(true);
			}

			return Result;
		}

		private void UpdateProgress()
		{
			Progress = this.GetProgress();
		}

		public bool IsCached()
		{
			return UseCache;
		}

		protected PipelineProgress Progress = new PipelineProgress();

		public PipelineProgress GetProgress()
		{
			if (DownloadStream != null && WriteStream != null)
			{
				return DownloadStream.GetProgress().Combine(new PipelineProgress().SetDownloadedProgress(Result?.IsOk ?? false));
			}
			else
			{
				return Progress;
			}
		}

		public void PresetDownloadSize(long fileSize)
		{
			if (DownloadStream != null)
			{
				DownloadStream.PresetDownloadSize(fileSize);
			}
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
				WriteStream = null;
			}
		}
	}
}