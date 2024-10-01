using System;
using System.IO;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;

namespace Framework.MiiAsset.Runtime.IOStreams
{
	public class WriteFileStream : IWriteStream, ICacheableStream
	{
		// protected IPumpStream ReadStream;
		protected string Uri;


		public WriteFileStream Init(string uri)
		{
			this.Uri = uri;
			IOManager.LocalIOProto.EnsureFileDirectory(uri);
			this.Ts = new();
			return this;
		}

		protected FileStream FileStream;
		protected TaskCompletionSource<bool> Ts;

		public int Write(byte[] data, int offset, int len)
		{
			FileStream.Write(data, offset, len);
			return len;
		}

		public void OnCtrl(StreamCtrlEvent evt)
		{
			if (evt.Event == StreamEvent.End)
			{
				if (!evt.IsOk)
				{
					FileStream.Close();
					File.Delete(Uri);
				}
				Ts.SetResult(evt.IsOk);
			}
			else if (evt.Event == StreamEvent.Begin)
			{
				FileStream = new FileStream(Uri, FileMode.OpenOrCreate);
			}
		}

		public Task WaitDone()
		{
			return Ts.Task;
		}

		public void Dispose()
		{
			// if (ReadStream != null)
			// {
			// 	ReadStream.OnReceivedData -= Write;
			// 	ReadStream.OnCtrl -= OnCtrl;
			// 	ReadStream = null;
			// }

			if (FileStream != null)
			{
				FileStream.Dispose();
				FileStream = null;
			}

			Ts = null;
		}

		public bool Exist()
		{
			return IOManager.LocalIOProto.Exists(Uri);
		}

		public int Read(byte[] data, int offset, int len)
		{
			return FileStream.Read(data, offset, len);
		}
	}
}