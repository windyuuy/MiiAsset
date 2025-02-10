using MiiAsset.Runtime.IOManagers;
using UnityEngine;

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
		#if UNITY_WEBGL && SUPPORT_WECHATGAME && !UNITY_EDITOR
			this.Adapt(new WXAdapter());
		#elif UNITY_WEBGL && !SUPPORT_WECHATGAME && !UNITY_EDITOR
			MyLogger.LogError($"cur webgl platform not support: {Application.platform}");
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