using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrackableResourceManager.Runtime
{
	public class AASceneManager
	{
		public static readonly AASceneManager Inst = new();

		private readonly ResourceSet _resourceSet = new();

		public Task<Scene> LoadSceneAsync(ResourceKey key,
			LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
		{
			return _resourceSet.LoadScene(key, loadMode, activateOnLoad, priority);
		}

		public Task UnloadSceneAsync(ResourceKey resKey)
		{
			return _resourceSet.UnloadScene(resKey);
		}
	}
}