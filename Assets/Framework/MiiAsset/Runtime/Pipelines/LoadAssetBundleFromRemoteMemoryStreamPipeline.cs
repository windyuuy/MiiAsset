using System.IO;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromRemoteMemoryStreamPipeline : ILoadAssetBundlePipeline
	{
		protected string RemoteUri;
		protected WebDownloadPipeline DownloadPipeline;

		public LoadAssetBundleFromRemoteMemoryStreamPipeline Init(string remoteUri)
		{
			RemoteUri = remoteUri;
			this.Build();
			return this;
		}

		public void Dispose()
		{
			DownloadPipeline.Dispose();
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
			DownloadPipeline = new WebDownloadPipeline().Init(RemoteUri);
		}

		public async Task<PipelineResult> Run()
		{
			Result = await DownloadPipeline.Run();
			if (Result.IsOk)
			{
				var bytes = DownloadPipeline.Bytes;
				this.AssetBundle = AssetBundle.LoadFromMemory(bytes);

				if (this.AssetBundle == null)
				{
					Result = new()
					{
						IsOk = false,
						Code = 0,
						Msg = "invalid bundle data",
						ErrorType = PipelineErrorType.DataIncorrect,
					};
				}
			}

			return Result;
		}

		public bool IsCached()
		{
			return false;
		}

		public AssetBundle AssetBundle { get; set; }
	}
}