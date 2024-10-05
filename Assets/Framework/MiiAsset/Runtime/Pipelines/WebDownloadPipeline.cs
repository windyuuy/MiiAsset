using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Framework.MiiAsset.Runtime.IOStreams
{
	public class WebDownloadPipeline : IPipeline
	{
		protected TaskCompletionSource<PipelineResult> Ts;

		protected string Uri;
		public byte[] Bytes;

		public WebDownloadPipeline Init(string uri)
		{
			Uri = uri;
			this.Result = new();
			return this;
		}

		public Task<PipelineResult> Run()
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

					Result.Code = (int)code;
					Result.IsOk = code == 200;
					Result.Msg = msg;
					if (!Result.IsOk)
					{
						Result.ErrorType = PipelineErrorType.NetError;
					}

					Ts.SetResult(Result);
				}

				_ = ReadInternal();
			}

			return Ts.Task;
		}

		public void Dispose()
		{
			Ts = null;
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
		}

		public bool IsCached()
		{
			return false;
		}
	}
}