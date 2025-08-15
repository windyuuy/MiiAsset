using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiiAsset.Runtime;
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
			foreach (var item in Cached)
			{
				if (item.Key.T == typeof(Scene))
				{
					AssetLoader.UnLoadScene(item.Key.Key);
				}
				else
				{
					AssetLoader.UnLoadAsset(item.Key.Key);
				}
			}
		#endif
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

		protected readonly Dictionary<CacheKey, ILoadAsyncOp> Cached = new();

		public Task<T> Load<T>(ResourceKey resKey) where T : UnityEngine.Object
		{
		#if !DISABLE_NOREFERCOUNT_API
			var resUri = resKey.Key;
			return Load<T>(resUri);
		#else
			throw new NotImplementedException();
		#endif
		}

		public Task<T> Load<T>(string resUri) where T : UnityEngine.Object
		{
		#if !DISABLE_NOREFERCOUNT_API
			var key = new CacheKey(resUri, typeof(T));
			if (!Cached.TryGetValue(key, out var op))
			{
#if UNITY_EDITOR
				if (!AssetLoader.IsValid())
				{
					var task = Task.FromResult<T>(default);
					op = new LoadAsyncOp<T>(task);
					Cached.Add(key, op);
				}
				else
#endif
				{
					var task = AssetLoader.LoadAsset<T>(resUri);
					op = new LoadAsyncOp<T>(task);
					Cached.Add(key, op);
				}
			}

			var loadAsyncOp = ((LoadAsyncOp<T>)op);
			loadAsyncOp.ReferCount++;
			return loadAsyncOp.Task;
		#else
			throw new NotImplementedException();
		#endif
		}

		public Task UnLoad<T>(ResourceKey resKey) where T : UnityEngine.Object
		{
		#if !DISABLE_NOREFERCOUNT_API
			var resUri = resKey.Key;
			return UnLoad<T>(resUri);
		#else
			throw new NotImplementedException();
		#endif
		}

		public Task UnLoad<T>(string resUri) where T : UnityEngine.Object
		{
		#if !DISABLE_NOREFERCOUNT_API
			var key = new CacheKey(resUri, typeof(T));
			if (Cached.TryGetValue(key, out var op))
			{
				var loadAsyncOp = ((LoadAsyncOp<T>)op);
				loadAsyncOp.ReferCount--;

				if (loadAsyncOp.ReferCount == 0)
				{
					return AssetLoader.UnLoadAsset(resUri);
				}
			}

			return Task.FromResult<T>(default);
		#else
			throw new NotImplementedException();
		#endif
		}

		public Task<Scene> LoadScene(ResourceKey resKey, LoadSceneMode loadMode, bool activateOnLoad = true,
			int priority = 100)
		{
		#if !DISABLE_NOREFERCOUNT_API
			var resUri = resKey.Key;
			var key = new CacheKey(resUri, typeof(Scene));
			if (!Cached.TryGetValue(key, out var op))
			{
				var task = AssetLoader.LoadScene(resUri);
				op = new LoadAsyncOp<Scene>(task);
				Cached.Add(key, op);
			}

			var loadAsyncOp = ((LoadAsyncOp<Scene>)op);
			loadAsyncOp.ReferCount++;
			return loadAsyncOp.Task;
		#else
			throw new NotImplementedException();
		#endif
		}

		public Task UnloadScene(ResourceKey resKey)
		{
		#if !DISABLE_NOREFERCOUNT_API
			var resUri = resKey.Key;
			var key = new CacheKey(resUri, typeof(Scene));
			if (Cached.TryGetValue(key, out var op))
			{
				var loadAsyncOp = ((LoadAsyncOp<Scene>)op);
				loadAsyncOp.ReferCount--;

				if (loadAsyncOp.ReferCount == 0)
				{
					return AssetLoader.UnLoadScene(resUri);
				}
			}

			return Task.CompletedTask;
		#else
			throw new NotImplementedException();
		#endif
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