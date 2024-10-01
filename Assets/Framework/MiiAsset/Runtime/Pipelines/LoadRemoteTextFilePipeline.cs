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
		protected TaskCompletionSource<bool> Ts;

		protected string Uri;
		protected string CacheUri;

		public LoadRemoteTextFilePipeline Init(string uri, string cacheUri)
		{
			Uri = uri;
			CacheUri = cacheUri;
			this.Build();
			return this;
		}

		public void Dispose()
		{
		}

		public void Build()
		{
		}

		public string Text { get; set; }

		public Task Run()
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
					
					IOManager.LocalIOProto.EnsureFileDirectory(CacheUri);
					await IOManager.LocalIOProto.WriteAllTextAsync(CacheUri, Text, EncodingExt.UTF8WithoutBom);

					Ts.SetResult(code == 200);
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