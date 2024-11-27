using System;
using System.IO;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOManagers;

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
			// MyLogger.Log($"WriteSeg: {Path.GetFileName(Uri)}, {len}, {FileStream != null}");
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
					MyLogger.LogError($"Download-Exception: {Uri}, {evt.GetReason()}");
					FileStream.Close();
					try
					{
						IOManager.LocalIOProto.Delete(ToTempPath(Uri));
					}
					catch (Exception exception)
					{
						// Result.ErrorType = PipelineErrorType.FileSystemError;
						MyLogger.LogException(exception);
					}
				}
				else
				{
					try
					{
#if false
						var bytes = new byte[FileStream.Length];
						FileStream.Seek(0, SeekOrigin.Begin);
						var count = FileStream.Read(bytes, 0, bytes.Length);
						await IOManager.LocalIOProto.WriteAllBytesAsync(ToTempPath(Uri),bytes);
#endif
						// await FileStream.FlushAsync();
						FileStream.Close();
						IOManager.LocalIOProto.Move(ToTempPath(Uri), Uri);
					}
					catch (Exception exception)
					{
						Result.Exception = exception;
						Result.ErrorType = PipelineErrorType.FileSystemError;
						Result.IsOk = false;
						// MyLogger.LogException(exception);
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
#if true
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