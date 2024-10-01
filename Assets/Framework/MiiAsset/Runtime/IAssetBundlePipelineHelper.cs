using Framework.MiiAsset.Runtime.Pipelines;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace Framework.MiiAsset.Runtime
{
	public static class AssetBundlePipelineHelper
	{
		public static ILoadAssetBundlePipeline GetLoadAssetBundlePipeline(this AssetBundleInfo assetBundleInfo, IResourceLoadSource loadSource)
		{
			ILoadAssetBundlePipeline pipeline;

			var remoteUri = loadSource.GetSourceUri(assetBundleInfo.fileName);
			var cacheUri = loadSource.GetCacheUri(assetBundleInfo.fileName);
			if (!IsWebUri(remoteUri))
			{
				remoteUri = "file://" + remoteUri;
			}

			if (cacheUri == null)
			{
				pipeline = new LoadAssetBundleFromRemoteMemoryStreamPipeline().Init(remoteUri);
			}
			else
			{
				pipeline = new LoadAssetBundleFromRemoteStreamPipeline().Init(remoteUri, cacheUri);
			}

			return pipeline;
		}

		public static bool IsWebUri(string uri)
		{
			return uri.Contains("://");
		}
	}
}