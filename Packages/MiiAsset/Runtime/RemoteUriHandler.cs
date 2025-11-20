using System;
using System.Threading.Tasks;

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

		public static Func<string, Task<bool>> WaitChooseReloadAssetFunc = DefaultChooseReloadAssetFunc;

		private static Task<bool> DefaultChooseReloadAssetFunc(string s)
		{
			return Task.FromResult(false);
		}

		public static Task<bool> WaitChooseReloadAsset(string Uri)
		{
			if (WaitChooseReloadAssetFunc != null)
			{
				return WaitChooseReloadAssetFunc(Uri);
			}
			else
			{
				return Task.FromResult(false);
			}
		}

		public static Action<Exception> ExceptionHandler;

		public static void EmitException(Exception exception)
		{
			ExceptionHandler?.Invoke(exception);
		}
	}
}