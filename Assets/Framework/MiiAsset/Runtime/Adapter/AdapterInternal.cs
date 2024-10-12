using Framework.MiiAsset.Runtime.IOManagers;

namespace Framework.MiiAsset.Runtime.Adapter
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
#if UNITY_WEBGL
			this.Adapt(new WXAdapter());
#endif
		}

		public void Adapt(IAdapter adapter)
		{
			IOManager.LocalIOProto = adapter.GetIOProto();
		}
	}
}