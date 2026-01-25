using System;
using System.IO;
using System.Threading.Tasks;
using GDK;
using MiiAsset.Runtime.Adapter;
using UnityEngine;

namespace MiiAsset.AddressablesExt
{
	public static class AddressablesExt
	{
		public static Task<bool> CleanUpAddressablesCache()
		{
		#if UNITY_WEBGL && SUPPORT_WDK && !UNITY_EDITOR
			var ts = new TaskCompletionSource<bool>();
			try
			{
				var fs = UserAPI.Instance.FileSystem.GetFileSystemManager();
				var dir = $"{UserAPI.Instance.GameInfo.UserDataPath}/__GAME_FILE_CACHE/hotres/";
				if (fs.AccessSync(dir).Exist)
				{
					fs.Rmdir(new()
					{
						success = (resp) =>
						{
							MyLogger.Log("清理AA缓存成功");
							ts.SetResult(true);
						},
						fail = (resp) =>
						{
							MyLogger.LogError($"remove aa-cache failed, errCode: {resp.ErrCode}, errMsg: {resp.ErrMsg}");
							ts.SetResult(false);
						},
						dirPath = dir,
						recursive = true,
					});
					//
					// try
					// {
					// 	Caching.ClearCache();
					// }
					// catch (Exception exception)
					// {
					// 	MyLogger.LogException(exception);
					// }
				}
				else
				{
					MyLogger.Log("本地存储为空");
					ts.SetResult(true);
				}
			}
			catch (Exception exception)
			{
				MyLogger.LogException(exception);
				ts.SetResult(false);
			}

			return ts.Task;
		#else
			var cacheDir = $"{Application.persistentDataPath}/com.unity.addressables/";
			if (Directory.Exists(cacheDir))
			{
				try
				{
					Directory.Delete(cacheDir, true);
				}
				catch (Exception exception)
				{
					MyLogger.LogException(exception);
				}

			#if !UNITY_WEBGL || UNITY_EDITOR
				try
				{
					Caching.ClearCache();
					return Task.FromResult(true);
				}
				catch (Exception exception)
				{
					MyLogger.LogException(exception);
					return Task.FromResult(false);
				}
			#else
				return Task.FromResult(true);
			#endif
			}
			else
			{
				return Task.FromResult(true);
			}
		#endif
		}
	}
}