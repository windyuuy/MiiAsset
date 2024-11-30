using MiiAsset.Runtime.IOManagers;

namespace MiiAsset.Runtime.Adapter
{
	public interface IAdapter
	{
		public IIOProto GetIOProto();
		public IWidget GetWidget();
	}
}