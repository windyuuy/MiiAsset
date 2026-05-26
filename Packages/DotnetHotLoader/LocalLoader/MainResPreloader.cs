using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace HatNetwork
{
	public class MainResPreloader
	{
		public static readonly MainResPreloader Inst = new();

		protected Task<MyAddressablesUtils.GetVersionResp> GetRemoteVersionCodeTask;
		protected Task<long> GetLocalVersionCodeTask;

		// TODO: 后续版本中完善资源释放, 暂不处理
		public Dictionary<string, (UnityWebRequest uwr, Task<AssetBundleDownloader.NetState> task)> LoadHatillsMap=new();
		protected bool isPreload = false;

		public void CleanUp()
		{
			this.isPreload = false;
			this.GetRemoteVersionCodeTask = null;
			this.GetLocalVersionCodeTask = null;
			foreach (var item in this.LoadHatillsMap)
			{
				try
				{
					var uwr = item.Value.uwr;
					uwr.downloadHandler.Dispose();
					uwr.Dispose();
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			}
			this.LoadHatillsMap.Clear();
		}
		
		public void Preload(MonoBehaviour comp,LoadTool loadTool)
		{
			if (isPreload)
			{
				return;
			}

			isPreload = true;

			GetRemoteVersionCodeTask = MyAddressablesUtils.GetRemoteVersionCode(comp);
			GetLocalVersionCodeTask = MyAddressablesUtils.GetLocalVersionCode(comp);

			async Task<AssetBundleDownloader.NetState> PreloadEgg(string s, long l)
			{
				var preloadPath = await MyAddressablesUtils.GetDllABPath(s, l, null, comp);
				var downloader = new AssetBundleDownloader();
				var uwr = UnityWebRequest.Get(preloadPath);
				var task = downloader.GetDownloadTaskWithoutRetry(uwr, comp,false);
				LoadHatillsMap.Add(preloadPath, (uwr, task));
				var netState = await task;
				return netState;
			}

			async Task<AssetBundleDownloader.NetState> PreloadPrehatill(string key)
			{
				var versionCode=await GetLocalVersionCodeTask;
				return await PreloadEgg(key, versionCode);
			}
			PreloadPrehatill("prehatill");

			async Task<AssetBundleDownloader.NetState> PreloadHatill(string key)
			{
				var resp = await GetRemoteVersionCodeTask;
				long versionCode;
				if (resp.Succeed)
				{
					versionCode = resp.versionCode;
				}
				else
				{
					versionCode=await GetLocalVersionCodeTask;
				}
				return await PreloadEgg(key, versionCode);
			}
			PreloadHatill("hatill");
		}

		public Task<MyAddressablesUtils.GetVersionResp> GetRemoteVersionCode(MonoBehaviour comp)
		{
			if (GetRemoteVersionCodeTask != null)
			{
				var task = GetRemoteVersionCodeTask;
				if (task.IsCompleted && false==task.IsCompletedSuccessfully)
				{
					GetRemoteVersionCodeTask = MyAddressablesUtils.GetRemoteVersionCode(comp);
				}
				return task;
			}
			else
			{
				return MyAddressablesUtils.GetRemoteVersionCode(comp);
			}
		}

		public Task<long> GetLocalVersionCode(MonoBehaviour comp)
		{
			if (GetLocalVersionCodeTask == null)
			{
				GetLocalVersionCodeTask = MyAddressablesUtils.GetLocalVersionCode(comp);
			}
			return GetLocalVersionCodeTask;
		}
	}
}