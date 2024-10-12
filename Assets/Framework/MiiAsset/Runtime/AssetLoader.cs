using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.Adapter;
using Framework.MiiAsset.Runtime.CertificateHandlers;
using Framework.MiiAsset.Runtime.IOManagers;
using Framework.MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Framework.MiiAsset.Runtime
{
	public static class AssetLoader
	{
		private static IAssetProvider Consumer;
		private static readonly AdapterInternal AdapterInternal = new();

		public static void Adapt(IAdapter adapter)
		{
			AdapterInternal.Adapt(adapter);
		}

		public static async Task<bool> Init()
		{
			AdapterInternal.AdaptDefault();
			RegisterCertificateHandler(new AcceptAllCertificate());

			var config = Resources.Load<AssetConsumerConfig>("MiiConfig/ConsumerConfig");
			var result = await Init(config);
			Resources.UnloadAsset(config);
			return result;
		}

		public static void RegisterCertificateHandler(CertificateHandler certificateHandler)
		{
			IOManager.LocalIOProto.RegisterCertificateHandler(certificateHandler);
		}

		public static async Task<bool> Init(AssetConsumerConfig config)
		{
#if UNITY_EDITOR
			if (config.loadType == AssetConsumerConfig.LoadType.LoadFromBundle)
			{
				Consumer = new BundledAssetProvider();
			}
			else if (config.loadType == AssetConsumerConfig.LoadType.LoadFromEditor)
			{
				Consumer = new EditorAssetProvider();
			}
			else
			{
				throw new ArgumentException($"loadType: {config.loadType}");
			}
#else
			Consumer = new BundledAssetProvider();
#endif
			return await Consumer.Init(config);
		}
		//
		// public static void Init()
		// {
		// 	Consumer.Init();
		// }

		public static Task<PipelineResult> UpdateCatalog(string remoteBaseUri)
		{
			return Consumer.UpdateCatalog(remoteBaseUri);
		}

		public static Task<PipelineResult> CleanUpOldVersionFiles()
		{
			return Consumer.CleanUpOldVersionFiles();
		}

		public static bool AllowTags(IEnumerable<string> tags)
		{
			if (tags is not string[] tags1)
			{
				tags1 = tags.ToArray();
			}

			return Consumer.AllowTags(tags1);
		}

		public static bool AllowTags(params string[] tags)
		{
			return Consumer.AllowTags(tags);
		}

		public static Task LoadTags(IEnumerable<string> tags, AssetLoadStatusGroup loadStatus = null)
		{
			if (tags is not string[] tags1)
			{
				tags1 = tags.ToArray();
			}

			return Consumer.LoadTags(tags1, loadStatus);
		}

		public static Task LoadTags(params string[] tags)
		{
			return Consumer.LoadTags(tags, null);
		}

		public static Task DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.DownloadTags(tags, loadStatus);
		}

		public static Task UnLoadTags(IEnumerable<string> tags)
		{
			if (tags is not string[] tags1)
			{
				tags1 = tags.ToArray();
			}

			return Consumer.UnLoadTags(tags1);
		}

		public static Task UnLoadTags(params string[] tags)
		{
			return Consumer.UnLoadTags(tags);
		}

		public static long GetDownloadSize(IEnumerable<string> tags)
		{
			return Consumer.GetDownloadSize(tags);
		}

		public static long GetDownloadSize(params string[] tags)
		{
			return Consumer.GetDownloadSize(tags);
		}

		public static bool IsAddressInTags(string address, IEnumerable<string> tags)
		{
			return Consumer.IsAddressInTags(address, tags);
		}

		public static bool IsBundleInTags(string bundleFileName, IEnumerable<string> tags)
		{
			return Consumer.IsBundleInTags(bundleFileName, tags);
		}

		public static Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus = null) where T : UnityEngine.Object
		{
			return Consumer.LoadAssetJust<T>(address, loadStatus);
		}

		public static Task UnloadAssetJust(string address)
		{
			return Consumer.UnloadAssetJust(address);
		}

		public static Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus = null) where T : UnityEngine.Object
		{
			return Consumer.LoadAsset<T>(address, loadStatus);
		}

		public static Task UnLoadAsset(string address)
		{
			return Consumer.UnLoadAsset(address);
		}

		public static Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus = null) where T : UnityEngine.Object
		{
			return Consumer.LoadAssetByRefer<T>(address, loadStatus);
		}

		public static Task UnLoadAssetByRefer(string address)
		{
			return Consumer.UnLoadAssetByRefer(address);
		}

		public static Task LoadScene(string sceneAddress, LoadSceneParameters parameters = new(), AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.LoadScene(sceneAddress, parameters, loadStatus);
		}

		public static Task UnLoadScene(string sceneAddress, UnloadSceneOptions options = UnloadSceneOptions.None)
		{
			return Consumer.UnLoadScene(sceneAddress, options);
		}

		public static Task LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters = new(), AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.LoadSceneByRefer(sceneAddress, parameters, loadStatus);
		}

		public static Task UnLoadSceneByRefer(string sceneAddress, UnloadSceneOptions options = UnloadSceneOptions.None)
		{
			return Consumer.UnLoadSceneByRefer(sceneAddress, options);
		}

		public static TagsBundleSet GetTagsBundleSet(params string[] tags)
		{
			return GetTagsBundleSet(tags, false);
		}

		public static TagsBundleSet GetTagsBundleSet(IEnumerable<string> tags, bool allowTags = false)
		{
			var tagsBundleSet = new TagsBundleSet(Consumer, tags);
			if (allowTags)
			{
				tagsBundleSet.AllowTags();
			}

			return tagsBundleSet;
		}
	}
}