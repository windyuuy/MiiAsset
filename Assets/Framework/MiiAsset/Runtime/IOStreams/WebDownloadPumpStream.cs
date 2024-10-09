using System;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Framework.MiiAsset.Runtime.IOStreams
{
	public class DownloadHandlerNotify : DownloadHandlerScript
	{
		public ulong TotalBytes = int.MaxValue;
		public Func<byte[], int, int, int> OnReceivedData { get; set; }

		public Action<StreamCtrlEvent> OnCtrl { get; set; }

		protected override bool ReceiveData(byte[] data0, int dataLength)
		{
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
				IsOk = true,
				Capability = (int)contentLength,
			});
			TotalBytes = contentLength;
			base.ReceiveContentLengthHeader(contentLength);
		}
	}

	public class WebDownloadPumpStream : IPumpStream
	{
		protected UnityWebRequest Uwr;
		protected DownloadHandlerNotify DownloadHandler;
		protected TaskCompletionSource<PipelineResult> Ts;

		protected string Uri;
		public PipelineResult Result;

		public WebDownloadPumpStream Init(string uri)
		{
			Uri = uri;
			DownloadHandler = new();
			Result = new();
			return this;
		}

		public Task<PipelineResult> Start()
		{
			if (Ts == null)
			{
				async Task ReadInternal()
				{
					Result.Status = PipelineStatus.Running;
					Ts = new();
					Uwr = new UnityWebRequest(this.Uri);
					Uwr.downloadHandler = DownloadHandler;

					OnCtrl?.Invoke(new StreamCtrlEvent()
					{
						Event = StreamEvent.Begin,
						PumpStream = this,
					});

					var op = Uwr.SendWebRequest();
					await op.GetTask();
					var code = Uwr.responseCode;
					var msg = Uwr.error;

					var evt = new StreamCtrlEvent()
					{
						Event = StreamEvent.End,
						Code = code,
						Msg = msg,
						IsOk = code == 200,
					};
					Result.Code = (int)code;
					Result.Msg = msg;
					Result.IsOk = evt.IsOk;
					if (!Result.IsOk)
					{
						Result.ErrorType = PipelineErrorType.NetError;
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
			if (Uwr != null)
			{
				Uwr.Abort();
			}
		}

		public Func<byte[], int, int, int> OnReceivedData
		{
			get { return DownloadHandler.OnReceivedData; }
			set { DownloadHandler.OnReceivedData = value; }
		}

		public Action<StreamCtrlEvent> OnCtrl
		{
			get { return DownloadHandler.OnCtrl; }
			set { DownloadHandler.OnCtrl = value; }
		}

		protected PipelineProgress Progress = new PipelineProgress();

		public PipelineProgress GetProgress()
		{
			if (Result.Status == PipelineStatus.Init)
			{
				Progress.Set01Progress(false);
			}
			else if (Result.Status == PipelineStatus.Done)
			{
				Progress.Complete();
			}
			else
			{
				UpdateProgress();
			}

			return Progress;
		}

		private void UpdateProgress()
		{
			Progress = new()
			{
				Total = DownloadHandler.TotalBytes,
				Count = Uwr.downloadedBytes,
			};
		}

		public void Dispose()
		{
			if (this.DownloadHandler != null)
			{
				this.DownloadHandler.Dispose();
				this.DownloadHandler.OnReceivedData = null;
				this.DownloadHandler.OnCtrl = null;
			}

			if (this.Uwr != null)
			{
				this.Uwr.Dispose();
			}

			Ts = null;
		}
	}
}