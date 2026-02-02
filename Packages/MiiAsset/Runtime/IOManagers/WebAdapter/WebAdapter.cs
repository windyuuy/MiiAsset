#if SUPPORT_WEBGL_LOCAL_STORAGE
using MiiAsset.Runtime.Adapter;

namespace MiiAsset.Runtime.IOManagers
{
	public class WebAdapter : IAdapter
	{
		public string Name { get; }

		public IIOProto GetIOProto()
		{
			MyLogger.Log("use-WebAdapter::GetIOProto");
			// return new WebIOProtoWithLocalStorage();
			return new WebIOProtoWithIndexedDB();
		}

		public IWidget GetWidget()
		{
			return null;
		}
	}
}
#endif
