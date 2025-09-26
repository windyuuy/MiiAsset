using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrackableResourceManager.Runtime;
using UnityEngine;

namespace Bundles.UI.Layers.Home.异象
{
	public class GameObjectSharedLoadUtils
	{
		public static readonly GameObjectSharedLoadUtils Shared = new();

		protected class LoadStatus
		{
			public GameObject GameObject;
			public Task<GameObject> LoadObjTask;
			public int ReferCount = 0;
			public int ResReferCount = 0;
			public Action<GameObject> OnLoadFunc;

			public bool IsComplete => LoadObjTask is { IsCompleted: true };

			public void Dispose()
			{
				if (this.GameObject != null)
				{
					GameObject.Destroy(this.GameObject);
					this.GameObject = null;
				}
			}
		}

		private static readonly Dictionary<string, LoadStatus> LoadSharedTaskMap = new();

		public void UnLoad(string resUri, bool immediately = false)
		{
			if (!string.IsNullOrEmpty(resUri))
			{
				if (LoadSharedTaskMap.TryGetValue(resUri, out var loadStatus))
				{
					--loadStatus.ResReferCount;
					if (loadStatus.ResReferCount == 0 && immediately)
					{
						loadStatus.Dispose();
						LoadSharedTaskMap.Remove(resUri);
					}
				}
			}
		}

		public void ApplyUnloads()
		{
			foreach (var (resUri, loadStatus) in LoadSharedTaskMap.ToArray())
			{
				if (loadStatus.ResReferCount == 0)
				{
					loadStatus.Dispose();
					LoadSharedTaskMap.Remove(resUri);
				}
			}
		}

		public void UnRefer(string resUri)
		{
			if (!string.IsNullOrEmpty(resUri))
			{
				if (LoadSharedTaskMap.TryGetValue(resUri, out var loadStatus))
				{
					--loadStatus.ReferCount;
					if (loadStatus.ReferCount == 0)
					{
						if (loadStatus.GameObject != null)
						{
							loadStatus.GameObject.SetActive(false);
						}
					}
				}
			}
		}

		public void Refer(string resUri)
		{
			if (!string.IsNullOrEmpty(resUri))
			{
				if (LoadSharedTaskMap.TryGetValue(resUri, out var loadStatus))
				{
					++loadStatus.ReferCount;
					if (loadStatus.GameObject != null)
					{
						loadStatus.GameObject.SetActive(loadStatus.ReferCount > 0);
					}
				}
			}
		}

		public async Task<GameObject> Load(string resUri,
			UObjectResourceScope scope, Transform parent, Action<GameObject> onLoadFunc = null)
		{
			if (!string.IsNullOrEmpty(resUri))
			{
				if (!LoadSharedTaskMap.TryGetValue(resUri, out var loadStatus))
				{
					loadStatus = new();
					loadStatus.OnLoadFunc = onLoadFunc;
					++loadStatus.ReferCount;
					++loadStatus.ResReferCount;
					var task = LoadGameObjectTask(resUri, scope, parent, loadStatus);
					loadStatus.LoadObjTask = task;
					LoadSharedTaskMap.Add(resUri, loadStatus);
				}
				else if (loadStatus.IsComplete)
				{
					++loadStatus.ReferCount;
					++loadStatus.ResReferCount;
					if (loadStatus.GameObject != null)
					{
						loadStatus.GameObject.SetActive(loadStatus.ReferCount > 0);
					}

					return loadStatus.GameObject;
				}
				else
				{
					++loadStatus.ReferCount;
					++loadStatus.ResReferCount;
				}

				return await loadStatus.LoadObjTask;
			}

			return null;
		}

		private async Task<GameObject> LoadGameObjectTask(string resUri, UObjectResourceScope scope, Transform parent,
			LoadStatus loadStatus)
		{
			var effect = await scope.LoadAsync<GameObject>(resUri);
			var obj = GameObject.Instantiate(effect, parent);
			obj.SetActive(loadStatus.ReferCount > 0);
			loadStatus.GameObject = obj;
			loadStatus.OnLoadFunc?.Invoke(obj);
			return obj;
		}

		public async Task<T[]> LoadComponents<T>(string resUri,
			UObjectResourceScope scope, Transform parent, Action<GameObject> onLoadFunc = null)
		{
			var obj = await Load(resUri, scope, parent, onLoadFunc);
			var rt2Dt = obj.GetComponents<T>();
			return rt2Dt;
		}
	}
}