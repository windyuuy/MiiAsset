using System;
using UnityEngine;

namespace TrackableResourceManager.Runtime
{
	public readonly struct UObjectResourceScope : IDisposable, IResourceScope
	{
		private readonly ResourceSet _resourceSet;
		private readonly ResourceScopeComp _comp;

		public UObjectResourceScope(Component component)
		{
			_comp = component.gameObject.AddComponent<ResourceScopeComp>();
			if (_comp != null)
			{
				_resourceSet = new();
				_comp.SetScope(this);
			}
			else
			{
				throw new Exception("add component failed");
			}
		}

		public UObjectResourceScope(GameObject gameObject)
		{
			_comp = gameObject.AddComponent<ResourceScopeComp>();
			if (_comp != null)
			{
				_resourceSet = new();
				_comp.SetScope(this);
			}
			else
			{
				throw new Exception("add component failed");
			}
		}

		public void Dispose()
		{
			if (_comp != null)
			{
				GameObject.Destroy(_comp);
			}
			_resourceSet?.Dispose();
		}

		ResourceSet IWithResourceSet.Token => _resourceSet;
	}
}