using System.Threading.Tasks;
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

		public Task LoadTags(string[] tags)
		{
			return Provider.LoadTags(tags);
		}

		public Task UnLoadTags(string[] tags)
		{
			return Provider.UnLoadTags(tags);
		}

		public Task<T> LoadAssetJust<T>(string address)
		{
			return Provider.LoadAssetJust<T>(address);
		}

		public Task UnloadAssetJust(string address)
		{
			return Provider.UnloadAssetJust(address);
		}

		public Task<T> LoadAsset<T>(string address)
		{
			return Provider.LoadAsset<T>(address);
		}

		public Task UnLoadAsset(string address)
		{
			return Provider.UnLoadAsset(address);
		}

		public Task<T> LoadAssetByRefer<T>(string address)
		{
			return Provider.LoadAssetByRefer<T>(address);
		}

		public Task UnLoadAssetByRefer(string address)
		{
			return Provider.UnLoadAssetByRefer(address);
		}

		public Task<Scene> LoadScene(string sceneAddress, LoadSceneParameters parameters)
		{
			return Provider.LoadScene(sceneAddress,parameters);
		}

		public Task UnLoadScene(string sceneAddress)
		{
			return Provider.UnLoadScene(sceneAddress);
		}

		public Task LoadSceneByRefer(string sceneAddress)
		{
			return Provider.LoadSceneByRefer(sceneAddress);
		}

		public Task UnLoadSceneByRefer(string sceneAddress)
		{
			return Provider.UnLoadSceneByRefer(sceneAddress);
		}
	}
}