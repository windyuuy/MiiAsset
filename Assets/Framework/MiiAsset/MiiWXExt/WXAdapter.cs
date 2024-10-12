using Framework.MiiAsset.Runtime.Adapter;

namespace Framework.MiiAsset.Runtime.IOManagers
{
	public class WXAdapter: IAdapter
	{
		public IIOProto GetIOProto()
		{
			return new WXIOProto();
		}
	}
}