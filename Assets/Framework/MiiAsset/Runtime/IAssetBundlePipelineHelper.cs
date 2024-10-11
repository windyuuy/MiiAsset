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
			// if (!IsWebUri(remoteUri))
			// {
			// 	remoteUri = "file://" + remoteUri;
			// }

			if (cacheUri == null)
			{
				if (remoteUri.StartsWith("jar:"))
				{
					pipeline = new LoadAssetBundleFromRemoteMemoryStreamPipeline().Init(remoteUri);
				}
				else if (Application.platform == RuntimePlatform.WebGLPlayer)
				{
					// 为了应对微信小游戏读文件片段次数过多会崩溃的bug
					pipeline = new LoadAssetBundleBytesPipeline().Init(remoteUri);
				}
				else
				{
					pipeline = new LoadAssetBundleBytesPipeline().Init(remoteUri);
					// pipeline = new LoadAssetBundlePipeline().Init(remoteUri);
				}
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