using System;
using System.IO;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
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
				Result.IsOk = evt.IsOk && evt.Capability == FileStream.Length;
				if (!evt.IsOk)
				{
					if (evt.Capability != FileStream.Length)
					{
						Result.ErrorType = PipelineErrorType.FileSystemError;
					}

					MyLogger.LogError($"Download-Failed: {Uri}, {evt.GetReason()}");
					FileStream.Close();
					FileStream = null;
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
						FileStream = null;
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
					if (FileStream != null)
					{
						FileStream.Close();
						FileStream.Dispose();
					}

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

		public void Start()
		{
			if (Ts != null && !Ts.Task.IsCompleted)
			{
				Debug.LogError($"WriteFileStream is Running Already, will be overwritten");
			}

			Ts = new();
		}

		public void Dispose()
		{
			if (FileStream != null)
			{
				FileStream.Dispose();
				FileStream = null;
			}

			if (Ts != null)
			{
				if (!Ts.Task.IsCompleted)
				{
					Ts.SetException(new OperationCanceledException("WriteFileStream is disposed before await return"));
				}

				Ts = null;
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