using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOManagers;
using MiiAsset.Runtime.Pipelines;
using MiiAsset.Runtime.Status;
using MonoExtLib.AsyncExt;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace MiiAsset.Runtime
{
	public class BundledAssetProvider : IAssetProvider
	{
		public string CatalogName;
		public string CatalogExt;
		public string InternalBaseUri;
		public string ExternalBaseUri;
		public string RemoteBaseUri;

		public PipelineResult Result;

		public async Task<bool> Init(IAssetProvider.IProviderInitOptions options)
		{
			Result = new();
			var result = await IOManager.LocalIOProto.Init(options);
			this.InternalBaseUri = IOManager.LocalIOProto.InternalDir;
			this.ExternalBaseUri = IOManager.LocalIOProto.ExternalDir;
			this.CatalogName = IOManager.LocalIOProto.CatalogName;
			this.CatalogExt = options.CatalogExt;

			BundleWebSemaphore.Init(options.InitDownloadCoCount, options.MaxDownloadCoCount);

			return result;
		}

		protected Task<PipelineResult> LoadCatalogTask;

		public Task<PipelineResult> UpdateCatalog(string remoteBaseUri)
		{
			if (LoadCatalogTask == null || (LoadCatalogTask.IsCompleted && !Result.IsOk))
			{
				this.RemoteBaseUri = remoteBaseUri;

				async Task<PipelineResult> LoadCatalogInternal()
				{
					using var pipeline = new UpdateCatalogPipeline()
						.Init(CatalogName, CatalogExt, InternalBaseUri, ExternalBaseUri, RemoteBaseUri);
					Result = await pipeline.Run();
					if (Result.IsOk)
					{
						HandleCatalog(pipeline.InternalCatalog, pipeline.ExternalCatalog, pipeline.SourceUri);
					}

					return Result;
				}

				LoadCatalogTask = LoadCatalogInternal();
			}

			return LoadCatalogTask;
		}

		public Task<PipelineResult> LoadLocalCatalog()
		{
			return UpdateCatalog(null);
		}
		//
		// private async Task<bool> EnsureStreamingAssets()
		// {
		// 	var results = await Task.WhenAll(CatalogInfo.BundleLoadSourceMap.Select(item => IOManager.LocalIOProto.EnsureStreamingAssets(item.Key)));
		// 	return results.All(r => r);
		// }

		public readonly CatalogInfo CatalogInfo = new();

		public bool TryGetExtraAddressInfo(string address, out ExtraAddressInfo extraAddressInfo)
		{
			return CatalogInfo.TryGetExtraAddressInfo(address, out extraAddressInfo);
		}

		/// <summary>
		/// 获取所有bundle名
		/// </summary>
		/// <returns></returns>
		public string[] GetAllBundleNames()
		{
			return CatalogInfo.GetAllBundleNames();
		}

		/// <summary>
		/// 获取所有bundle文件名
		/// </summary>
		/// <returns></returns>
		public string[] GetAllBundleFileNames()
		{
			return CatalogInfo.GetAllBundleFileNames();
		}

		public AssetBundleInfo[] GetAllAssetBundleInfos()
		{
			return CatalogInfo.GetAllAssetBundleInfos();
		}

		private void HandleCatalog(CatalogConfig internalCatalog, CatalogConfig externalCatalog, string sourceUri)
		{
			var cacheDir = IOManager.LocalIOProto.CacheDir;
			// var isDirReady = false;
			// try
			// {
			// 	IOManager.LocalIOProto.EnsureDirectory(cacheDir);
			// 	isDirReady = true;
			// }
			// catch (Exception exception)
			// {
			// 	Result.Exception = exception;
			// 	Result.ErrorType = PipelineErrorType.FileSystemError;
			// }

			// if (isDirReady)
			{
				try
				{
					if (externalCatalog != null)
					{
						var loadSource = new ResourceLoadSource(sourceUri, cacheDir);
						LoadCatalogInfo(externalCatalog, loadSource, Result);

						// merge internal catalog
						if (internalCatalog != null)
						{
							// cacheUri == null, 表示内部资源无需缓存
							var internalSource = new ResourceLoadSource(InternalBaseUri, null);
							foreach (var bundleInfo in internalCatalog.bundleInfos)
							{
								if (CatalogInfo.BundleLoadSourceMap.ContainsKey(bundleInfo.fileName))
								{
									CatalogInfo.BundleLoadSourceMap[bundleInfo.fileName] = internalSource;
								}

								CatalogInfo.InternalBundles.Add(bundleInfo.fileName, internalSource);
							}
						}
					}
					else
					{
						if (sourceUri == null || IOManager.LocalIOProto.IsInternalAssetsValid)
						{
							sourceUri = IOManager.LocalIOProto.InternalDir;
						}

						// cacheUri == null, 表示内部资源无需缓存
						var internalSource2 = new ResourceLoadSource(sourceUri, null);
						LoadCatalogInfo(internalCatalog, internalSource2, Result);
					}

					if (Result.Exception == null)
					{
						Result.IsOk = true;
					}
				}
				catch (Exception exception)
				{
					Result.Exception = exception;
					Result.ErrorType = PipelineErrorType.DataIncorrect;
				}

				foreach (var resourceLoadSource in CatalogInfo.BundleLoadSourceMap)
				{
					// Debug.Log(
					// 	$"resourceLoadSource: {resourceLoadSource.Key}, {resourceLoadSource.Value.GetSourceUri(resourceLoadSource.Key)}, {resourceLoadSource.Value.GetCacheUri(resourceLoadSource.Key)};");
				}
			}
		}

		private void LoadCatalogInfo(CatalogConfig catalog, ResourceLoadSource loadSource, PipelineResult result)
		{
			CatalogInfo.LoadCatalogInfo(catalog, result);

			foreach (var bundleInfo in catalog.bundleInfos)
			{
				CatalogInfo.BundleLoadSourceMap.Add(bundleInfo.fileName, loadSource);
			}
		}

		protected CatalogStatus CatalogStatus = new();

		public bool AllowTags(string[] tags)
		{
			CatalogStatus.AllowTags(tags, CatalogInfo);
			return true;
		}

		public async Task<PipelineResultGroup> LoadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			AllowTags(tags);
			var results = await CatalogStatus.LoadTags(tags, CatalogInfo, loadStatus);
			var resultGroup = new PipelineResultGroup(results);
			return resultGroup;
		}

		public async Task<PipelineResultGroup> DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			var task = CatalogStatus.DownloadTags(tags, CatalogInfo, loadStatus);
			var results = await task;
			var resultGroup = new PipelineResultGroup(results);
			return resultGroup;
		}

		public Task UnLoadTags(string[] tags)
		{
			return CatalogStatus.UnloadTags(tags, CatalogInfo);
		}

		public long GetDownloadSize(IEnumerable<string> tags)
		{
			return CatalogStatus.GetDownloadSize(tags, CatalogInfo);
		}

		public bool IsAddressInTags(string address, IEnumerable<string> tags)
		{
			return CatalogStatus.IsAddressInTags(address, tags, CatalogInfo);
		}

		public bool IsBundleInTags(string bundleFileName, IEnumerable<string> tags)
		{
			return CatalogStatus.IsAddressInTags(bundleFileName, tags, CatalogInfo);
		}

		public Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus) where T : UnityEngine.Object
		{
			var subStatus = loadStatus?.AllocAsyncOperationStatus();
			// var bundleLoadStatus = CatalogStatus.GetOrCreateLoadStatusByAddress(address, CatalogInfo);
			// if (bundleLoadStatus != null)
			// {
			// 	return bundleLoadStatus.LoadAssetJust<T>(address, subStatus);
			// }
			// else
			// {
			// 	return Task.FromResult<T>(default);
			// }
			return LoadAssetJust<T>(address, subStatus);
		}

		protected Task<T> LoadAssetJust<T>(string address, AsyncOperationStatus loadStatus) where T : UnityEngine.Object
		{
			var bundleLoadStatus = CatalogStatus.GetOrCreateLoadStatusByAddress(address, CatalogInfo);
			if (bundleLoadStatus != null)
			{
				return bundleLoadStatus.LoadAssetJust<T>(address, loadStatus);
			}
			else
			{
				return Task.FromResult<T>(default);
			}
		}

		public Task UnloadAssetJust(string address, Type type)
		{
			var bundleLoadStatus = CatalogStatus.GetOrCreateLoadStatusByAddress(address, CatalogInfo);
			if (bundleLoadStatus != null)
			{
				return bundleLoadStatus.UnLoadAssetJust(address, type);
			}
			else
			{
				return Task.CompletedTask;
			}
		}

		public async Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus) where T : UnityEngine.Object
		{
			var subStatus = loadStatus?.AllocAsyncOperationStatus();
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			var results = await CatalogStatus.LoadBundles(deps, CatalogInfo, loadStatus);
			if (results.All(result => result.IsOk))
			{
				var asset = await LoadAssetJust<T>(address, subStatus);
				return asset;
			}
			else
			{
				foreach (var result in results)
				{
					if (!result.IsOk)
					{
						result.PrintError();
					}
				}

				return default(T);
			}
		}

		public async Task UnLoadAsset(string address, Type type)
		{
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			await CatalogStatus.GetLoadingBundlesTasks(deps, CatalogInfo);
			await UnloadAssetJust(address, type);
			await CatalogStatus.UnLoadBundles(deps);
		}

		public string GetAddressFromGuid(string guid)
		{
			return CatalogInfo.GetAddressFromGuid(guid);
		}

		public bool ExistAddress(string address)
		{
			return CatalogInfo.ExistAddress(address);
		}

		public bool ExistGuid(string address)
		{
			return CatalogInfo.ExistGuid(address);
		}

		public async Task<Scene> LoadScene(string sceneAddress, LoadSceneParameters parameters,
			AssetLoadStatusGroup loadStatus)
		{
			var subStatus = loadStatus?.AllocAsyncOperationStatus();
			CatalogInfo.GetAssetDependBundles(sceneAddress, out var deps);
			var results = await CatalogStatus.LoadBundles(deps, CatalogInfo, loadStatus);
			if (results.All(result => result.IsOk))
			{
				var op = SceneManager.LoadSceneAsync(sceneAddress, parameters);
				subStatus?.Set(op);
				await op.GetTask();
				var scene = SceneManager.GetSceneByPath(sceneAddress);
				return scene;
			}
			else
			{
				foreach (var result in results)
				{
					if (!result.IsOk)
					{
						result.PrintError();
					}
				}

				return default;
			}
		}

		public async Task UnLoadScene(string sceneAddress, UnloadSceneOptions options)
		{
			// var scene = SceneManager.GetSceneByPath(sceneAddress);
			var op = SceneManager.UnloadSceneAsync(sceneAddress, options);
			if (op != null)
			{
				await op.GetTask();
				await UnLoadAsset(sceneAddress, typeof(Scene));
			}
			else
			{
				MyLogger.LogError($"cannot unload the scene: {sceneAddress}");
			}
		}

		protected Task<T> LoadAssetJustSync<T>(string address, SyncOperationStatus loadStatus)
			where T : UnityEngine.Object
		{
			var bundleLoadStatus = CatalogStatus.GetOrCreateLoadStatusByAddress(address, CatalogInfo);
			if (bundleLoadStatus != null)
			{
				return bundleLoadStatus.LoadAssetJustSync<T>(address, loadStatus);
			}
			else
			{
				return Task.FromResult<T>(default);
			}
		}

		public Task<T> LoadAssetByReferSync<T>(string address, AssetLoadStatusGroup loadStatus)
			where T : UnityEngine.Object
		{
			var task = LoadByReferInternalSync<T>(address, loadStatus);
			AddressReferStatus.RegisterAddress(address, task);
			return task;
		}

		protected async Task<T> LoadByReferInternalSync<T>(string address, AssetLoadStatusGroup loadStatus)
			where T : UnityEngine.Object
		{
			var subStatus = loadStatus?.AllocSyncOperationStatus();
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			await CatalogStatus.LoadBundlesByRefer(deps, CatalogInfo, loadStatus);
			var asset = await LoadAssetJustSync<T>(address, subStatus);
			AddressReferStatus.RegisterAsset(address, asset);
			return asset;
		}

		protected AddressReferStatus AddressReferStatus = new();

		public Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus) where T : UnityEngine.Object
		{
			var task = LoadByReferInternal<T>(address, loadStatus);
			AddressReferStatus.RegisterAddress(address, task);
			return task;
		}

		protected async Task<T> LoadByReferInternal<T>(string address, AssetLoadStatusGroup loadStatus)
			where T : UnityEngine.Object
		{
			var subStatus = loadStatus?.AllocAsyncOperationStatus();
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			await CatalogStatus.LoadBundlesByRefer(deps, CatalogInfo, loadStatus);
			var asset = await LoadAssetJust<T>(address, subStatus);
			AddressReferStatus.RegisterAsset(address, asset);
			return asset;
		}

		public async Task UnLoadAssetByRefer(string address, Type type)
		{
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			var needUnloadAssetJust = await AddressReferStatus.UnRegisterAsset(address, type);
			if (needUnloadAssetJust)
			{
				UnloadAssetJust(address, type);
			}

			await CatalogStatus.UnLoadBundlesByRefer(address, deps);
		}

		public async Task<Scene> LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters,
			AssetLoadStatusGroup loadStatus)
		{
			// await LoadAssetByRefer<UnityEngine.Object>(sceneAddress);
			var task = LoadSceneInternal(sceneAddress, parameters, loadStatus);
			AddressReferStatus.RegisterAddress(sceneAddress, task);
			var opStand = await task;
			AddressReferStatus.RegisterAsset(sceneAddress, opStand);

			var scene = SceneManager.GetSceneByPath(sceneAddress);
			return scene;
		}

		private async Task<AsyncOperation> LoadSceneInternal(string sceneAddress, LoadSceneParameters parameters,
			AssetLoadStatusGroup loadStatus)
		{
			var subStatus = loadStatus?.AllocAsyncOperationStatus();
			CatalogInfo.GetAssetDependBundles(sceneAddress, out var deps);
			await CatalogStatus.LoadBundlesByRefer(deps, CatalogInfo, loadStatus);
			var op = SceneManager.LoadSceneAsync(sceneAddress, parameters);
			subStatus?.Set(op);
			await op.GetTask();
			return op;
		}

		public async Task UnLoadSceneByRefer(string sceneAddress, UnloadSceneOptions options)
		{
			CatalogInfo.GetAssetDependBundles(sceneAddress, out var deps);
			var opStand = SceneManager.UnloadSceneAsync(sceneAddress, options);
			await opStand.GetTask();
			// await UnLoadAssetByRefer(sceneAddress);
			var needUnloadAssetJust = await AddressReferStatus.UnRegisterAsset(sceneAddress, typeof(Scene));
			if (needUnloadAssetJust)
			{
				UnloadAssetJust(sceneAddress, typeof(Scene));
			}

			await CatalogStatus.UnLoadBundlesByRefer(sceneAddress, deps);
		}

		public Task<PipelineResult> CleanUpOldVersionFiles()
		{
			var failedList = ListPool<string>.Get();
			{
				var cacheDir = IOManager.LocalIOProto.CacheDir;
				var files = IOManager.LocalIOProto.ExistsDir(cacheDir)
					? IOManager.LocalIOProto.ReadDir(cacheDir)
					: Array.Empty<FilePathInfo>();
				CatalogInfo.BundlesToClean.Clear();
				foreach (var fileInfo in files)
				{
					var fileName = fileInfo.FileName;
					var filePath = fileInfo.FilePath;
					if (!CatalogInfo.BundleLoadSourceMap.ContainsKey(fileName))
					{
						CatalogInfo.BundlesToClean.Add(fileName);

						try
						{
							MyLogger.LogInfo($"delete1 {filePath}");
							IOManager.LocalIOProto.Delete(filePath);
						}
						catch (Exception exception)
						{
							MyLogger.LogException(exception, "e24");
							failedList.Add(filePath);
						}
					}
					else if (CatalogInfo.InternalBundles.ContainsKey(fileName))
					{
						CatalogInfo.BundlesToClean.Add(fileName);

						try
						{
							MyLogger.LogInfo($"delete2 {filePath}");
							IOManager.LocalIOProto.Delete(filePath);
						}
						catch (Exception exception)
						{
							MyLogger.LogException(exception, "e25");
							failedList.Add(filePath);
						}
					}
				}
			}

			if (IOManager.LocalIOProto.IsInternalDirUpdating)
			{
				var internalDir = IOManager.LocalIOProto.InternalDir;
				var files = IOManager.LocalIOProto.ExistsDir(internalDir)
					? IOManager.LocalIOProto.ReadDir(internalDir)
					: Array.Empty<FilePathInfo>();
				foreach (var fileInfo in files)
				{
					var fileName = fileInfo.FileName;
					if (!fileName.EndsWith(".bundle"))
					{
						continue;
					}

					var filePath = fileInfo.FilePath;
					if (!CatalogInfo.BundleLoadSourceMap.ContainsKey(fileName))
					{
						try
						{
							CatalogInfo.BundlesToClean.Add(fileName);

							MyLogger.LogInfo($"delete3 {filePath}");
							IOManager.LocalIOProto.Delete(filePath);
						}
						catch (Exception exception)
						{
							MyLogger.LogException(exception, "e26");
							failedList.Add(filePath);
						}
					}
				}
			}

			var result = new PipelineResult();
			result.IsOk = failedList.Count == 0;
			if (!result.IsOk)
			{
				result.Msg = $"delete failed list: {string.Join(",", failedList)}";
			}

			result.Status = PipelineStatus.Done;

			failedList.Clear();
			ListPool<string>.Release(failedList);

			return Task.FromResult(result);
		}

		public bool IsAssetBundlesOfAssetLoaded(string address)
		{
			var dependBundles = this.GetAssetDependBundles(address);
			var isAllLoaded = dependBundles
				.All(bundleName => this.CatalogStatus.IsBundleLoadedImmediate(bundleName));
			return isAllLoaded;
		}

		public IAssetBundleStatus GetBundleStatus(string bundleName)
		{
			return this.CatalogStatus.GetStatus(bundleName);
		}

		public LoadAddressStatus GetAddressStatus<T>(string address) where T : UnityEngine.Object
		{
			return AddressReferStatus.GetAddressStatus<T>(address);
		}

		public HashSet<string> GetAssetDependBundles(string address)
		{
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			return deps;
		}

		public Dictionary<string, IAssetBundleStatus> GetAllBundleLoadStatusesMap()
		{
			return CatalogStatus.BundleLoadStatus;
		}

		public IAssetBundleStatus[] GetAllBundleLoadStatuses()
		{
			return CatalogStatus.BundleLoadStatus.Values.ToArray();
		}

		public AssetBundle[] GetAllLoadedAssetBundles()
		{
			return CatalogStatus.BundleLoadStatus
				.Select(p => p.Value.AssetBundle)
				.Where(assetBundle => assetBundle != null)
				.ToArray();
		}

		public void RunDelayedTasks()
		{
			this.CatalogStatus.RunDelayedTasks();
		}

		public Task<bool> CleanAllCaches()
		{
			return IOManager.LocalIOProto.CleanAllFileCaches();
		}

		public void Dispose()
		{
			UniAsyncUtils.SetTimeout(DelayDispose);
		}

		private void DelayDispose(float _)
		{
			CatalogStatus.Dispose();
		}
	}
}