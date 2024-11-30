using System.Collections.Generic;
using System.Threading.Tasks;
using MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiiAsset.Runtime
{
	public interface IAssetBundleConsumer : IAssetProvider
	{
		public Task Init();
	}

	public class AssetBundleConsumer : IAssetBundleConsumer
	{
		protected IAssetProvider Provider;

		public async Task Init()
		{
			var config = AssetConsumerConfig.Load();
			await this.Init(config);
			Resources.UnloadAsset(config);
		}

		public async Task Init(AssetConsumerConfig config)
		{
			Provider = new BundledAssetProvider();
			await Provider.Init(config);
		}

		public async Task<bool> Init(IAssetProvider.IProviderInitOptions options)
		{
			Provider = new BundledAssetProvider();
			return await Provider.Init(options);
		}

		public Task<PipelineResult> UpdateCatalog(string remoteBaseUri)
		{
			return Provider.UpdateCatalog(remoteBaseUri);
		}

		public Task<PipelineResult> LoadLocalCatalog()
		{
			return Provider.LoadLocalCatalog();
		}

		public bool AllowTags(string[] tags)
		{
			return Provider.AllowTags(tags);
		}

		public Task<bool> LoadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			return Provider.LoadTags(tags, loadStatus);
		}

		public Task<bool> DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			return Provider.DownloadTags(tags, loadStatus);
		}

		public Task UnLoadTags(string[] tags)
		{
			return Provider.UnLoadTags(tags);
		}

		public long GetDownloadSize(IEnumerable<string> tags)
		{
			return Provider.GetDownloadSize(tags);
		}

		public bool IsAddressInTags(string address, IEnumerable<string> tags)
		{
			return Provider.IsAddressInTags(address, tags);
		}

		public bool IsBundleInTags(string bundleFileName, IEnumerable<string> tags)
		{
			return Provider.IsBundleInTags(bundleFileName, tags);
		}

		public Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus)
		{
			return Provider.LoadAssetJust<T>(address, loadStatus);
		}

		public Task UnloadAssetJust(string address)
		{
			return Provider.UnloadAssetJust(address);
		}

		public Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus)
		{
			return Provider.LoadAsset<T>(address, loadStatus);
		}

		public Task UnLoadAsset(string address)
		{
			return Provider.UnLoadAsset(address);
		}

		public string GetAddressFromGuid(string guid)
		{
			return Provider.GetAddressFromGuid(guid);
		}

		public bool ExistAddress(string address)
		{
			return Provider.ExistAddress(address);
		}

		public bool ExistGuid(string guid)
		{
			return Provider.ExistGuid(guid);
		}

		public Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus)
		{
			return Provider.LoadAssetByRefer<T>(address, loadStatus);
		}

		public Task UnLoadAssetByRefer(string address)
		{
			return Provider.UnLoadAssetByRefer(address);
		}

		public Task<Scene> LoadScene(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus)
		{
			return Provider.LoadScene(sceneAddress, parameters, loadStatus);
		}

		public Task UnLoadScene(string sceneAddress, UnloadSceneOptions options)
		{
			return Provider.UnLoadScene(sceneAddress, options);
		}

		public Task<Scene> LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus)
		{
			return Provider.LoadSceneByRefer(sceneAddress, parameters, loadStatus);
		}

		public Task UnLoadSceneByRefer(string sceneAddress, UnloadSceneOptions options)
		{
			return Provider.UnLoadSceneByRefer(sceneAddress, options);
		}

		public Task<PipelineResult> CleanUpOldVersionFiles()
		{
			return Provider.CleanUpOldVersionFiles();
		}

		public bool IsAssetBundlesOfAssetLoaded(string address)
		{
			return Provider.IsAssetBundlesOfAssetLoaded(address);
		}

		public void Dispose()
		{
			this.Provider.Dispose();
		}
	}
}