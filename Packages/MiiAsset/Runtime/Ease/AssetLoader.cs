using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.CertificateHandlers;
using MiiAsset.Runtime.IOManagers;
using MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace MiiAsset.Runtime
{
	public static class AssetLoader
	{
		internal static IAssetProvider Consumer;
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

			var config = AssetConsumerConfig.Load();
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

		public static void Dispose()
		{
			Consumer.Dispose();
		}

		/// <summary>
		/// 注册自定义证书许可策略
		/// </summary>
		/// <param name="certificateHandler"></param>
		public static void RegisterCertificateHandler(CertificateHandler certificateHandler)
		{
			IOManager.LocalIOProto.RegisterCertificateHandler(certificateHandler);
		}

		public static async Task<bool> Init(AssetConsumerConfig config)
		{
			Debug.Assert(config != null, "config != null");
			SetLoadAssetTimeout(config.LoadTimeout / 1000.0f, config.checkLoadTimeout, config.displayLoadTimeout);
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

		/// <summary>
		/// 清理旧版本文件
		/// </summary>
		/// <returns></returns>
		public static Task<PipelineResult> CleanUpOldVersionFiles()
		{
			return Consumer.CleanUpOldVersionFiles();
		}

		/// <summary>
		/// 设置下载最大并发数
		/// </summary>
		/// <param name="maxCount"></param>
		public static void SetDownloadMaxCount(int maxCount)
		{
			BundleWebSemaphore.MaxCount = maxCount;
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

		public static Task<bool> DownloadBatch(int batch, AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.DownloadTags(new string[] { $"batch{batch}" }, loadStatus);
		}

		public static Task<bool> DownloadAll(AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.DownloadTags(new string[] { "all" }, loadStatus);
		}

		public static Task<bool> DownloadAllLeft(AssetLoadStatusGroup loadStatus = null)
		{
			return DownloadAll(loadStatus);
		}

		public static long GetBatchDownloadSize(int batch)
		{
			return Consumer.GetDownloadSize(new string[] { $"batch{batch}" });
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

		public static string GetAddressFromGuid(string guid)
		{
			return Consumer.GetAddressFromGuid(guid);
		}

#if !DISABLE_NOREFERCOUNT_API
		/// <summary>
		/// 无视bundle引用计数, 直接加载资源
		/// </summary>
		/// <param name="address"></param>
		/// <param name="loadStatus"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus = null)
			where T : UnityEngine.Object
		{
			return Consumer.LoadAssetJust<T>(address, loadStatus);
		}

		/// <summary>
		/// 无视bundle引用计数,直接卸载资源
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static Task UnloadAssetJust(string address)
		{
			return Consumer.UnloadAssetJust(address);
		}

		/// <summary>
		/// 不带引用计数加载资源
		/// </summary>
		/// <param name="address"></param>
		/// <param name="loadStatus"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.LoadAsset<T>(address, loadStatus);
		}

		/// <summary>
		/// 不带引用计数卸载资源
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static Task UnLoadAsset(string address)
		{
			return Consumer.UnLoadAsset(address);
		}

		/// <summary>
		/// 不带引用计数加载场景,老项目误用
		/// </summary>
		/// <param name="sceneAddress"></param>
		/// <param name="parameters"></param>
		/// <param name="loadStatus"></param>
		/// <returns></returns>
		public static Task<Scene> LoadScene(string sceneAddress, LoadSceneParameters parameters = new(),
			AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.LoadScene(sceneAddress, parameters, loadStatus);
		}

		/// <summary>
		/// 不带引用计数卸载场景,老项目勿用
		/// </summary>
		/// <param name="sceneAddress"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public static Task UnLoadScene(string sceneAddress, UnloadSceneOptions options = UnloadSceneOptions.None)
		{
			return Consumer.UnLoadScene(sceneAddress, options);
		}
#endif

		private static bool _enableTimeout = true;
		private static bool _displayTimeout = true;
		private static float _timeout = 5;

		public static void SetLoadAssetTimeout(float timeout, bool enable, bool displayTimeout)
		{
			if (timeout <= 0)
			{
				Debug.LogError("资源加载超时时长 timeout<=0, 重置为默认值");
				timeout = 5000;
			}

			_timeout = timeout;
			_enableTimeout = enable;
			_displayTimeout = displayTimeout;
		}

		private static readonly PooledLinkedList<(float timeEnd, string address)> TimeoutMap = new PooledLinkedList<(float, string)>();

		public static int CheckTimeout()
		{
			var time = UnityEngine.Time.time;
			var timeoutCount = 0;
			foreach (var node in TimeoutMap.ToEnumerable())
			{
				if (node.Value.timeEnd < time)
				{
					++timeoutCount;
					var address = node.Value.address;
					var isRemoveCorrect = TimeoutMap.Remove(node);
					var isAllBundlesLoaded = IsAssetBundlesOfAssetLoaded(address);
					var timeoutMsg = $"加载超时z:{isRemoveCorrect},{isAllBundlesLoaded},{address}";
					MyLogger.LogError(timeoutMsg);
					if (_displayTimeout)
					{
						IOManager.Widget.ShowToast(timeoutMsg, 5);
					}
				}
			}

			return timeoutCount;
		}

		public static bool IsAssetBundlesOfAssetLoaded(string address)
		{
			return Consumer.IsAssetBundlesOfAssetLoaded(address);
		}

		/// <summary>
		/// 带引用计数加载资源
		/// </summary>
		/// <param name="address"></param>
		/// <param name="loadStatus"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus = null)
		{
			if (_enableTimeout)
			{
				return LoadAssetByReferWithTimeout<T>(address, loadStatus);
			}
			else
			{
				return LoadAssetByReferWithInternal<T>(address, loadStatus);
			}
		}

		private static async Task<T> LoadAssetByReferWithInternal<T>(string address, AssetLoadStatusGroup loadStatus = null)
		{
			try
			{
				return await Consumer.LoadAssetByRefer<T>(address, loadStatus);
			}
			catch (Exception exception)
			{
				MyLogger.LogException(exception);
				_ = IOManager.Widget.ShowToast(exception.Message, 5);
				throw;
			}
		}

		private static async Task<T> LoadAssetByReferWithTimeout<T>(string address, AssetLoadStatusGroup loadStatus = null)
		{
			var timeStart = UnityEngine.Time.time;
			var timeEnd = timeStart + _timeout;
			var node = TimeoutMap.AddLast((timeEnd, address));
			try
			{
				var ret = await Consumer.LoadAssetByRefer<T>(address, loadStatus);
				if (!TimeoutMap.Remove(node))
				{
					var time2 = UnityEngine.Time.time;
					MyLogger.Log($"ldab-ATimeout, but Loaded finally: {address},TimeCost: {time2 - timeStart}");
				}

				return ret;
			}
			catch (Exception exception)
			{
				MyLogger.LogError($"Unexpected Error loading {address}");
				MyLogger.LogException(exception);
				TimeoutMap.Remove(node);
				_ = IOManager.Widget.ShowToast(exception.Message, 5);
				throw;
			}
		}

		/// <summary>
		/// 带引用计数加载资源
		/// </summary>
		/// <param name="address"></param>
		/// <param name="createStatus"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static AsyncLoadingStatus<T> LoadAssetByReferWrapped<T>(string address,
			bool createStatus = false)
		{
			AssetLoadStatusGroup loadStatus = createStatus ? new AssetLoadStatusGroup() : null;
			var task = LoadAssetByRefer<T>(address, loadStatus);
			var status = new AsyncLoadingStatus<T>(address, task, loadStatus);
			return status;
		}

		/// <summary>
		/// 带引用计数加载场景
		/// </summary>
		/// <param name="sceneAddress"></param>
		/// <param name="parameters"></param>
		/// <param name="loadStatus"></param>
		/// <returns></returns>
		public static Task<Scene> LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters = new(),
			AssetLoadStatusGroup loadStatus = null)
		{
			if (_enableTimeout)
			{
				return LoadSceneByReferWithTimeout(sceneAddress, parameters, loadStatus);
			}
			else
			{
				return LoadSceneByReferInternal(sceneAddress, parameters, loadStatus);
			}
		}

		private static async Task<Scene> LoadSceneByReferInternal(string sceneAddress, LoadSceneParameters parameters = new(),
			AssetLoadStatusGroup loadStatus = null)
		{
			try
			{
				return await Consumer.LoadSceneByRefer(sceneAddress, parameters, loadStatus);
			}
			catch (Exception exception)
			{
				MyLogger.LogException(exception);
				_ = IOManager.Widget.ShowToast(exception.Message, 5);
				throw;
			}
		}

		private static async Task<Scene> LoadSceneByReferWithTimeout(string sceneAddress, LoadSceneParameters parameters = new(),
			AssetLoadStatusGroup loadStatus = null)
		{
			var timeStart = UnityEngine.Time.time;
			var timeEnd = timeStart + _timeout;
			var node = TimeoutMap.AddLast((timeEnd, sceneAddress));
			try
			{
				var ret = await Consumer.LoadSceneByRefer(sceneAddress, parameters, loadStatus);
				if (!TimeoutMap.Remove(node))
				{
					var time2 = UnityEngine.Time.time;
					MyLogger.Log($"ldab-ATimeout, but Loaded finally: {sceneAddress},TimeCost: {time2 - timeStart}");
				}

				return ret;
			}
			catch (Exception exception)
			{
				MyLogger.LogError($"Unexpected Error loading {sceneAddress}");
				MyLogger.LogException(exception);
				TimeoutMap.Remove(node);
				_ = IOManager.Widget.ShowToast(exception.Message, 5);
				throw;
			}
		}

		/// <summary>
		/// 带引用计数加载场景
		/// </summary>
		/// <param name="sceneAddress"></param>
		/// <param name="parameters"></param>
		/// <param name="createStatus"></param>
		/// <returns></returns>
		public static AsyncLoadingStatus<Scene> LoadSceneByReferWrapped(string sceneAddress,
			LoadSceneParameters parameters = new(), bool createStatus = false)
		{
			AssetLoadStatusGroup loadStatus = createStatus ? new AssetLoadStatusGroup() : null;
			var task = LoadSceneByRefer(sceneAddress, parameters, loadStatus);
			var status = new AsyncLoadingStatus<Scene>(sceneAddress, task, loadStatus);
			return status;
		}

		/// <summary>
		/// 带引用计数卸载资源
		/// </summary>
		/// <param name="status"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static Task UnLoadAssetByReferWrapped<T>(AsyncLoadingStatus<T> status)
		{
			var task = Consumer.UnLoadAssetByRefer(status.Address);
			return task;
		}

		/// <summary>
		/// 带引用计数卸载场景
		/// </summary>
		/// <param name="status"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public static async Task<Scene> UnLoadSceneByReferWrapped(AsyncLoadingStatus<Scene> status,
			UnloadSceneOptions options = UnloadSceneOptions.None)
		{
			await Consumer.UnLoadSceneByRefer(status.Address, options);
			var scene = status.Result;
			return scene;
		}

		/// <summary>
		/// 带引用计数卸载资源
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static Task UnLoadAssetByRefer(string address)
		{
			return Consumer.UnLoadAssetByRefer(address);
		}

		/// <summary>
		/// 带引用计数卸载资源
		/// </summary>
		/// <param name="sceneAddress"></param>
		/// <param name="options"></param>
		/// <returns></returns>
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

		public static AsyncLoadingStatus<GameObject> InstantiateAsync(string key, bool createStatus = false)
		{
			var status = LoadAssetByReferWrapped<GameObject>(key, createStatus);

			async Task<GameObject> Load()
			{
				var asset = await status.Task;
				var obj = GameObject.Instantiate(asset);
				return obj;
			}

			var task = Load();
			var status2 = new AsyncLoadingStatus<GameObject>(status, task);
			return status2;
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

		public static bool ExistAddress(string address)
		{
#if UNITY_EDITOR
			if (!IsValid())
			{
				return false;
			}
#endif
			return Consumer.ExistAddress(address);
		}

		public static bool IsValid()
		{
			return Consumer != null;
		}
	}
}