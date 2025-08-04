using System;

namespace TrackableResourceManager.Runtime
{
	public readonly struct LifecycleResourceScope : IDisposable, IResourceScope
	{
		private readonly ResourceSet _resourceSet;

		public LifecycleResourceScope(IWithResourceLifecycle lifecycle)
		{
			if (lifecycle != null)
			{
				_resourceSet = new();
				lifecycle.disposeEvent += Dispose;
			}
			else
			{
				throw new Exception("add component failed");
			}
		}

		public void Dispose()
		{
			_resourceSet?.Dispose();
		}

		ResourceSet IWithResourceSet.Token => _resourceSet;
	}
}