using System;
using System.IO;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.IOStreams
{
	public class WriteFileStream : IWriteStream, ICacheableStream
	{
		// protected IPumpStream ReadStream;
		protected string Uri;
		public PipelineResult Result;

		public WriteFileStream Init(string uri)
		{
			this.Uri = uri;
			this.Result = new();
			this.Ts = new();
			return this;
		}

		protected FileStream FileStream;
		protected TaskCompletionSource<PipelineResult> Ts;

		public int Write(byte[] data, int offset, int len)
		{
			if (FileStream != null)
			{
				FileStream.Write(data, offset, len);
				return len;
			}
			else
			{
				return 0;
			}
		}

		public void OnCtrl(StreamCtrlEvent evt)
		{
			if (evt.Event == StreamEvent.End)
			{
				Result.IsOk = evt.IsOk;
				if (!evt.IsOk)
				{
					Result.ErrorType = PipelineErrorType.FileSystemError;
					FileStream.Close();
					try
					{
						File.Delete(Uri);
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}

				Ts.SetResult(Result);
			}
			else if (evt.Event == StreamEvent.Begin)
			{
				try
				{
					IOManager.LocalIOProto.EnsureFileDirectory(Uri);
					FileStream = new FileStream(Uri, FileMode.OpenOrCreate);
				}
				catch (Exception exception)
				{
					Result.Exception = exception;
					Result.ErrorType = PipelineErrorType.FileSystemError;
					Ts.SetResult(Result);
					if (evt.PumpStream != null)
					{
						evt.PumpStream.Abort();
					}
				}
			}
		}

		public Task<PipelineResult> WaitDone()
		{
			return Ts.Task;
		}

		public void Dispose()
		{
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
			if (FileStream != null)
			{
				return FileStream.Read(data, offset, len);
			}
			else
			{
				return 0;
			}
		}
	}
}