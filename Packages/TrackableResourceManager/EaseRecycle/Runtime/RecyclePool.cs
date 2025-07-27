using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace EaseRecycle
{
	public class RecyclePool : IDisposable
	{
		public Dictionary<string, ObjectPool<GameObject>> NodePools = new();

		private List<RecycleMark> _recycleMarks = new();
		public Transform RecycleRoot = GetOrCreateRecycleRoot();

		private readonly Dictionary<string, GameObject> _tempDict = new();
		private ObjectPool<GameObject> KeepingPool = new(() => throw new NotImplementedException());

		public static Transform SharedRecycleRoot;

		public static Transform GetOrCreateRecycleRoot()
		{
			if (SharedRecycleRoot == null)
			{
				SharedRecycleRoot= new GameObject("SharedRecyclePool").transform;
				Object.DontDestroyOnLoad(SharedRecycleRoot);
				SharedRecycleRoot.gameObject.SetActive(false);
			}

			return SharedRecycleRoot;
		}

		public virtual void LoadPrefab(GameObject prefab, string key = null)
		{
			prefab.GetComponentsInChildren(true, _recycleMarks);
			if (_recycleMarks.Count == 0)
			{
				if (!string.IsNullOrEmpty(key))
				{
					var uid = key;
					if (!NodePools.TryGetValue(uid, out var nodePool))
					{
						// 如果之前未建池，则此时建池
						var child = prefab;
						_tempDict.Add(uid, child);
						nodePool = new ObjectPool<GameObject>(() => GameObject.Instantiate(child));
						NodePools.Add(uid, nodePool);
					}
				}
			}
			else if (_recycleMarks.Count == 1 && _recycleMarks[0].gameObject == prefab)
			{
				string uid;
				if (string.IsNullOrEmpty(key))
				{
					uid = _recycleMarks[0].Uid;
				}
				else
				{
					uid = key;
				}

				if (!NodePools.TryGetValue(uid, out var nodePool))
				{
					// 如果之前未建池，则此时建池
					var child = prefab;
					_tempDict.Add(uid, child);
					nodePool = new ObjectPool<GameObject>(() => GameObject.Instantiate(child));
					NodePools.Add(uid, nodePool);
				}
			}
			else
			{
				var gameObject = GameObject.Instantiate(prefab, RecycleRoot);
				gameObject.name = prefab.name;
				Put(gameObject, key);


				if (string.IsNullOrEmpty(key) && gameObject.GetComponent<RecycleMark>() == null)
				{
					// destroy left part
#if UNITY_EDITOR
					if (Application.isPlaying)
					{
#endif
						GameObject.Destroy(gameObject);
#if UNITY_EDITOR
					}
					else
					{
						GameObject.DestroyImmediate(gameObject);
					}
#endif
				}
			}
		}

		public virtual void Put(GameObject gameObject, string key)
		{
			gameObject.GetComponentsInChildren(true, _recycleMarks);
			RecycleMark rootMark = null;
			foreach (var recycleMark in _recycleMarks)
			{
				if (recycleMark.gameObject == gameObject)
				{
					rootMark = recycleMark;
				}
				recycleMark.transform.SetParent(RecycleRoot);
			}

			foreach (var recycleMark in _recycleMarks)
			{
				string uid;
				if (recycleMark == rootMark)
				{
					if (string.IsNullOrEmpty(key))
					{
						uid = recycleMark.Uid;
					}
					else
					{
						uid = key;
					}
				}
				else
				{
					uid = recycleMark.Uid;
				}

				if (!NodePools.TryGetValue(uid, out var nodePool))
				{
					// 如果之前未建池，则此时建池
					var child = recycleMark.gameObject;
					_tempDict.Add(uid, child);
					KeepingPool.Release(child);
					nodePool = new ObjectPool<GameObject>(() => GameObject.Instantiate(child));
					NodePools.Add(uid, nodePool);
				}
				else
				{
					nodePool.Release(recycleMark.gameObject);
				}
			}

			if (rootMark == null && !string.IsNullOrEmpty(key))
			{
				var uid = key;
				if (!NodePools.TryGetValue(key, out var nodePool))
				{
					// 如果之前未建池，则此时建池
					var child = gameObject;
					_tempDict.Add(uid, child);
					KeepingPool.Release(child);
					nodePool = new ObjectPool<GameObject>(() => GameObject.Instantiate(child));
					NodePools.Add(uid, nodePool);
				}
				else
				{
					nodePool.Release(gameObject);
				}
			}

			_recycleMarks.Clear();
		}

		public virtual GameObject Get(string key, Func<GameObject> createFunc = null)
		{
			if (!NodePools.TryGetValue(key, out var nodePool))
			{
				Debug.Assert(createFunc != null, "para createFunc cannot be null");
				nodePool = new ObjectPool<GameObject>(createFunc);
				NodePools.Add(key, nodePool);
			}

			return nodePool.Get();
		}

		public virtual void Dispose()
		{
			KeepingPool?.Dispose();
			foreach (var nodePoolsValue in this.NodePools.Values)
			{
				nodePoolsValue.Dispose();
			}

			NodePools.Clear();
			_tempDict.Clear();
			_recycleMarks.Clear();
		}
	}
}