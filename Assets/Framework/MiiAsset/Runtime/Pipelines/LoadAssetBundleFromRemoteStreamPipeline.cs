using System.Threading.Tasks;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromRemoteStreamPipeline : ILoadAssetBundlePipeline
	{
		protected DownloadPipeline DownloadPipeline;
		protected LoadAssetBundlePipeline LoadAssetBundlePipeline;

		protected string RemoteUri;
		protected string LocalUri;

		public LoadAssetBundleFromRemoteStreamPipeline Init(string remoteUri, string localUri)
		{
			RemoteUri = remoteUri;
			LocalUri = localUri;
			this.Build();
			return this;
		}

		public void Dispose()
		{
			if (DownloadPipeline != null)
			{
				DownloadPipeline.Dispose();
				DownloadPipeline = null;
			}

			if (LoadAssetBundlePipeline != null)
			{
				LoadAssetBundlePipeline.Dispose();
				LoadAssetBundlePipeline = null;
			}
		}

		public void Build()
		{
			DownloadPipeline = new DownloadPipeline().Init(RemoteUri, LocalUri);
			LoadAssetBundlePipeline = new LoadAssetBundlePipeline().Init(LocalUri);
		}

		public AssetBundle AssetBundle => LoadAssetBundlePipeline.AssetBundle;

		public async Task Run()
		{
			await DownloadPipeline.Run();
			await LoadAssetBundlePipeline.Run();
		}

		public bool IsCached()
		{
			return LoadAssetBundlePipeline.IsCached();
		}
	}
}