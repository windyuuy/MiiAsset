using System;

namespace MiiAsset.Runtime
{
	public static class RemoteUriHandler
	{
		public static Func<string, string, string> RemoteUriConvertor = DefaultRemoteUriConvertor;

		public static string ConvertRemoteUri(string remoteBaseUri, string catalogName)
		{
			if (RemoteUriConvertor != null)
			{
				return RemoteUriConvertor(remoteBaseUri, catalogName);
			}
			else
			{
				return remoteBaseUri + catalogName;
			}
		}

		public static string DefaultRemoteUriConvertor(string remoteBaseUri, string catalogName)
		{
			return remoteBaseUri + catalogName;
		}
	}
}