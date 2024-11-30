namespace MiiAsset.Runtime.IOManagers
{
	public class IOManager
	{
		public static IIOProto LocalIOProto = new LocalIOProto();
		public static IWidget Widget = new DefaultWidget();
	}
}