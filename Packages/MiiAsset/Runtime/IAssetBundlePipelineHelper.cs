using System;
using System.Text.RegularExpressions;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOManagers;
using MiiAsset.Runtime.Pipelines;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace MiiAsset.Runtime
{
	public static class AssetBundlePipelineHelper
	{
		private static readonly Regex HttpsProto = new Regex(@"^https?\://");
		public static ILoadAssetBundlePipeline GetLoadAssetBundlePipeline(this AssetBundleInfo assetBundleInfo,
			IResourceLoadSource loadSource, uint crc, Hash128 hash128, bool isEncrypt, ulong predictFileSize)
		{
			ILoadAssetBundlePipeline pipeline;

			var bundleName = assetBundleInfo.fileName;
			var remoteUri = loadSource.GetSourceUri(bundleName);
			var cacheUri = loadSource.GetCacheUri(bundleName);
			
			// Debug.Log(
			// 	$"loadSource2: {bundleName}, {remoteUri}, {cacheUri}, {cacheUri==""}, {cacheUri==null};");
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
					pipeline = new LoadAssetBundleFromLocalBytesPipeline().Init(remoteUri, crc, isEncrypt,
						predictFileSize);
				#elif SUPPORT_WEBGL_LOCAL_STORAGE
					pipeline = new LoadAssetBundleCustomInternalPipeline().Init(remoteUri, null, crc, hash128, isEncrypt, predictFileSize);
				#else
					// 正常webgl从包内加载, 直接使用内置方式, 暂不支持加密
					pipeline = new LoadAssetBundleInternalPipeline().Init(remoteUri, crc, hash128);
					// pipeline = new LoadAssetBundleFromRemoteMemoryPipeline().Init(remoteUri, crc, isEncrypt);
				#endif
				}
				else
				{
					if (remoteUri != null)
					{
						if (HttpsProto.IsMatch(remoteUri))
						{
							MyLogger.LogError($"检测到正在使用远程uri作为本地资源路径: {assetBundleInfo.bundleName}, {remoteUri}");
							pipeline = new LoadAssetBundleFromRemoteMemoryPipeline().Init(remoteUri, crc, isEncrypt);
						}
						else
						{
							pipeline = new LoadAssetBundlePipelineFromLocalStream().Init(remoteUri, crc, isEncrypt,
								predictFileSize);
						}
					}
					else
					{
						throw new Exception($"加载uri为null: {assetBundleInfo.bundleName}");
					}
				}
			}
			else
			{
				// 从缓存或网络加载
				if (Application.platform == RuntimePlatform.WebGLPlayer)
				{
				#if SUPPORT_WDK
					// 为了应对微信小游戏读文件片段次数过多会崩溃的bug
					pipeline = new LoadAssetBundleFromRemoteBytesPipeline()
						.Init(remoteUri, cacheUri, crc, isEncrypt, predictFileSize);
				#elif SUPPORT_WEBGL_LOCAL_STORAGE
					pipeline = new LoadAssetBundleCustomInternalPipeline().Init(remoteUri, cacheUri, crc, hash128, isEncrypt, predictFileSize);
				#else
					// 正常webgl从包内加载, 直接使用内置方式, 暂不支持加密
					pipeline = new LoadAssetBundleInternalPipeline().Init(remoteUri, crc, hash128);
				#endif
				}
				else
				{
					pipeline = new LoadAssetBundleFromRemoteStreamPipeline().Init(remoteUri, cacheUri, crc, isEncrypt,
						predictFileSize);
				}
			}

			// MyLogger.Log($"userpipeline {pipeline.GetType().Name} for {assetBundleInfo.fileName}");

			return pipeline;
		}

		public static void InvalidateLocalFile(string localUri)
		{
			var exist = IOManager.LocalIOProto.Exists(localUri);
			if (exist)
			{
				MyLogger.LogError($"delete invalid assetbundle {localUri}");
				try
				{
					IOManager.LocalIOProto.Delete(localUri);
				}
				catch (Exception exception)
				{
					MyLogger.LogError($"cannot remove invalid assetbundle file: {localUri}");
					RemoteUriHandler.EmitException(exception);
				}
			}
		}
	}
}