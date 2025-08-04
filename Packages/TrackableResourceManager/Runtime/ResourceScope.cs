using System;
using System.Collections;
using UnityEngine;
using Object = System.Object;

namespace TrackableResourceManager.Runtime
{
	public readonly struct ResourceScope : IDisposable, IResourceScope
	{
		private readonly ResourceSet _resourceSet;

		public static ResourceScope New => new ResourceScope(null);

		private ResourceScope(ResourceSet set)
		{
			_resourceSet = set ?? new();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			DelayCheck();
#endif
		}

		// 延迟检查是否已释放，否则发出警告
		private void DelayCheck()
		{
			// LoomMG.SharedLoom.StartCoroutine(DelayCheckCo());
		}

		private IEnumerator DelayCheckCo()
		{
			yield return new WaitForEndOfFrame();
			while (!_resourceSet.IsAllLoaded())
			{
				yield return null;
			}

			yield return new WaitForEndOfFrame();
			yield return null;
			yield return null;
			yield return new WaitForEndOfFrame();
			Debug.Assert(_resourceSet.IsDisposed, new Exception("ResourceSet not released"));
			// 未释放则强制释放
			if (!_resourceSet.IsDisposed)
			{
				_resourceSet.Dispose();
			}
		}

		public void Dispose()
		{
			_resourceSet.Dispose();
		}

		/// <summary>
		/// use internal only
		/// </summary>
		ResourceSet IWithResourceSet.Token
		{
			get
			{
#if UNITY_EDITOR
				if (_resourceSet == null)
				{
					throw new Exception("use ResourceScope.New instead !!!");
				}
#endif
				return _resourceSet;
			}
		}
	}
}