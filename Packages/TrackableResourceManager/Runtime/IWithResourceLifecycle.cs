using System;

namespace TrackableResourceManager.Runtime
{
	public interface IWithResourceLifecycle
	{
		public Action disposeEvent { get; set; }
	}
}