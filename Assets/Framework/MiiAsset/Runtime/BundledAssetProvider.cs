using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.AssetUtils;
using Framework.MiiAsset.Runtime.IOManagers;
using Framework.MiiAsset.Runtime.Pipelines;
using Framework.MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework.MiiAsset.Runtime
{
	public class BundledAssetProvider : IAssetProvider
	{
		public string CatalogName;
		public string InternalBaseUri;
		public string ExternalBaseUri;
		public string RemoteBaseUri;

		public PipelineResult Result;

		public IAssetProvider Init(string internalBaseUri, string externalBaseUri)
		{
			Result = new();
#if UNITY_EDITOR
			this.InternalBaseUri = AssetHelper.GetInternalBuildPath();
#else
			this.InternalBaseUri = Application.dataPath + "/" + internalBaseUri;
#endif
			IOManager.LocalIOProto.InternalDir = this.InternalBaseUri;
			this.ExternalBaseUri = Application.persistentDataPath + "/" + externalBaseUri;
			return this;
		}

		protected Task<PipelineResult> LoadCatalogTask;

		public Task<PipelineResult> UpdateCatalog(string remoteBaseUri, string catalogName)
		{
			if (LoadCatalogTask == null)
			{
				this.RemoteBaseUri = remoteBaseUri;
				this.CatalogName = catalogName;

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

		protected CatalogInfo CatalogInfo = new();

		private void HandleCatalog(CatalogConfig internalCatalog, CatalogConfig externalCatalog, string sourceUri)
		{
			var cacheDir = IOManager.LocalIOProto.CacheDir;
			var isDirReady = false;
			try
			{
				IOManager.LocalIOProto.EnsureDirectory(cacheDir);
				isDirReady = true;
			}
			catch (Exception exception)
			{
				Result.Exception = exception;
				Result.ErrorType = PipelineErrorType.FileSystemError;
			}

			if (isDirReady)
			{
				try
				{
					if (externalCatalog != null)
					{
						var loadSource = new ResourceLoadSource(sourceUri, cacheDir);
						LoadCatalogInfo(externalCatalog, loadSource);

						// merge internal catalog
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

		private void LoadCatalogInfo(CatalogConfig externalCatalog, ResourceLoadSource loadSource)
		{
			CatalogInfo.LoadCatalogInfo(externalCatalog);

			foreach (var bundleInfo in externalCatalog.bundleInfos)
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

		public Task LoadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			AllowTags(tags);
			var task = CatalogStatus.LoadTags(tags, CatalogInfo, loadStatus);
			return task;
		}

		public Task DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			var task = CatalogStatus.DownloadTags(tags, CatalogInfo, loadStatus);
			return task;
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
					result.Print();
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

		public async Task UnLoadScene(string sceneAddress)
		{
			var scene = SceneManager.GetSceneByPath(sceneAddress);
			var op = SceneManager.UnloadSceneAsync(scene);
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

		public async Task UnLoadSceneByRefer(string sceneAddress)
		{
			CatalogInfo.GetAssetDependBundles(sceneAddress, out var deps);
			await CatalogAddressStatus.UnRegisterAsset(sceneAddress);
			var opStand = SceneManager.UnloadSceneAsync(sceneAddress);
			await opStand.GetTask();
			// await UnLoadAssetByRefer(sceneAddress);
			await CatalogStatus.UnLoadBundlesByRefer(deps);
		}

		public Task<PipelineResult> CleanUpOldVersionFiles()
		{
			var cacheDir = IOManager.LocalIOProto.CacheDir;

			var failedList = new List<string>();
			var files = IOManager.LocalIOProto.ReadDir(cacheDir);
			CatalogInfo.BundlesToClean.Clear();
			foreach (var filePath in files)
			{
				var fileName = Path.GetFileName(filePath);
				if (!CatalogInfo.BundleLoadSourceMap.ContainsKey(fileName))
				{
					CatalogInfo.BundlesToClean.Add(fileName);

					try
					{
						IOManager.LocalIOProto.Delete(filePath);
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						failedList.Add(filePath);
					}
				}

				if (CatalogInfo.InternalBundles.ContainsKey(fileName))
				{
					CatalogInfo.BundlesToClean.Add(fileName);

					try
					{
						IOManager.LocalIOProto.Delete(filePath);
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						failedList.Add(filePath);
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
	}
}