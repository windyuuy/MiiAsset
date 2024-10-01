using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework.MiiAsset.Runtime
{
	public interface IAssetProvider
	{
		public IAssetProvider Init(string internalBaseUri, string externalBaseUri);

		public Task UpdateCatalog(string remoteBaseUri, string catalogName);

		public bool AllowTags(string[] tags);

		public Task LoadTags(string[] tags);

		public Task UnLoadTags(string[] tags);
		public Task<T> LoadAssetJust<T>(string address);

		public Task UnloadAssetJust(string address);

		public Task<T> LoadAsset<T>(string address);

		public Task UnLoadAsset(string address);

		public Task<T> LoadAssetByRefer<T>(string address);

		public Task UnLoadAssetByRefer(string address);

		public Task LoadScene(string sceneAddress, LoadSceneParameters parameters);

		public Task UnLoadScene(string sceneAddress);

		public Task LoadSceneByRefer(string sceneAddress);

		public Task UnLoadSceneByRefer(string sceneAddress);
	}
}