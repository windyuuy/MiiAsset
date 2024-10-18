using MiiAsset.Runtime.IOManagers;

namespace MiiAsset.Runtime.Adapter
{
	public class AdapterInternal
	{
		protected bool IsAdaptDefaultDone = false;

		public void AdaptDefault()
		{
			if (IsAdaptDefaultDone)
			{
				return;
			}

			IsAdaptDefaultDone = true;
#if UNITY_WEBGL && !UNITY_EDITOR
			this.Adapt(new WXAdapter());
#endif
		}

		public void Adapt(IAdapter adapter)
		{
			IOManager.LocalIOProto = adapter.GetIOProto();
		}
	}
}