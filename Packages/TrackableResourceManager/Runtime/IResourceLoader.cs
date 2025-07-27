using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrackableResourceManager.Runtime
{
	public interface IResourceLoader
	{
		public Task<T> Load<T>(ResourceKey resKey) where T : UnityEngine.Object;

		public Task UnLoad<T>(ResourceKey resKey) where T : UnityEngine.Object;

		public Task<Scene> LoadScene(ResourceKey resKey,
			LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100);

		public Task UnloadScene(ResourceKey resKey);
	}
}