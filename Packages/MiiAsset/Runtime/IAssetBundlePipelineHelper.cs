using MiiAsset.Runtime.Pipelines;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace MiiAsset.Runtime
{
	public static class AssetBundlePipelineHelper
	{
		public static ILoadAssetBundlePipeline GetLoadAssetBundlePipeline(this AssetBundleInfo assetBundleInfo,
			IResourceLoadSource loadSource, uint crc, Hash128 hash128, bool isEncrypt)
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
				// 从包内加载
				if (remoteUri?.StartsWith("jar:") ?? false)
				{
					pipeline = new LoadAssetBundleFromRemoteMemoryPipeline().Init(remoteUri, crc, isEncrypt);
				}
				else if (Application.platform == RuntimePlatform.WebGLPlayer)
				{
				#if SUPPORT_WDK
					// 为了应对微信小游戏读文件片段次数过多会崩溃的bug
					pipeline = new LoadAssetBundleFromLocalBytesPipeline().Init(remoteUri, crc, isEncrypt);
				#else
					// 正常webgl从包内加载, 直接使用内置方式, 暂不支持加密
					pipeline = new LoadAssetBundleInternalPipeline().Init(remoteUri, crc, hash128);
				#endif
				}
				else
				{
					pipeline = new LoadAssetBundlePipelineFromLocalStream().Init(remoteUri, crc, isEncrypt);
				}
			}
			else
			{
				// 从缓存或网络加载
				if (Application.platform == RuntimePlatform.WebGLPlayer)
				{
				#if SUPPORT_WDK
					// 为了应对微信小游戏读文件片段次数过多会崩溃的bug
					pipeline = new LoadAssetBundleFromRemoteBytesPipeline().Init(remoteUri, cacheUri, crc, isEncrypt);
				#else
					// 正常webgl从包内加载, 直接使用内置方式, 暂不支持加密
					pipeline = new LoadAssetBundleInternalPipeline().Init(remoteUri, crc, hash128);
				#endif
				}
				else
				{
					pipeline = new LoadAssetBundleFromRemoteStreamPipeline().Init(remoteUri, cacheUri, crc, isEncrypt);
				}
			}

			// MyLogger.Log($"userpipeline {pipeline.GetType().Name} for {assetBundleInfo.fileName}");

			return pipeline;
		}
	}
}