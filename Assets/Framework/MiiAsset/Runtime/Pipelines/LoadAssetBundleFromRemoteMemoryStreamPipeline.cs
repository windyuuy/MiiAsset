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

		public void Build()
		{
			DownloadPipeline = new WebDownloadPipeline().Init(RemoteUri);
		}

		public async Task Run()
		{
			await DownloadPipeline.Run();
			var bytes = DownloadPipeline.Bytes;
			this.AssetBundle = AssetBundle.LoadFromMemory(bytes);
		}

		public bool IsCached()
		{
			return false;
		}

		public AssetBundle AssetBundle { get; set; }
	}
}