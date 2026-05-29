using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using U3DUdpater.AppSettings;
using UnityEngine;
using UnityEngine.Networking;

namespace HatNetwork
{
	public class MyAddressablesUtils
	{
		/// <summary>
		/// Retrieves the Addressables platform subfolder of the build platform that is being used.
		/// </summary>
		/// <returns>Returns the Addressables platform subfolder of the build platform that is being used.</returns>
		public static string GetPlatformPathSubFolder()
		{
			return PlatformAdapter.GetPlatformPathSubFolder();
		}

		public static string GetWritableDllABCachePath(string name, long versionCode)
		{
			var cachePath = $"{Application.persistentDataPath}/HLtLib/{versionCode}/{name}";
			return cachePath;
		}

		public static void ClearOldVersionCache(long versionCode)
		{
			try
			{
				var oldDir = $"{Application.persistentDataPath}/HLtLib/{versionCode}";
				if (Directory.Exists(oldDir))
				{
					Directory.Delete(oldDir, true);
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"delete old version failed: {versionCode}");
			}
		}

		public static string ToFileURL(string filePath)
		{
			return $"file://{filePath}";
		}

		public static string GetDllABCachePath(string name, long versionCode)
		{
			var cachePath = $"file://{Application.persistentDataPath}/HLtLib/{versionCode}/{name}";
			return cachePath;
		}

		public static string GetDllABCacheDirectPath(string name, long versionCode)
		{
			var cachePath = $"{Application.persistentDataPath}/HLtLib/{versionCode}/{name}";
			return cachePath;
		}

		public static string GetDllABStreamPath(string name, long versionCode)
		{
			var localPath = $"{StreamingAssetsPath}/HLtLib/{versionCode}/{name}";
			return localPath;
		}

	#if UNITY_EDITOR
		public static string ConvFileUrlToLocal(string path)
		{
			var proto = "file://";
			if (path.StartsWith(proto))
			{
				return string.Join("", path.Skip(proto.Length));
			}

			return path;
		}
	#endif

		public static async Task<string> GetDllABLocalPath(string name, long versionCode, MonoBehaviour comp)
		{
		#if UNITY_EDITOR
			{
				var rlocalPath = GetDllABStreamPath(name, versionCode);
				var localPath = ConvFileUrlToLocal(rlocalPath);
				if (File.Exists(localPath))
				{
					return rlocalPath;
				}

				var localVT = ConvFileUrlToLocal(VersionTextFilePath);
				var localVersion = int.Parse(File.ReadAllText(localVT));
				var rlocalPath2 = GetDllABStreamPath(name, localVersion);
				var localPath2 = ConvFileUrlToLocal(rlocalPath2);
				if (File.Exists(localPath2))
				{
					return rlocalPath2;
				}
			}
		#else
			if (AppConfigBase.InitResVersionCode == versionCode)
			{
				var localPath = GetDllABStreamPath(name, versionCode);
				// var task = GetUWRRequestGetTask(localPath, comp);
				// var result = await task;
				// if (result == UnityWebRequest.Result.Success)
				// {
				// 	return localPath;
				// }
				return localPath;
			}
			else
		#endif
			{
				var cachePath = GetDllABCacheDirectPath(name, versionCode);
				if (File.Exists(cachePath))
				{
					var cacheUrl = ToFileURL(cachePath);
					return cacheUrl;
				}
			}

			return null;
		}

		public static Task<UnityWebRequest.Result> GetUWRRequestTask(UnityWebRequest uwr, MonoBehaviour comp)
		{
			var taskSource = new TaskCompletionSource<UnityWebRequest.Result>();

			IEnumerator Run()
			{
			#if UNITY_IOS
				if (EnableLocalMode)
				{
					Debug.Log($"watchabload: {uwr.url}");
				}
			#endif
				yield return uwr.SendWebRequest();

				if (uwr.result != UnityWebRequest.Result.Success)
				{
					Debug.LogError($"Http请求失败: {uwr.url}: {uwr.error}");
				}

				if (uwr.result != UnityWebRequest.Result.Success && uwr.url.StartsWith("http"))
				{
					MyAddressablesUtils.ShowNetError(
						$"检查更新游戏失败(2), code:{uwr.responseCode}, msg:{uwr.error}",
						() => { taskSource.SetResult(uwr.result); });
				}
				else
				{
					taskSource.SetResult(uwr.result);
				}
			}

			comp.StartCoroutine(Run());

			return taskSource.Task;
		}

		public static Task<UnityWebRequest.Result> GetUWRRequestGetTask(string url, MonoBehaviour comp)
		{
			var uwr = UnityWebRequest.Get(url);
			var task = GetUWRRequestTask(uwr, comp);
			return task;
		}

		public static bool EnableLocalMode = false;

		public static void UpdateLocalMode()
		{
		#if UNITY_IOS
			// ios官方提审包需要禁用热更功能, 全程使用包内代码和资源
			if (AppConfigBase.ChannelId == "ios")
			{
				Debug.Log("iOS官方渠道, 始终使用包内版本");
				EnableLocalMode = true;
			}
		#endif
		}

		public static string RemoteCDNUrl
		{
			get
			{
			#if TEST_HYBRIDCLR
		// TODO: 替换为配置中的地址
				var remoteAddress = "https://ms-static-test.meta.youdao.com";
			#elif UNITY_IOS
				string remoteAddress;
				if (EnableLocalMode)
				{
					remoteAddress = Application.streamingAssetsPath;
				}
				else
				{
					remoteAddress = AppConfigBase.CdnUrl;
				}
			#else
				var remoteAddress = AppConfigBase.CdnUrl;
			#endif
				return remoteAddress;
			}
		}

		public static string AccountServerUrl
		{
			get
			{
			#if TEST_HYBRIDCLR
		// TODO: 替换为配置中的地址
		var remoteAddress = "https://api-ms-test.meta.youdao.com/v1";
			#else
				var remoteAddress = AppConfigBase.AccountHttpUrl;
			#endif
				return remoteAddress;
			}
		}

		public static string HotUpdateTunnelPrefix
		{
			get
			{
				var tunnel = AppConfigBase.HotUpdateTunnel;
				if (!string.IsNullOrEmpty(tunnel))
				{
					return $"{tunnel}_";
				}
				else
				{
					return "";
				}
			}
		}

		public static string CorrectHttpUrlParent(string url)
		{
			if (url.EndsWith("/"))
			{
				return url;
			}
			else
			{
				return url + "/";
			}
		}

		public static async Task<string> GetDllABPath(string name, long versionCode, string resourceUrl,
			MonoBehaviour comp)
		{
			var localPath = await GetDllABLocalPath(name, versionCode, comp);
			if (localPath != null)
			{
				return localPath;
			}

			var remotePath = $"{CorrectHttpUrlParent(resourceUrl)}{HotUpdateTunnelPrefix}{name}";

		#if UNITY_EDITOR
			var remotePathTest =
				$"file://{Path.GetFullPath($"Build/{UnityEditor.EditorUserBuildSettings.activeBuildTarget}/HotRes/{versionCode}/{name}").Replace("\\", "/")}";
			if (File.Exists(remotePath))
			{
				remotePath = remotePathTest;
			}
			else
			{
				Debug.LogWarning("本地测试代码资源不存在");
			}
		#endif

			return remotePath;
		}

		public static async Task<string> GetTestResPath(string name, long versionCode, MonoBehaviour comp)
		{
		#if UNITY_EDITOR
			return
				$"file://D:/Projects/test/TestHyberCLR/Build/{UnityEditor.EditorUserBuildSettings.activeBuildTarget}/HotRes/{name}";
		#endif
			var localPath = $"{StreamingAssetsPath}/HLtLib/{name}";
			var result = await GetUWRRequestGetTask(localPath, comp);
			if (result == UnityWebRequest.Result.Success)
			{
				return localPath;
			}

			var buildTarget = GetPlatformPathSubFolder();
			var remoteAddress = RemoteCDNUrl;
			var remotePath =
				$"{remoteAddress}/uu3du/EGBL/A10001/{versionCode}/{buildTarget}/hotres/{HotUpdateTunnelPrefix}{name}";

			return remotePath;
		}

		public static bool ParserVersionCode(string filePath, out long versionCode)
		{
			var content = File.ReadAllText(filePath, Encoding.UTF8);
			return long.TryParse(content, out versionCode);
		}

		public static string StreamingAssetsPath
		{
			get
			{
			#if UNITY_ANDROID && (!UNITY_EDITOR)
				return Application.streamingAssetsPath;
			#else
				if (!Application.streamingAssetsPath.StartsWith("file://"))
				{
					return "file://" + Application.streamingAssetsPath;
				}
				else
				{
					return Application.streamingAssetsPath;
				}
			#endif
			}
		}

		public static string VersionTextFilePath => $"{StreamingAssetsPath}/HLtLib/version.txt";

		public static bool SkipLoadHotCodeForce = false;
		public static bool SkipLoadHotCode
		{
			get
			{
				if (SkipLoadHotCodeForce)
				{
					return true;
				}
			#if UNITY_EDITOR
				var existHotCodeRes = File.Exists(VersionTextFilePath);
				return !existHotCodeRes;
			#else
				return false;
			#endif
			}
		}

		// TODO: compat app-v1.6.x 之后的版本强更后, 才能使用
		public static string GetHLtLibDir()
		{
			return $"{Application.persistentDataPath}/HLtLib/";
		}

		// TODO: compat app-v1.6.x 之后的版本强更后, 才能使用
		public static string GetVersionTextPath()
		{
			var cachePath = $"{Application.persistentDataPath}/HLtLib/version.txt";
			return cachePath;
		}

		// TODO: compat app-v1.6.x 之后的版本强更后, 才能使用
		public static bool IsVersionTextExist()
		{
			return File.Exists(GetVersionTextPath());
		}

		private static bool? isVersionTextExistOverTime;

		public static bool IsVersionTextExistOverTime()
		{
			if (isVersionTextExistOverTime == null)
			{
				isVersionTextExistOverTime = File.Exists(GetVersionTextPath());
			}

			return isVersionTextExistOverTime.Value;
		}

		// TODO: compat app-v1.6.x 之后的版本强更后, 才能使用
		public static void ClearOldVersionCachesBefore(long versionCode)
		{
			var dirs = new DirectoryInfo(GetHLtLibDir()).GetDirectories();
			foreach (var dir in dirs)
			{
				if (long.TryParse(dir.Name, out var folderVersion))
				{
					if (folderVersion < versionCode)
					{
						MyAddressablesUtils.ClearOldVersionCache(folderVersion);
					}
				}
			}
		}

		public static async Task<long> GetLocalVersionCode(MonoBehaviour comp)
		{
			// if (AppConfigBase.IsDevToolsEnabled)
			// {
			// 	// 测试用
			// 	if (TestVersionSettings.IsEnabled)
			// 	{
			// 		if (TestVersionSettings.VersionCode >= 0)
			// 		{
			// 			return TestVersionSettings.VersionCode;
			// 		}
			// 	}
			// }

			var cachePath = $"{Application.persistentDataPath}/HLtLib/version.txt";
			if (File.Exists(cachePath))
			{
				if (ParserVersionCode(cachePath, out long versionCode1))
				{
					return versionCode1;
				}
			}

			var localPath = VersionTextFilePath;
			var localUWP = UnityWebRequest.Get(localPath);
			var result = await GetUWRRequestTask(localUWP, comp);

			if (result == UnityWebRequest.Result.Success)
			{
				if (long.TryParse(localUWP.downloadHandler.text, out long versionCode2))
				{
					// TODO: 兼容app-v1.1.x
					// if (!File.Exists($"{Application.persistentDataPath}/HLtLib/{versionCode2}/hatill"))
					{
						RemoveAddressablesCatalog();
					}
					return versionCode2;
				}
				else
				{
					Debug.LogError($"invalid version code: {localUWP.downloadHandler.text}");
				}
			}
			else
			{
				Debug.LogError("no valid version.txt found");
			}

			return 1;
		}

		public static void RemoveAddressablesCatalog()
		{
			var addDir = $"{Application.persistentDataPath}/com.unity.addressables";
			if (Directory.Exists(addDir))
			{
				Directory.Delete(addDir, true);
			}
		}

		public static Task WaitForSeconds(int sec, MonoBehaviour comp)
		{
			var taskSource = new TaskCompletionSource<bool>();

			IEnumerator Run()
			{
				yield return new WaitForSeconds(sec);
				taskSource.SetResult(true);
			}

			comp.StartCoroutine(Run());

			return taskSource.Task;
		}

		[Serializable]
		public class GetVersionResp
		{
			public long versionCode;
			public string resourceUrl;
			public bool isNeedRestart;
			public bool isForceUpdate;
			public bool Succeed;
			public string Message;
		}

		public static IGetVersionNetClient NetClient { get; set; }

		public static async Task<GetVersionResp> GetRemoteVersionCode(MonoBehaviour comp)
		{
			var versionCode = await GetRemoteVersionCodeInternal(comp);
			Debug.Log($"RemoteCodeVersion: {versionCode.versionCode}");
			return versionCode;
		}
		private static async Task<GetVersionResp> GetRemoteVersionCodeInternal(MonoBehaviour comp)
		{
			var version = await NetClient.GetVersion();
			try
			{
				if (version.isForceUpdate)
				{
					// 强更模式下, 跳过远程加载, 直接使用本地加载
					long localVersionCode;
					if (EnableLocalMode)
					{
						localVersionCode = AppConfigBase.InitResVersionCode;
					}
					else
					{
						localVersionCode = await MyAddressablesUtils.GetLocalVersionCode(comp);
					}

					version.versionCode = localVersionCode;
					return version;
				}
				else
				{
					// 成功返回热更版本
				#if UNITY_IOS
				// iOS官方渠道, 始终采用包内版本
				if (EnableLocalMode)
				{
					Debug.Log($"iOS官方渠道, 始终使用包内版本1: {AppConfigBase.InitResVersionCode}");
					version.versionCode = AppConfigBase.InitResVersionCode;
				}else
				#endif
					// 不更新不兼容的低版本热更
					if (version.versionCode < AppConfigBase.MinResVersionCode)
					{
						Debug.Log($"使用最低兼容版本1: {AppConfigBase.MinResVersionCode}");
						version.versionCode = AppConfigBase.MinResVersionCode;
					}

					return version;
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return new GetVersionResp()
				{
					Succeed = false,
					Message = e.ToString(),
				};
			}
		}

		public static bool SaveLocalVersionCode(long versionCode)
		{
			var cachePath = $"{Application.persistentDataPath}/HLtLib/version.txt";
			var dir = Path.GetDirectoryName(cachePath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			File.WriteAllTextAsync(cachePath, $"{versionCode}");
			return true;
		}

		public static void SaveFileSafe(string path, byte[] data)
		{
			if (path.StartsWith("file://"))
			{
				path = path.Replace("file://", "");
			}

			var dir = Path.GetDirectoryName(path);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			File.WriteAllBytes(path, data);
		}

		public static void CopyDirectory(string sourceDir, string destinationDir, string prefix, bool recursive,
			bool overwrite)
		{
			// Get information about the source directory
			var dir = new DirectoryInfo(sourceDir);

			// Check if the source directory exists
			if (!dir.Exists)
				throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

			// Cache directories before we start copying
			DirectoryInfo[] dirs = dir.GetDirectories();

			// Create the destination directory
			Directory.CreateDirectory(destinationDir);

			// Get the files in the source directory and copy to the destination directory
			foreach (FileInfo file in dir.GetFiles())
			{
				string targetFilePath = Path.Combine(destinationDir, prefix + file.Name);
				file.CopyTo(targetFilePath, overwrite);
			}

			// If recursive and copying subdirectories, recursively call this method
			if (recursive)
			{
				foreach (DirectoryInfo subDir in dirs)
				{
					string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
					CopyDirectory(subDir.FullName, newDestinationDir, prefix, recursive, overwrite);
				}
			}
		}

		public static void ShowNetError(string message, Action callback)
		{
			MyAddressablesUtils.ShowLocalDialog("网络异常", $"请确认网络环境后再试\n{message}", (ok) =>
			{
				if (ok)
				{
					callback?.Invoke();
				}
				else
				{
				#if UNITY_EDITOR
					UnityEditor.EditorApplication.ExitPlaymode();
				#else
				Application.Quit();
				#endif
				}
			}, "重试", "退出应用");
		}

		/// <summary>
		/// 退出app, app 2.10 才支持
		/// </summary>
		public static void QuitApp()
		{
		#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExitPlaymode();
		#else
			Application.Quit();
		#endif
		}

		private static TaskCompletionSource<bool> tipQuitTask;

		/// <summary>
		/// 提示退出应用, SDK初始化完成之前都调用这个, app 2.10 才支持
		/// </summary>
		/// <param name="callback"></param>
		public static async void ShowSimpleQuitTip(Action<bool> callback = null)
		{
			if (tipQuitTask == null)
			{
				tipQuitTask = new();

				ShowLocalDialog("提示", "是否退出有道魔力说", (ok) =>
				{
					tipQuitTask.SetResult(ok);
					if (ok)
					{
						QuitApp();
					}
				}, "确定", "取消");
			}

			var ret = await tipQuitTask.Task;
			tipQuitTask = null;
			callback?.Invoke(ret);
		}

		public static Action<string, string, Action<bool>, string, string> ShowLocalDialogAction { get; set; }

		/// <summary>
		/// 展示本地对话框, 必须是模态对话框
		/// </summary>
		/// <param name="title"></param>
		/// <param name="content"></param>
		/// <param name="callback">返回选择结果, true: 确认, false: 取消</param>
		/// <param name="agreeStr"></param>
		/// <param name="refuseStr"></param>
		public static void ShowLocalDialog(string title, string content, Action<bool> callback = null,
			string agreeStr = null, string refuseStr = null)
		{
			ShowLocalDialogAction(title, content, callback, agreeStr, refuseStr);
		}
	}
}