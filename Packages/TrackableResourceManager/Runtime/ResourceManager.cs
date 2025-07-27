using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace TrackableResourceManager.Runtime
{
	// public class ModuleResourceAccessKeys
	// {
	// 	public static readonly ResourceKey CheckInBgImage = new ResourceKey("@eeeeeeeeee:CheckInBgImage");
	// }

	// internal class ResourceManager
	// {
	// 	public static readonly ResourceManager Inst = new();
	//
	// 	public Task<T> LoadAsync<T>(string resUri)
	// 	{
	// 		return Addressables.LoadAssetAsync<T>(resUri).Task;
	// 	}
	//
	// 	public void Unload<T>(T obj)
	// 	{
	// 		Addressables.Release<T>(obj);
	// 	}
	//
	// 	public Task<GameObject> InstantiateAsync<T>(string resUri)
	// 	{
	// 		return Addressables.InstantiateAsync(resUri).Task;
	// 	}
	//
	// 	public void ReleaseInstance(GameObject obj)
	// 	{
	// 		Addressables.ReleaseInstance(obj);
	// 	}
	// }

	public interface ILoadAsyncOp
	{
		public bool IsLoaded();
	}

	public class LoadAsyncOp<T> : ILoadAsyncOp
	{
		public int ReferCount;

		public LoadAsyncOp(Task<T> task)
		{
			ReferCount = 0;
			Task = task;
		}

		public Task<T> Task { get; internal set; }

		public bool IsLoaded()
		{
			return Task.IsCompletedSuccessfully;
		}
	}
}