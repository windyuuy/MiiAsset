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

		private static bool _isInited = false;

		public static async Task<bool> Init()
		{
			if (_isInited)
			{
				return true;
			}

			AdapterInternal.AdaptDefault();
			RegisterCertificateHandler(new AcceptAllCertificate());

			var config = Resources.Load<AssetConsumerConfig>("MiiConfig/ConsumerConfig");
			var result = await Init(config);
			Resources.UnloadAsset(config);
			
			InitDispose();
			
			_isInited = result;
			return result;
		}

		private static void InitDispose()
		{
			Application.quitting += AssetLoader.Dispose;
		}

		private static void Dispose()
		{
			Consumer.Dispose();
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
			if (!remoteBaseUri.EndsWith("/"))
			{
				remoteBaseUri = $"{remoteBaseUri}/";
			}

			return Consumer.UpdateCatalog(remoteBaseUri);
		}

		public static Task<PipelineResult> LoadLocalCatalog()
		{
			return Consumer.LoadLocalCatalog();
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

		public static Task<bool> LoadTags(IEnumerable<string> tags, AssetLoadStatusGroup loadStatus = null)
		{
			if (tags is not string[] tags1)
			{
				tags1 = tags.ToArray();
			}

			return Consumer.LoadTags(tags1, loadStatus);
		}

		public static Task<bool> LoadTags(params string[] tags)
		{
			return Consumer.LoadTags(tags, null);
		}

		public static Task<bool> DownloadTags(IEnumerable<string> tags, AssetLoadStatusGroup loadStatus = null)
		{
			if (tags is not string[] tags1)
			{
				tags1 = tags.ToArray();
			}

			return Consumer.DownloadTags(tags1, loadStatus);
		}

		public static Task<bool> DownloadTags(params string[] tags)
		{
			return Consumer.DownloadTags(tags, null);
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

		public static Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.LoadAsset<T>(address, loadStatus);
		}

		public static Task UnLoadAsset(string address)
		{
			return Consumer.UnLoadAsset(address);
		}

		public static Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.LoadAssetByRefer<T>(address, loadStatus);
		}

		public static AsyncLoadingStatus<T> LoadAssetByReferWrapped<T>(string address, AssetLoadStatusGroup loadStatus = null)
		{
			var task = Consumer.LoadAssetByRefer<T>(address, loadStatus);
			var status = new AsyncLoadingStatus<T>(address, task, loadStatus);
			return status;
		}

		public static Task UnLoadAssetByReferWrapped<T>(AsyncLoadingStatus<T> status)
		{
			var task = Consumer.UnLoadAssetByRefer(status.Address);
			return task;
		}

		public static AsyncLoadingStatus<Scene> LoadSceneByReferWrapped(string sceneAddress, LoadSceneParameters parameters = new(), AssetLoadStatusGroup loadStatus = null)
		{
			var task = Consumer.LoadSceneByRefer(sceneAddress, parameters, loadStatus);
			var status = new AsyncLoadingStatus<Scene>(sceneAddress, task, loadStatus);
			return status;
		}

		public static async Task<Scene> UnLoadSceneByReferWrapped(AsyncLoadingStatus<Scene> status, UnloadSceneOptions options = UnloadSceneOptions.None)
		{
			await Consumer.UnLoadSceneByRefer(status.Address, options);
			var scene = status.Result;
			return scene;
		}

		public static Task UnLoadAssetByRefer(string address)
		{
			return Consumer.UnLoadAssetByRefer(address);
		}

		public static Task<Scene> LoadScene(string sceneAddress, LoadSceneParameters parameters = new(), AssetLoadStatusGroup loadStatus = null)
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

		public static AsyncLoadingStatus<GameObject> InstantiateAsync(string key)
		{
			var status = LoadAssetByReferWrapped<GameObject>(key, new AssetLoadStatusGroup());

			async Task<GameObject> Load()
			{
				var asset = await status.Task;
				var obj = GameObject.Instantiate(asset);
				return obj;
			}

			status.Task = Load();
			return status;
		}

		public static bool ReleaseInstance(string key, GameObject gameObject)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				GameObject.DestroyImmediate(gameObject);
			}
			else
#endif
			{
				GameObject.Destroy(gameObject);
			}

			UnLoadAssetByRefer(key);
			return true;
		}
	}
}