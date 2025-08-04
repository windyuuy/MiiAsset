using System;
using UnityEngine;

namespace TrackableResourceManager.Runtime
{
	public class ResourceScopeComp : MonoBehaviour
	{
		private UObjectResourceScope _scope;

		public void SetScope(UObjectResourceScope scope)
		{
			_scope = scope;
		}

		private void OnDestroy()
		{
			_scope.Dispose();
		}
	}
}