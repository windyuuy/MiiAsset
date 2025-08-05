using MiiAsset.Runtime.Pipelines;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace MiiAsset.Runtime
{
	public static class AssetBundlePipelineHelper
	{
		public static ILoadAssetBundlePipeline GetLoadAssetBundlePipeline(this AssetBundleInfo assetBundleInfo,
			IResourceLoadSource loadSource, uint crc, Hash128 hash128)
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
					pipeline = new LoadAssetBundleFromRemoteMemoryPipeline().Init(remoteUri, crc);
				}
				else if (Application.platform == RuntimePlatform.WebGLPlayer)
				{
				#if SUPPORT_WDK
					// 为了应对微信小游戏读文件片段次数过多会崩溃的bug
					pipeline = new LoadAssetBundleFromLocalBytesPipeline().Init(remoteUri, crc);
				#else
					pipeline = new LoadAssetBundleInternalPipeline().Init(remoteUri, crc, hash128);
				#endif
				}
				else
				{
				// #if UNITY_WEBGL
				// 	pipeline = new LoadAssetBundleInternalPipeline().Init(remoteUri, crc, hash128);
				// #else
					// pipeline = new LoadAssetBundleBytesPipeline().Init(remoteUri);
					pipeline = new LoadAssetBundlePipelineFromLocalStream().Init(remoteUri, crc);
				// #endif
				}
			}
			else
			{
				// 从缓存或网络加载
				if (Application.platform == RuntimePlatform.WebGLPlayer)
				{
				#if SUPPORT_WDK
					// 为了应对微信小游戏读文件片段次数过多会崩溃的bug
					pipeline = new LoadAssetBundleFromRemoteBytesPipeline().Init(remoteUri, cacheUri, crc);
				#else
					pipeline = new LoadAssetBundleInternalPipeline().Init(remoteUri, crc, hash128);
				#endif
				}
				else
				{
				// #if UNITY_WEBGL
				// 	pipeline = new LoadAssetBundleInternalPipeline().Init(remoteUri, crc, hash128);
				// #else
					pipeline = new LoadAssetBundleFromRemoteStreamPipeline().Init(remoteUri, cacheUri, crc);
				// #endif
				}
			}

			// MyLogger.Log($"userpipeline {pipeline.GetType().Name} for {assetBundleInfo.fileName}");

			return pipeline;
		}
	}
}