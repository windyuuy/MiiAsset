using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrackableResourceManager.Runtime
{
	public interface IResourceLoader
	{
		public Task<T> LoadAsset<T>(ResourceKey resKey) where T : UnityEngine.Object;

		public Task UnLoadAsset<T>(ResourceKey resKey) where T : UnityEngine.Object;

		public Task<Scene> LoadScene(ResourceKey resKey,
			LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100);

		public Task UnloadScene(ResourceKey resKey);
	}
}