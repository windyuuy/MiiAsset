using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Framework.MiiAsset.Runtime.IOStreams
{
	public class WebDownloadPipeline : IPipeline
	{
		protected TaskCompletionSource<bool> Ts;

		protected string Uri;
		public byte[] Bytes;

		public WebDownloadPipeline Init(string uri)
		{
			Uri = uri;
			return this;
		}

		public Task Run()
		{
			if (Ts == null)
			{
				async Task ReadInternal()
				{
					Ts = new();
					var uwr = UnityWebRequest.Get(this.Uri);

					var op = uwr.SendWebRequest();
					await op.GetTask();
					var code = uwr.responseCode;
					var msg = uwr.error;

					Bytes = uwr.downloadHandler.data;

					uwr.Dispose();
					uwr = null;

					Ts.SetResult(code == 200);
				}

				_ = ReadInternal();
			}

			return Ts.Task;
		}

		public void Dispose()
		{
			Ts = null;
		}

		public void Build()
		{
		}

		public bool IsCached()
		{
			return false;
		}
	}
}