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
	}
}