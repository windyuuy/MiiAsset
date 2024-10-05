using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using Lang.Encoding;
using UnityEngine.Networking;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadRemoteTextFilePipeline : ILoadTextAssetPipeline
	{
		protected UnityWebRequest Uwr;
		protected DownloadHandler DownloadHandler;
		protected TaskCompletionSource<PipelineResult> Ts;

		protected string Uri;
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
					Ts = new();
					Uwr = UnityWebRequest.Get(this.Uri);
					DownloadHandler = Uwr.downloadHandler;
					var op = Uwr.SendWebRequest();
					await op.GetTask();
					var code = Uwr.responseCode;
					var msg = Uwr.error;
					Text = DownloadHandler.text;

					DownloadHandler.Dispose();
					DownloadHandler = null;

					Uwr.Dispose();
					Uwr = null;

					Result.Code = (int)code;
					Result.IsOk = code == 200;
					Result.Msg = msg;
					if (!Result.IsOk)
					{
						Result.ErrorType = PipelineErrorType.NetError;
					}
					else
					{
						try
						{
							IOManager.LocalIOProto.EnsureFileDirectory(CacheUri);
							await IOManager.LocalIOProto.WriteAllTextAsync(CacheUri, Text, EncodingExt.UTF8WithoutBom);
							Result.IsOk = true;
						}
						catch (Exception exception)
						{
							Result.ErrorType = PipelineErrorType.FileSystemError;
							Result.Exception = exception;
						}
					}

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
	}
}