using MiiAsset.Runtime.Adapter;

namespace MiiAsset.Runtime.IOManagers
{
	public class WDKAdapter : IAdapter
	{
		public string Name => "WDKAdapter";

		public IIOProto GetIOProto()
		{
		#if UNITY_WEBGL && SUPPORT_WDK
			return new WDKIOProto();
		#else
			return null;
		#endif
		}

		public IWidget GetWidget()
		{
		#if UNITY_WEBGL && SUPPORT_WDK
			return new WDKWidget();
		#else
			return null;
		#endif
		}
	}
}