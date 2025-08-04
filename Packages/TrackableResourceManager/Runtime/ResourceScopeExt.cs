using System.Threading.Tasks;
using UnityEngine;

namespace TrackableResourceManager.Runtime
{
	public interface IWithResourceSet
	{
		ResourceSet Token { get; }
	}

	public interface IResourceScope: IWithResourceSet
	{
		
	}
	
	public static class ResourceScopeExt
	{
		public static Task<T> LoadAsync<T>(this IResourceScope wrap, ResourceKey resKey) where T : UnityEngine.Object
		{
			return wrap.Token.Load<T>(resKey);
		}

		public static Task UnLoad<T>(this IResourceScope wrap, ResourceKey resKey) where T : UnityEngine.Object
		{
			return wrap.Token.UnLoad<T>(resKey);
		}

	}
}