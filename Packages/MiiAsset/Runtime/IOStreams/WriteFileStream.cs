using System;
using System.IO;
using System.Threading.Tasks;
using MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace MiiAsset.Runtime.IOStreams
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

		protected Stream FileStream;
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

		public async void OnCtrl(StreamCtrlEvent evt)
		{
			if (evt.Event == StreamEvent.End)
			{
				Result.IsOk = evt.IsOk;
				if (!evt.IsOk)
				{
					Debug.LogError($"Download-Exception: {Uri}, {evt.GetReason()}");
					FileStream.Close();
					try
					{
						IOManager.LocalIOProto.Delete(ToTempPath(Uri));
					}
					catch (Exception exception)
					{
						Result.ErrorType = PipelineErrorType.FileSystemError;
						Debug.LogException(exception);
					}
				}
				else
				{
					try
					{
#if true
						var bytes = new byte[FileStream.Length];
						FileStream.Seek(0, SeekOrigin.Begin);
						var count = FileStream.Read(bytes, 0, bytes.Length);
						await IOManager.LocalIOProto.WriteAllBytesAsync(ToTempPath(Uri),bytes);
#endif
						FileStream.Close();
						IOManager.LocalIOProto.Move(ToTempPath(Uri), Uri);
					}
					catch (Exception exception)
					{
						Result.Exception = exception;
						Result.ErrorType = PipelineErrorType.FileSystemError;
						Result.IsOk = false;
						// Debug.LogException(exception);
					}
				}

				Result.Status = PipelineStatus.Done;

				Ts.SetResult(Result);
			}
			else if (evt.Event == StreamEvent.Begin)
			{
				try
				{
					// IOManager.LocalIOProto.EnsureFileDirectory(Uri);
#if false
					FileStream = IOManager.LocalIOProto.OpenWrite(ToTempPath(Uri));
#else
					FileStream = new MemoryStream();
#endif
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

		private string ToTempPath(string uri)
		{
			return uri + "__temp";
		}

		public Task<PipelineResult> WaitDone()
		{
			return Ts.Task;
		}

		public PipelineProgress GetProgress()
		{
			return new PipelineProgress().Set01Progress(Result.IsOk);
		}

		public void Dispose()
		{
			if (FileStream != null)
			{
				FileStream.Dispose();
				FileStream = null;
			}
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