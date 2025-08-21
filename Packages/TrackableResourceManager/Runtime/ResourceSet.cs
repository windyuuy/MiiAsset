using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiiAsset.Runtime;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace TrackableResourceManager.Runtime
{
	public class ResourceSet : IDisposable, IResourceLoader
	{
		private bool _isDisposed = false;
		public bool IsDisposed => _isDisposed;

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;

		#if !DISABLE_NOREFERCOUNT_API
			foreach (var (cacheKey, _) in Cached)
			{
				if (!SharedCached.TryGetValue(cacheKey, out var op) || (1 != op.ReferCount && 0 == --op.ReferCount))
				{
					if (cacheKey.T == typeof(Scene))
					{
						AssetLoader.UnLoadScene(cacheKey.Key);
					}
					else
					{
						AssetLoader.UnLoadAsset(cacheKey.Key);
					}
				}
			}
		#else
			foreach (var item in Cached)
			{
				if (item.Key.T == typeof(Scene))
				{
					AssetLoader.UnLoadSceneByRefer(item.Key.Key);
				}
				else
				{
					AssetLoader.UnLoadAssetByRefer(item.Key.Key);
				}
			}
		#endif

			Cached.Clear();
			DictionaryPool<CacheKey, ILoadAsyncOp>.Release(Cached);
		}

		~ResourceSet()
		{
			Dispose();
		}

		protected struct CacheKey
		{
			public readonly string Key;
			public readonly Type T;

			public CacheKey(string cacheKey, Type t)
			{
				Key = cacheKey;
				T = t;
			}

			public override int GetHashCode()
			{
				return Key.GetHashCode() ^ T.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				if (obj is CacheKey cacheKey)
					return this.Key == cacheKey.Key && this.T == cacheKey.T;
				return false;
			}
		}

		protected readonly Dictionary<CacheKey, ILoadAsyncOp> Cached = DictionaryPool<CacheKey, ILoadAsyncOp>.Get();
		protected static readonly Dictionary<CacheKey, ILoadAsyncOp> SharedCached = new();

		public Task<T> LoadAsset<T>(ResourceKey resKey) where T : UnityEngine.Object
		{
			var resUri = resKey.Key;
			return LoadAsset<T>(resUri);
		}

		public Task<T> LoadAsset<T>(string resUri) where T : UnityEngine.Object
		{
			var key = new CacheKey(resUri, typeof(T));
			if (!Cached.TryGetValue(key, out var op))
			{
			#if !DISABLE_NOREFERCOUNT_API
				if (!SharedCached.TryGetValue(key, out op))
				{
				#if UNITY_EDITOR
					if (!AssetLoader.IsValid())
					{
						var task = Task.FromResult<T>(default);
						op = new LoadAsyncOp<T>(task);
						SharedCached.Add(key, op);
					}
					else
				#endif
					{
						var task = AssetLoader.LoadAsset<T>(resUri);
						op = new LoadAsyncOp<T>(task);
						SharedCached.Add(key, op);
					}
				}
			#else
				var task = AssetLoader.LoadAssetByRefer<T>(resUri);
				op = new LoadAsyncOp<T>(task);
			#endif

				++op.ReferCount;

				Cached.Add(key, op);
			}

			return (Task<T>)op.Task;
		}

		public Task UnLoadAsset<T>(ResourceKey resKey) where T : UnityEngine.Object
		{
			var resUri = resKey.Key;
			return UnLoadAsset<T>(resUri);
		}

		public Task UnLoadAsset<T>(string resUri) where T : UnityEngine.Object
		{
			var key = new CacheKey(resUri, typeof(T));
			if (Cached.Remove(key))
			{
			#if !DISABLE_NOREFERCOUNT_API
				if (SharedCached.TryGetValue(key, out var op) && op.ReferCount == 1)
				{
					op.ReferCount--;
					return AssetLoader.UnLoadAsset(resUri);
				}
				else
				{
					Debug.LogError($"resource not in set0: {resUri}");
				}
			#else
				return AssetLoader.UnLoadAssetByRefer(resUri);
			#endif
			}
			else
			{
				Debug.LogError($"resource not in set1: {resUri}");
			}

			return Task.FromResult<T>(default);
		}

		public Task<Scene> LoadScene(ResourceKey resKey, LoadSceneMode loadMode, bool activateOnLoad = true,
			int priority = 100)
		{
			var resUri = resKey.Key;
			var key = new CacheKey(resUri, typeof(Scene));
			if (!Cached.TryGetValue(key, out var op))
			{
			#if !DISABLE_NOREFERCOUNT_API
				if (!SharedCached.TryGetValue(key, out op))
				{
					var task = AssetLoader.LoadScene(resUri);
					op = new LoadAsyncOp<Scene>(task);
					SharedCached.Add(key, op);
				}
			#else
				var task = AssetLoader.LoadSceneByRefer(resUri);
				op = new LoadAsyncOp<Scene>(task);
			#endif

				op.ReferCount++;
				Cached.Add(key, op);
			}

			return (Task<Scene>)op.Task;
		}

		public Task UnloadScene(ResourceKey resKey)
		{
			var resUri = resKey.Key;
			var key = new CacheKey(resUri, typeof(Scene));
			if (Cached.Remove(key))
			{
			#if !DISABLE_NOREFERCOUNT_API
				if (SharedCached.TryGetValue(key, out var op) && op.ReferCount == 1)
				{
					--op.ReferCount;
					return AssetLoader.UnLoadScene(resUri);
				}
				else
				{
					Debug.LogError($"resource not in set0: {resUri}");
				}
			#else
				return AssetLoader.UnLoadSceneByRefer(resUri);
			#endif
			}
			else
			{
				Debug.LogError($"resource not in set1: {resUri}");
			}

			return Task.CompletedTask;
		}

		public bool IsAllLoaded()
		{
			bool isAllLoaded = true;
			foreach (var (_, item) in Cached)
			{
				if (item.IsLoaded())
				{
					isAllLoaded = false;
					break;
				}
			}

			// var isAllLoaded = Cached.Values.All(item => item.IsLoaded());
			return isAllLoaded;
		}
	}
}