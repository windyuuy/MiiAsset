using System;
using System.Threading.Tasks;
using System.Web;
using Lang.Encoding;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOManagers;
using MonoExtLib.AsyncExt;
using UnityEngine.Networking;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadRemoteTextFilePipeline : ILoadTextAssetPipeline
	{
		protected UnityWebRequest Uwr;
		protected DownloadHandler DownloadHandler;
		protected TaskCompletionSource<PipelineResult> Ts;

		protected string Uri;
		protected string UnescapeUri => HttpUtility.UrlDecode(Uri);
		protected string CacheUri;

		public LoadRemoteTextFilePipeline Init(string uri, string cacheUri)
		{
			Uri = uri;
			CacheUri = cacheUri;
			Result = new();
			this.Build();
			return this;
		}

		public void Dispose()
		{
			Reset();
		}

		private void Reset()
		{
			Text = null;
			Ts = null;
			if (DownloadHandler != null)
			{
				DownloadHandler.Dispose();
				DownloadHandler = null;
			}

			if (Uwr != null)
			{
				Uwr.Dispose();
				Uwr = null;
			}
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
		}

		public string Text { get; set; }

		public Task<PipelineResult> Run()
		{
			if (Ts == null)
			{
				async Task ReadInternal()
				{
					try
					{
						Result.Status = PipelineStatus.Running;
						Ts = new();
						MyLogger.Log($"download: {UnescapeUri}");
						Uwr = UnityWebRequest.Get(this.Uri);
						DownloadHandler = Uwr.downloadHandler;
						IOManager.LocalIOProto.SetUwr(Uwr);
						var op = Uwr.SendWebRequest();
						await op.GetTask();
						var code = (int)Uwr.responseCode;
						var msg = Uwr.error;
						var uwrResult = Uwr.result;
						Text = DownloadHandler.text;

						DownloadHandler.Dispose();
						DownloadHandler = null;

						Uwr.Dispose();
						Uwr = null;

						Result.Code = (int)code;
						Result.IsOk = uwrResult == UnityWebRequest.Result.Success;
						Result.Msg = msg;

						if (Result.IsOk)
						{
							MyLogger.Log($"download-done: {UnescapeUri}, {Result.IsOk}, {Result.Code}, {Result.Msg}");
						}
						else
						{
							MyLogger.LogError(
								$"download-failed: {UnescapeUri}, {Result.IsOk}, {Result.Code}, {Result.Msg}, {Text}");
						}
					}
					catch (Exception exception)
					{
						Result.Exception = exception;
						MyLogger.LogError($"download-failed: {UnescapeUri}");
						MyLogger.LogException(exception, "e42");
					}

					if (!Result.IsOk)
					{
						Result.ErrorType = PipelineErrorType.NetError;
					}
					else
					{
						try
						{
							// IOManager.LocalIOProto.EnsureFileDirectory(CacheUri);
							await IOManager.LocalIOProto.WriteAllTextAsync(CacheUri, Text, EncodingExt.UTF8WithoutBom);
							Result.IsOk = true;
						}
						catch (Exception exception)
						{
							Result.ErrorType = PipelineErrorType.FileSystemError;
							Result.Exception = exception;
						}
					}

					Result.Status = PipelineStatus.Done;

					Ts.SetResult(Result);
				}

				_ = ReadInternal();
			}

			return Ts.Task;
		}

		public bool IsCached()
		{
			return false;
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
				var uwrDownloadedBytes = Uwr.downloadedBytes;
				ulong waitDoneAddition = 100;
				Progress = new()
				{
					Total = Math.Max(uwrDownloadedBytes, (ulong)(uwrDownloadedBytes / Uwr.downloadProgress)) +
					        waitDoneAddition,
					Count = uwrDownloadedBytes,
				};
			}

			return Progress;
		}

		public void Invalidate()
		{
			Reset();
			DeleteCachedFile();
			Build();
		}

		private void DeleteCachedFile()
		{
			var exist = IOManager.LocalIOProto.Exists(CacheUri);
			if (exist)
			{
				IOManager.LocalIOProto.Delete(CacheUri);
			}
		}
	}
}