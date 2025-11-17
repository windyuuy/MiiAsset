using System;
using System.Threading.Tasks;
using System.Web;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOManagers;
using MonoExtLib.AsyncExt;
using UnityEngine.Networking;

namespace MiiAsset.Runtime.IOStreams
{
	public class DownloadHandlerNotify : DownloadHandlerScript
	{
		public ulong TotalBytes = 0;
		protected bool IsTotalBytesUnkown = true;
		public string Uri;
		protected string UnescapeUri => HttpUtility.UrlDecode(Uri);

		public Func<byte[], int, int, int> OnReceivedData { get; set; }

		public Action<StreamCtrlEvent> OnCtrl { get; set; }

		protected override bool ReceiveData(byte[] data0, int dataLength)
		{
			if (IsTotalBytesUnkown)
			{
				TotalBytes += (ulong)data0.Length;
			}

			OnReceivedData(data0, 0, dataLength);
			return base.ReceiveData(data0, dataLength);
		}

		protected override void ReceiveContentLengthHeader(ulong contentLength)
		{
			OnCtrl(new()
			{
				Event = StreamEvent.Capability,
				Code = 0,
				Msg = null,
				SourceUri = Uri,
				IsOk = true,
				Capability = (int)contentLength,
			});
			TotalBytes = contentLength;
			IsTotalBytesUnkown = false;
			base.ReceiveContentLengthHeader(contentLength);
		}
	}

	public class WebDownloadPumpStream : IPumpStream
	{
		protected UnityWebRequest Uwr;
		protected DownloadHandlerNotify DownloadHandler;
		protected TaskCompletionSource<PipelineResult> Ts;

		protected string Uri;
		protected string UnescapeUri => HttpUtility.UrlDecode(Uri);
		public PipelineResult Result;
		protected ulong PredictFileSize;

		public WebDownloadPumpStream Init(string uri, ulong predictFileSize)
		{
			Uri = uri;
			PredictFileSize = predictFileSize;
			Result = new();
			return this;
		}

		public Task<PipelineResult> Start()
		{
			if (Ts == null || (Ts.Task.IsCompleted && !Result.IsOk))
			{
				async Task ReadInternal()
				{
					if (Uwr != null)
					{
						if (DownloadHandler != null)
						{
							DownloadHandler.Dispose();
							DownloadHandler = null;
						}

						Uwr.Dispose();
						Uwr = null;
					}

					{
						DownloadHandler = new();
						DownloadHandler.Uri = Uri;
						DownloadHandler.OnCtrl = OnCtrl;
						DownloadHandler.OnReceivedData = OnReceivedData;
					}
					Result.Status = PipelineStatus.Running;
					Ts = new();

					using (await BundleWebSemaphore.Wait())
					{
						Uwr = new UnityWebRequest(this.Uri);
						Uwr.downloadHandler = DownloadHandler;

						OnCtrl?.Invoke(new StreamCtrlEvent()
						{
							Event = StreamEvent.Begin,
							SourceUri = this.Uri,
							PumpStream = this,
						});

						IOManager.LocalIOProto.SetUwr(Uwr);
						var op = Uwr.SendWebRequest();
						await op.GetTask();
					}

					var code = (int)Uwr.responseCode;
					var uwrError = Uwr.error;
					var uwrResult = Uwr.result;

					var evt = new StreamCtrlEvent()
					{
						Event = StreamEvent.End,
						Code = code,
						Msg = uwrError,
						IsOk = uwrResult == UnityWebRequest.Result.Success,
						SourceUri = this.Uri,
						Capability = (int)DownloadHandler.TotalBytes,
					};

					Result.Code = (int)code;
					Result.Msg = uwrError;
					Result.IsOk = evt.IsOk;
					if (!Result.IsOk)
					{
						Result.ErrorType = PipelineErrorType.NetError;
						MyLogger.LogError($"download-failed: {code}, {uwrError}");
					}

					Result.Status = PipelineStatus.Done;

					DownloadHandler.OnCtrl?.Invoke(evt);
					Ts.SetResult(Result);
				}

				_ = ReadInternal();
			}

			return Ts.Task;
		}

		public void Abort()
		{
			MyLogger.LogError($"Abort-Uwr: {UnescapeUri}");
			if (Uwr != null)
			{
				Uwr.Abort();
			}
		}

		public Func<byte[], int, int, int> OnReceivedData { get; set; }

		public Action<StreamCtrlEvent> OnCtrl { get; set; }

		protected PipelineProgress Progress = new PipelineProgress();

		public PipelineProgress GetProgress()
		{
			if (Result.Status == PipelineStatus.Init)
			{
				Progress.SetTotal(PredictFileSize);
				Progress.SetProgress(0);
			}
			else if (Result.Status == PipelineStatus.Done)
			{
				UpdateProgress();
				Progress.Complete();
			}
			else
			{
				UpdateProgress();
			}

			return Progress;
		}

		public void PresetDownloadSize(long fileSize)
		{
			if (DownloadHandler != null)
			{
				DownloadHandler.TotalBytes = (ulong)fileSize;
			}
		}

		private void UpdateProgress()
		{
			var total = DownloadHandler != null ? DownloadHandler.TotalBytes : 0;
			if (total <= 0)
			{
				total = PredictFileSize;
			}

			Progress = new()
			{
				Total = total,
				Count = Uwr?.downloadedBytes ?? 0,
			};
		}

		public void Dispose()
		{
			if (this.DownloadHandler != null)
			{
				this.DownloadHandler.OnReceivedData = null;
				this.DownloadHandler.OnCtrl = null;
				this.DownloadHandler.Dispose();
				this.DownloadHandler = null;
			}

			if (this.Uwr != null)
			{
				this.Uwr.Dispose();
				this.Uwr = null;
			}

			this.OnCtrl = null;
			this.OnReceivedData = null;

			if (Ts != null)
			{
				if (!Ts.Task.IsCompleted)
				{
					Ts.SetException(
						new OperationCanceledException($"{nameof(WriteFileStream)} is disposed before await return"));
				}

				Ts = null;
			}
		}
	}
}