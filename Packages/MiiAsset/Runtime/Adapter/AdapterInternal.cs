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
			#if SUPPORT_WDK
			this.Adapt(new WDKAdapter());
			#elif SUPPORT_WEBGL_LOCAL_STORAGE
			this.Adapt(new WebAdapter());
			#else
			MyLogger.Log($"use default sdk adapter for webgl platform: {UnityEngine.Application.platform}");
			#endif
		#endif
		}

		public void Adapt(IAdapter adapter)
		{
			var localIOProto = adapter.GetIOProto();
			if (localIOProto != null)
			{
				MyLogger.LogInfo($"use {adapter.Name}.LocalIOProto");
				IOManager.LocalIOProto = localIOProto;
			}

			var widget = adapter.GetWidget();
			if (widget != null)
			{
				MyLogger.LogInfo($"use {adapter.Name}.Widget");
				IOManager.Widget = widget;
			}
		}
	}
}