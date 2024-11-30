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
			var localIOProto = adapter.GetIOProto();
			if (localIOProto != null)
			{
				IOManager.LocalIOProto = localIOProto;
			}

			var widget = adapter.GetWidget();
			if (widget != null)
			{
				IOManager.Widget = widget;
			}
		}
	}
}