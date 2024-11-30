using MiiAsset.Runtime.Adapter;

namespace MiiAsset.Runtime.IOManagers
{
	public class WXAdapter : IAdapter
	{
		public IIOProto GetIOProto()
		{
#if UNITY_WEBGL && SUPPORT_WECHATGAME
			return new WXIOProto();
#else
			return null;
#endif
		}

		public IWidget GetWidget()
		{
#if UNITY_WEBGL && SUPPORT_WECHATGAME
			return new WXWidget();
#else
			return null;
#endif
		}
	}
}