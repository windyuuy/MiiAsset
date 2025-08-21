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
		#if UNITY_WEBGL && SUPPORT_WDK && !UNITY_EDITOR
			this.Adapt(new WDKAdapter());
		#elif UNITY_WEBGL && !SUPPORT_WDK && !UNITY_EDITOR
			// MyLogger.LogError($"cur webgl platform not support: {Application.platform}");
		#endif
		}

		public void Adapt(IAdapter adapter)
		{
			var localIOProto = adapter.GetIOProto();
			if (localIOProto != null)
			{
				MyLogger.Log($"use {adapter.Name}.LocalIOProto");
				IOManager.LocalIOProto = localIOProto;
			}

			var widget = adapter.GetWidget();
			if (widget != null)
			{
				MyLogger.Log($"use {adapter.Name}.Widget");
				IOManager.Widget = widget;
			}
		}
	}
}