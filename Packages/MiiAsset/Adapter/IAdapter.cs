using MiiAsset.Runtime.IOManagers;

namespace MiiAsset.Runtime.Adapter
{
	public interface IAdapter
	{
		public string Name { get; }
		public IIOProto GetIOProto();
		public IWidget GetWidget();
	}
}