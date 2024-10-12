using System.Text.RegularExpressions;
using Framework.MiiAsset.Runtime.Pipelines;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace Framework.MiiAsset.Runtime
{
	public static class AssetBundlePipelineHelper
	{
		public static ILoadAssetBundlePipeline GetLoadAssetBundlePipeline(this AssetBundleInfo assetBundleInfo, IResourceLoadSource loadSource, uint crc)
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
					pipeline = new LoadAssetBundleFromRemoteMemoryStreamPipeline().Init(remoteUri, crc);
				}
				else if (Application.platform == RuntimePlatform.WebGLPlayer)
				{
					// 为了应对微信小游戏读文件片段次数过多会崩溃的bug
					pipeline = new LoadAssetBundleBytesPipeline().Init(remoteUri, crc);
				}
				else
				{
					// pipeline = new LoadAssetBundleBytesPipeline().Init(remoteUri);
					pipeline = new LoadAssetBundlePipeline().Init(remoteUri, crc);
				}
			}
			else
			{
				pipeline = new LoadAssetBundleFromRemoteStreamPipeline().Init(remoteUri, cacheUri, crc);
			}
			
			// Debug.Log($"userpipeline {pipeline.GetType().Name} for {assetBundleInfo.fileName}");

			return pipeline;
		}
	}
}