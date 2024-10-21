using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MiiAsset.Runtime.IOManagers;
using MiiAsset.Runtime.Pipelines;
using MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiiAsset.Runtime
{
	public class BundledAssetProvider : IAssetProvider
	{
		public string CatalogName;
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
			
			BundleWebSemaphore.Init(options.InitDownloadCoCount, options.MaxDownloadCoCount);

			return result;
		}

		protected Task<PipelineResult> LoadCatalogTask;

		public Task<PipelineResult> UpdateCatalog(string remoteBaseUri)
		{
			if (LoadCatalogTask == null)
			{
				this.RemoteBaseUri = remoteBaseUri;

				async Task<PipelineResult> LoadCatalogInternal()
				{
					using var pipeline = new UpdateCatalogPipeline().Init(CatalogName, InternalBaseUri, ExternalBaseUri, RemoteBaseUri);
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

		protected CatalogInfo CatalogInfo = new();

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
						LoadCatalogInfo(externalCatalog, loadSource);

						// merge internal catalog
						if (internalCatalog != null)
						{
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
						var loadSource = new ResourceLoadSource(sourceUri, null);
						LoadCatalogInfo(internalCatalog, loadSource);
					}

					Result.IsOk = true;
				}
				catch (Exception exception)
				{
					Result.Exception = exception;
					Result.ErrorType = PipelineErrorType.DataIncorrect;
				}
			}
		}

		private void LoadCatalogInfo(CatalogConfig catalog, ResourceLoadSource loadSource)
		{
			CatalogInfo.LoadCatalogInfo(catalog);

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

		public async Task<bool> LoadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			AllowTags(tags);
			var results = await CatalogStatus.LoadTags(tags, CatalogInfo, loadStatus);
			var isOk = results.All(result => result.IsOk);
			return isOk;
		}

		public async Task<bool> DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			var task = CatalogStatus.DownloadTags(tags, CatalogInfo, loadStatus);
			var results = await task;
			var isOk = results.All(result => result.IsOk);
			return isOk;
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

		public Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus)
		{
			var subStatus = loadStatus?.AllocAsyncOperationStatus();
			var bundleLoadStatus = CatalogStatus.GetOrCreateLoadStatusByAddress(address, CatalogInfo);
			return bundleLoadStatus.LoadAssetJust<T>(address, subStatus);
		}

		protected Task<T> LoadAssetJust<T>(string address, AsyncOperationStatus loadStatus)
		{
			var bundleLoadStatus = CatalogStatus.GetOrCreateLoadStatusByAddress(address, CatalogInfo);
			return bundleLoadStatus.LoadAssetJust<T>(address, loadStatus);
		}

		public async Task UnloadAssetJust(string address)
		{
			var bundleLoadStatus = CatalogStatus.GetOrCreateLoadStatusByAddress(address, CatalogInfo);
			await bundleLoadStatus.UnLoadAssetJust(address);
		}

		public async Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus)
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
						result.Print();
					}
				}

				return default(T);
			}
		}

		public async Task UnLoadAsset(string address)
		{
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			await CatalogStatus.GetLoadingBundlesTasks(deps, CatalogInfo);
			await UnloadAssetJust(address);
		}

		public async Task<Scene> LoadScene(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus)
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
						result.Print();
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
				await UnLoadAsset(sceneAddress);
			}
			else
			{
				Debug.LogError($"cannot unload the scene: {sceneAddress}");
			}
		}

		protected CatalogAddressStatus CatalogAddressStatus = new();

		public Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus)
		{
			var task = LoadByReferInternal<T>(address, loadStatus);
			CatalogAddressStatus.RegisterAddress(address, task);
			return task;
		}

		protected async Task<T> LoadByReferInternal<T>(string address, AssetLoadStatusGroup loadStatus)
		{
			var subStatus = loadStatus?.AllocAsyncOperationStatus();
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			await CatalogStatus.LoadBundlesByRefer(deps, CatalogInfo, loadStatus);
			var asset = await LoadAssetJust<T>(address, subStatus);
			CatalogAddressStatus.RegisterAsset(address, asset);
			return asset;
		}

		public async Task UnLoadAssetByRefer(string address)
		{
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			await CatalogAddressStatus.UnRegisterAsset(address);
			await CatalogStatus.UnLoadBundlesByRefer(deps);
		}

		public async Task<Scene> LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus)
		{
			// await LoadAssetByRefer<UnityEngine.Object>(sceneAddress);
			var task = LoadSceneInternal(sceneAddress, parameters, loadStatus);
			CatalogAddressStatus.RegisterAddress(sceneAddress, task);
			var opStand = await task;
			CatalogAddressStatus.RegisterAsset(sceneAddress, opStand);

			var scene = SceneManager.GetSceneByPath(sceneAddress);
			return scene;
		}

		private async Task<AsyncOperation> LoadSceneInternal(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus)
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
			await CatalogAddressStatus.UnRegisterAsset(sceneAddress);
			await CatalogStatus.UnLoadBundlesByRefer(deps);
		}

		public Task<PipelineResult> CleanUpOldVersionFiles()
		{
			var failedList = new List<string>();
			{
				var cacheDir = IOManager.LocalIOProto.CacheDir;
				var files = IOManager.LocalIOProto.ExistsDir(cacheDir)?  IOManager.LocalIOProto.ReadDir(cacheDir): Array.Empty<FilePathInfo>();
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
							Debug.Log($"delete1 {filePath}");
							IOManager.LocalIOProto.Delete(filePath);
						}
						catch (Exception exception)
						{
							Debug.LogException(exception);
							failedList.Add(filePath);
						}
					}
					else if (CatalogInfo.InternalBundles.ContainsKey(fileName))
					{
						CatalogInfo.BundlesToClean.Add(fileName);

						try
						{
							Debug.Log($"delete2 {filePath}");
							IOManager.LocalIOProto.Delete(filePath);
						}
						catch (Exception exception)
						{
							Debug.LogException(exception);
							failedList.Add(filePath);
						}
					}
				}
			}

			if (IOManager.LocalIOProto.IsInternalDirUpdating)
			{
				var internalDir = IOManager.LocalIOProto.InternalDir;
				var files = IOManager.LocalIOProto.ExistsDir(internalDir)? IOManager.LocalIOProto.ReadDir(internalDir):Array.Empty<FilePathInfo>();
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

							Debug.Log($"delete3 {filePath}");
							IOManager.LocalIOProto.Delete(filePath);
						}
						catch (Exception exception)
						{
							Debug.LogException(exception);
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
			return Task.FromResult(result);
		}

		public void Dispose()
		{
			this.CatalogStatus.Dispose();
		}
	}
}