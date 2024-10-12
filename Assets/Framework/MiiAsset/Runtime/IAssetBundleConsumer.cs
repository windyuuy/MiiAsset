using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework.MiiAsset.Runtime
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
			var config = Resources.Load<AssetConsumerConfig>("MiiConfig/ConsumerConfig");
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

		public bool AllowTags(string[] tags)
		{
			return Provider.AllowTags(tags);
		}

		public Task LoadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			return Provider.LoadTags(tags, loadStatus);
		}

		public Task DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus)
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

		public Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus) where T : Object
		{
			return Provider.LoadAssetJust<T>(address, loadStatus);
		}

		public Task UnloadAssetJust(string address)
		{
			return Provider.UnloadAssetJust(address);
		}

		public Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus) where T : Object
		{
			return Provider.LoadAsset<T>(address, loadStatus);
		}

		public Task UnLoadAsset(string address)
		{
			return Provider.UnLoadAsset(address);
		}

		public Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus) where T : Object
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
	}
}