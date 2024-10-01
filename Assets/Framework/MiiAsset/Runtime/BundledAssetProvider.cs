using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.AssetUtils;
using Framework.MiiAsset.Runtime.IOManagers;
using Framework.MiiAsset.Runtime.Pipelines;
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

		public IAssetProvider Init(string internalBaseUri, string externalBaseUri)
		{
#if UNITY_EDITOR
			this.InternalBaseUri = AssetHelper.GetInternalBuildPath();
#else
			this.InternalBaseUri = Application.dataPath + "/" + internalBaseUri;
#endif
			this.ExternalBaseUri = Application.persistentDataPath + "/" + externalBaseUri;
			return this;
		}

		protected TaskCompletionSource<bool> LoadCatalogTask;

		public Task UpdateCatalog(string remoteBaseUri, string catalogName)
		{
			if (LoadCatalogTask == null)
			{
				this.RemoteBaseUri = remoteBaseUri;
				this.CatalogName = catalogName;

				this.LoadCatalogTask = new();

				async Task LoadCatalogTask()
				{
					using var pipeline = new UpdateCatalogPipeline().Init(CatalogName, InternalBaseUri, ExternalBaseUri, RemoteBaseUri);
					await pipeline.Run();
					HandleCatalog(pipeline.InternalCatalog, pipeline.ExternalCatalog, pipeline.SourceUri);
					this.LoadCatalogTask.SetResult(true);
				}

				_ = LoadCatalogTask();
			}

			return LoadCatalogTask.Task;
		}

		protected CatalogInfo CatalogInfo = new();

		private void HandleCatalog(CatalogConfig internalCatalog, CatalogConfig externalCatalog, string sourceUri)
		{
			var cacheDir = $"{Application.persistentDataPath}/hotres/";
			IOManager.LocalIOProto.EnsureDirectory(cacheDir);
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
					else
					{
						CatalogInfo.BundlesToClean.Add(bundleInfo.fileName, internalSource);
					}
				}
			}
			else
			{
				var loadSource = new ResourceLoadSource(sourceUri, null);
				LoadCatalogInfo(internalCatalog, loadSource);
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

		public Task LoadTags(string[] tags)
		{
			AllowTags(tags);
			var task = CatalogStatus.LoadTags(tags, CatalogInfo);
			return task;
		}

		public Task UnLoadTags(string[] tags)
		{
			return CatalogStatus.UnloadTags(tags, CatalogInfo);
		}

		public Task<T> LoadAssetJust<T>(string address)
		{
			var bundleLoadStatus = CatalogStatus.GetOrCreateLoadStatusByAddress(address, CatalogInfo);
			return bundleLoadStatus.LoadAssetJust<T>(address);
		}

		public async Task UnloadAssetJust(string address)
		{
			var bundleLoadStatus = CatalogStatus.GetOrCreateLoadStatusByAddress(address, CatalogInfo);
			await bundleLoadStatus.UnLoadAssetJust(address);
		}

		public async Task<T> LoadAsset<T>(string address)
		{
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			await CatalogStatus.LoadBundles(deps, CatalogInfo);
			var asset = await LoadAssetJust<T>(address);
			return asset;
		}

		public async Task UnLoadAsset(string address)
		{
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			await CatalogStatus.LoadBundles(deps, CatalogInfo);
			await UnloadAssetJust(address);
		}

		public async Task LoadScene(string sceneAddress, LoadSceneParameters parameters)
		{
			CatalogInfo.GetAssetDependBundles(sceneAddress, out var deps);
			await CatalogStatus.LoadBundles(deps, CatalogInfo);
			var op = SceneManager.LoadSceneAsync(sceneAddress, parameters);
			await op.GetTask();
		}

		public async Task UnLoadScene(string sceneAddress)
		{
			var op = SceneManager.UnloadSceneAsync(sceneAddress);
			await op.GetTask();
			await UnLoadAsset(sceneAddress);
		}

		protected CatalogAddressStatus CatalogAddressStatus = new();

		public Task<T> LoadAssetByRefer<T>(string address)
		{
			var task = LoadInternal<T>(address);
			CatalogAddressStatus.RegisterAddress(address, task);
			return task;
		}

		async Task<T> LoadInternal<T>(string address)
		{
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			await CatalogStatus.LoadBundlesByRefer(deps, CatalogInfo);
			var asset = await LoadAssetJust<T>(address);
			CatalogAddressStatus.RegisterAsset(address, asset);
			return asset;
		}

		public async Task UnLoadAssetByRefer(string address)
		{
			CatalogInfo.GetAssetDependBundles(address, out var deps);
			await CatalogAddressStatus.UnAddress(address);
			await CatalogStatus.UnLoadBundlesByRefer(deps);
		}

		public async Task LoadSceneByRefer(string sceneAddress)
		{
			await LoadAssetByRefer<object>(sceneAddress);
			var op = SceneManager.LoadSceneAsync(sceneAddress);
			await op.GetTask();
		}

		public async Task UnLoadSceneByRefer(string sceneAddress)
		{
			var op = SceneManager.UnloadSceneAsync(sceneAddress);
			await op.GetTask();
			await UnLoadAssetByRefer(sceneAddress);
		}
	}
}