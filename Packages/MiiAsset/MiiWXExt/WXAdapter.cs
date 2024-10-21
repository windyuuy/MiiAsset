using MiiAsset.Runtime.Adapter;

namespace MiiAsset.Runtime.IOManagers
{
	public class WXAdapter: IAdapter
	{
		public IIOProto GetIOProto()
		{
			return new WXIOProto();
		}
	}
}