using System.Threading.Tasks;
using UnityEngine;

namespace TrackableResourceManager.Runtime
{
	public interface IWithResourceSet
	{
		ResourceSet Token { get; }
	}

	public interface IResourceScope : IWithResourceSet
	{
	}

	public static class ResourceScopeExt
	{
		public static Task<T> LoadAsset<T>(this IResourceScope wrap, ResourceKey resKey) where T : UnityEngine.Object
		{
			return wrap.Token.LoadAsset<T>(resKey);
		}

		public static Task<T> LoadAsset<T>(this IResourceScope wrap, string resKey) where T : UnityEngine.Object
		{
			return wrap.Token.LoadAsset<T>(resKey);
		}

		public static Task UnLoadAsset<T>(this IResourceScope wrap, ResourceKey resKey) where T : UnityEngine.Object
		{
			return wrap.Token.UnLoadAsset<T>(resKey);
		}

		public static Task UnLoadAsset<T>(this IResourceScope wrap, string resKey) where T : UnityEngine.Object
		{
			return wrap.Token.UnLoadAsset<T>(resKey);
		}

		public static Task<GameObject> Instantiate(this IResourceScope wrap, string key)
		{
			return wrap.Instantiate(key, null);
		}

		public static async Task<GameObject> Instantiate(this IResourceScope wrap, string key, Transform parent)
		{
			var asset = await wrap.LoadAsset<GameObject>(key);
			var obj = GameObject.Instantiate(asset, parent);
			return obj;
		}

		public static async Task<bool> ReleaseInstance(this IResourceScope wrap, string key, GameObject gameObject)
		{
		#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				GameObject.DestroyImmediate(gameObject);
			}
			else
		#endif
			{
				GameObject.Destroy(gameObject);
			}

			await wrap.UnLoadAsset<GameObject>(key);
			return true;
		}
	}
}