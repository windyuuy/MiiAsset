using System;
using System.Net;
using UnityEngine.Networking;

namespace MiiAsset.Runtime.IOStreams
{
	public class DownloadHandlerNotify : DownloadHandlerScript
	{
		public ulong TotalBytes = 0;
		protected bool IsTotalBytesUnkown = true;
		public string Uri;
		protected string UnescapeUri => WebUtility.UrlDecode(Uri);

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
}