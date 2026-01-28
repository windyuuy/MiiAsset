using System;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOManagers;
using MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class DownloadPipeline : IDownloadPipeline
	{
		protected WebDownloadPumpStream DownloadStream;
		protected WriteFileStream WriteStream;

		protected string Uri;
		protected string WriteUri;
		public bool Overwrite;

		protected bool UseCache = false;
		protected ulong PredictFileSize;

		public DownloadPipeline Init(string uri, string writeUri, bool overwrite, ulong predictFileSize)
		{
			Debug.Assert(writeUri != null, "writeUri!=null");
			Uri = uri;
			WriteUri = writeUri;
			Overwrite = overwrite;
			PredictFileSize = predictFileSize;
			this.Build();
			return this;
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
			if (Overwrite || !IOManager.LocalIOProto.Exists(WriteUri))
			{
				Result = new PipelineResult
				{
					IsOk = false,
					Status = PipelineStatus.Init,
					Uri = Uri,
				};
				DownloadStream = new WebDownloadPumpStream().Init(Uri, PredictFileSize);
				WriteStream = new WriteFileStream().Init(WriteUri, PredictFileSize);
				DownloadStream.BindReadStream(WriteStream);
			}
			else
			{
				UseCache = true;
				Result = new PipelineResult
				{
					IsOk = true,
					Status = PipelineStatus.Done,
					Uri = Uri,
				};
			}
		}

		public async Task<PipelineResult> Run()
		{
			if (Result is not { Status: PipelineStatus.Done } || !Result.IsOk)
			{
				WriteStream.Start();
				PipelineResult result;
				try
				{
					result = await DownloadStream.Start();
				}
				catch (OperationCanceledException cancelException)
				{
					MyLogger.LogException(cancelException, "e35");
					result = new PipelineResult
					{
						IsOk = false,
						Exception = cancelException,
						Code = -1,
						Msg = "operation-cancelled",
						Uri = Uri,
						ErrorType = PipelineErrorType.OperationCancelled,
						Status = PipelineStatus.Done,
					};
				}

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
				return DownloadStream.GetProgress()
					.Combine(new PipelineProgress().SetDownloadedProgress(Result?.IsOk ?? false));
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

		public void Invalidate()
		{
			this.Reset();
			AssetBundlePipelineHelper.InvalidateLocalFile(WriteUri);
			this.Build();
		}

		public void Dispose()
		{
			Reset();
		}

		private void Reset()
		{
			UseCache = false;

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