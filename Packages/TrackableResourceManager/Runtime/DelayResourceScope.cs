using System;
using System.Collections;
using MonoExtLib.Loom;

namespace TrackableResourceManager.Runtime
{
	public readonly struct DelayResourceScope : IDisposable, IResourceScope
	{
		private readonly ResourceSet _resourceSet;

		public DelayResourceScope(ResourceSet set = null)
		{
			_resourceSet = set ?? new();
		}

		private void DelayDispose(IEnumerator delayer = null)
		{
			LoomMG.SharedLoom.StartCoroutine(DelayDisposeCo(delayer));
		}

		private IEnumerator DelayDisposeCo(IEnumerator delayer)
		{
			yield return delayer;
			_resourceSet.Dispose();
		}

		public void Dispose()
		{
			DelayDispose();
		}

		ResourceSet IWithResourceSet.Token => _resourceSet;
	}
}