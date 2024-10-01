using System;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Framework.MiiAsset.Runtime.IOStreams
{
	public class DownloadHandlerNotify : DownloadHandlerScript
	{
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
			base.ReceiveContentLengthHeader(contentLength);
		}
	}

	public class WebDownloadPumpStream : IPumpStream
	{
		protected UnityWebRequest Uwr;
		protected DownloadHandlerNotify DownloadHandler;
		protected TaskCompletionSource<bool> Ts;

		protected string Uri;

		public WebDownloadPumpStream Init(string uri)
		{
			Uri = uri;
			DownloadHandler = new();
			return this;
		}

		public Task Start()
		{
			if (Ts == null)
			{
				async Task ReadInternal()
				{
					Ts = new();
					Uwr = new UnityWebRequest(this.Uri);
					Uwr.downloadHandler = DownloadHandler;

					OnCtrl?.Invoke(new StreamCtrlEvent()
					{
						Event = StreamEvent.Begin,
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
					DownloadHandler.OnCtrl?.Invoke(evt);
					Ts.SetResult(evt.IsOk);
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