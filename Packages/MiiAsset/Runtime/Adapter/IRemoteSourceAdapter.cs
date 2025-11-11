namespace MiiAsset.Runtime.Adapter
{
	public interface IRemoteSourceAdapter
	{
		string ConvertRemoteUri(string remoteBaseUri, string fileName);
	}
}