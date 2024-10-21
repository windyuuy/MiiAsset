using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace MiiAsset.AddressablesExt
{
	public static class AddressablesExt
	{
		public static Task<bool> CleanUpAddressablesCache()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			var ts = new TaskCompletionSource<bool>();
			var wxfs = WeChatWASM.WX.GetFileSystemManager();
			var dir = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE/hotres/";
			if (wxfs.AccessSync(dir) == "access:ok")
			{
				wxfs.Rmdir(new WeChatWASM.RmdirParam
				{
					success = (resp) => { ts.SetResult(true); },
					fail = (resp) =>
					{
						Debug.LogError($"remove aa-cache failed, errCode: {resp.errCode}, errMsg: {resp.errMsg}");
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
				// 	Debug.LogException(exception);
				// }
			}
			else
			{
				Debug.Log("本地存储为空");
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
					Debug.LogException(exception);
				}

				try
				{
					Caching.ClearCache();
					return Task.FromResult(true);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
					return Task.FromResult(false);
				}
			}
			else
			{
				return Task.FromResult(true);
			}
#endif
		}
	}
}