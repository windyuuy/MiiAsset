using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework.MiiAsset.Runtime
{
	public interface IAssetBundleConsumer : IAssetProvider
	{
		public void Init();
	}

	public class AssetBundleConsumer : IAssetBundleConsumer
	{
		protected IAssetProvider Provider;

		public void Init()
		{
			var config = Resources.Load<AssetConsumerConfig>("MiiConfig/ConsumerConfig");
			this.Init(config);
			Resources.UnloadAsset(config);
		}

		public void Init(AssetConsumerConfig config)
		{
			Provider = new BundledAssetProvider().Init(config.internalBaseUri, config.externalBaseUri);
		}

		public IAssetProvider Init(string internalBaseUri, string externalBaseUri)
		{
			Provider.Init(internalBaseUri, externalBaseUri);
			return this;
		}

		public Task<PipelineResult> UpdateCatalog(string remoteBaseUri, string catalogName)
		{
			return Provider.UpdateCatalog(remoteBaseUri, catalogName);
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

		public Task UnLoadScene(string sceneAddress)
		{
			return Provider.UnLoadScene(sceneAddress);
		}

		public Task<Scene> LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus)
		{
			return Provider.LoadSceneByRefer(sceneAddress, parameters, loadStatus);
		}

		public Task UnLoadSceneByRefer(string sceneAddress)
		{
			return Provider.UnLoadSceneByRefer(sceneAddress);
		}
	}
}