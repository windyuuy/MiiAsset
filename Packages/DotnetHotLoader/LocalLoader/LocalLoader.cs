using System;
using System.Threading.Tasks;
using UnityEngine;

namespace HatNetwork
{
	/// <summary>
	/// 可先通过LocalLoader加载最少量的程序集, 先加载出界面, 然后通过 HatLoader 加载剩余的程序集
	/// </summary>
	public class LocalLoader : MonoBehaviour
	{
		protected LoadTool loadTool = new LoadTool();

		/// <summary>
		/// 模块版本
		/// </summary>
		public static int ModuleVersion = 2;

		protected virtual async void Start()
		{
			MyAddressablesUtils.UpdateLocalMode();
			await HotUpdate();
		}

//#if UNITY_EDITOR
//		protected virtual IEnumerator LoadDlls()
//		{
//			var uwp1 = UnityWebRequest.Get($"file://D:/Projects/test/TestHyberCLR/Build/{UnityEditor.EditorUserBuildSettings.activeBuildTarget}/HotRes/prehatill");
//			yield return uwp1.SendWebRequest();

//			var uwp2 = UnityWebRequest.Get($"file://D:/Projects/test/TestHyberCLR/Build/{UnityEditor.EditorUserBuildSettings.activeBuildTarget}/HotRes/hatill");
//			yield return uwp2.SendWebRequest();

//			var ab1 = AssetBundle.LoadFromMemory(uwp1.downloadHandler.data);
//			var ab2 = AssetBundle.LoadFromMemory(uwp2.downloadHandler.data);
//			loadTool.LoadDllBundle(ab1);
//			loadTool.LoadDllBundle(ab2);
//		}
//#endif

		/// <summary>
		/// addressables ab path -> preload dlls -> addressables hot update -> load reset dlls -> goon next
		/// </summary>
		/// <returns></returns>
		public virtual async Task HotUpdate()
		{
			Debug.Log("启动加载");
			HatUpdateState.Inst.IsHotScriptEnabled = true;

			MainResPreloader.Inst.CleanUp();
			MainResPreloader.Inst.Preload(this, loadTool);

			var localVersionCode = await MainResPreloader.Inst.GetLocalVersionCode(this);

			var isRemoteLoaded = false;
			long versionCode = -1;
			string resourceUrl = null;

			AssetBundleDownloader.NetState netState1 = new AssetBundleDownloader.NetState()
			{
				needRetry = false,
				success = false,
				complete = true,
			};

			try
			{
				Debug.Log("加载远端版本号");
				var MaxRetries = 10;
			#if UNITY_EDITOR
				MaxRetries = 1;
			#endif
				for (var retries = 0; retries < MaxRetries; retries++)
				{
					var remoteVersion = await MainResPreloader.Inst.GetRemoteVersionCode(this);
					if (remoteVersion.Succeed)
					{
						versionCode = remoteVersion.versionCode;
						resourceUrl = remoteVersion.resourceUrl;
						HatUpdateState.Inst.IsNeedRestart = remoteVersion.isNeedRestart;
						break;
					}

					if (retries == MaxRetries)
					{
						Debug.Log("加载远端版本号失败");
					}
					else
					{
						await MyAddressablesUtils.WaitForSeconds(1, this);
					}
				}

				HatUpdateState.Inst.RemoteVersionCode = versionCode;
				HatUpdateState.Inst.RemoteResourceUrl = resourceUrl;
				if (HatUpdateState.Inst.IsRemoteVersionCodeValid)
				{
					Debug.Log($"加载远端remote-prehatill: {versionCode}");
					// 远程版本号可用, 先尝试远程下载
					try
					{
						for (var retries = 0; retries < MaxRetries; retries++)
						{
							netState1 = await loadTool.LoadVersionedDllsAndCache("prehatill", versionCode, resourceUrl,
								this, true);
							if (!netState1.needRetry)
							{
								isRemoteLoaded = netState1.success;
								break;
							}

							if (retries == MaxRetries)
							{
								Debug.Log("加载远端prehatill失败");
							}
							else
							{
								await MyAddressablesUtils.WaitForSeconds(1, this);
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			HatUpdateState.Inst.IsPreRemoteLoaded = isRemoteLoaded;

			// 如果服务器版本更新异常, 那么检查本地版本
			HatUpdateState.Inst.LocalVersionCode = localVersionCode;
			if (!isRemoteLoaded)
			{
				versionCode = localVersionCode;
				Debug.Log($"加载本地local-prehatill: {versionCode}");
				try
				{
					netState1 = await loadTool.LoadVersionedDllsAndCache("prehatill", versionCode, null, this, true);
				}
				catch (Exception e)
				{
					Debug.LogError("加载本地prehatill失败");
					Debug.LogException(e);
				}
			}

			if (netState1.success)
			{
				var assetBundle = netState1.assetBundle;
				var prefab1 = assetBundle.LoadAsset("HatLoader");
				if (prefab1 != null)
				{
					Debug.Log("加载HatLoader预制体");
					GameObject.Instantiate(prefab1);
					netState1.Release();
				}
				else
				{
					Debug.LogError($"无法加载预制体: HatLoader");
				}
			}

			this.gameObject.tag = "LocalLoaderNode";
			GameObject.DontDestroyOnLoad(this.gameObject);
			// Debug.Log("强制加载热更资源");
			// try
			// {
			// 	while (true)
			// 	{
			// 		var netState2 = await loadTool.LoadPrefabs("hotab", versionCode, this);
			// 		if (netState2.success)
			// 		{
			// 			break;
			// 		}
			// 	}
			// }
			// catch (Exception e)
			// {
			// 	Debug.LogError("加载热更资源失败, 热更出现异常, 请检查HatLoader.prefab");
			// 	Debug.LogException(e);
			// }

			// Destroy(this.gameObject);
		}
	}
}