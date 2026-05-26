using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using HybridCLR;

using UnityEngine;
using UnityEngine.Networking;

using static HatNetwork.AssetBundleDownloader;

namespace HatNetwork
{
	public class LoadTool
	{
		public virtual async Task<NetState> LoadPrefabs(string abname, long versionCode,string resourceUrl, MonoBehaviour comp)
		{
			var prefabABUrl = await MyAddressablesUtils.GetDllABPath(abname, versionCode, resourceUrl, comp);
			var uwr = UnityWebRequest.Get(prefabABUrl);
			var downloader = new AssetBundleDownloader();
			var netState = await downloader.GetDownloadTask(uwr, comp);
			if (!netState.success)
			{
				Debug.LogException(netState.SafeException);
				return netState;
			}
			var assetBundle = downloader.GetAssetBundle(uwr);
			downloader.DisposeResource(uwr);
			var prefab1 = assetBundle.LoadAsset("HatLoader");
			GameObject.Instantiate(prefab1);
			return netState;
		}

#if TEST_HYBRIDCLR
		public virtual async Task<NetState> LoadTestPrefabs(string abname, long versionCode, MonoBehaviour comp)
		{
			var prefabABUrl = await MyAddressablesUtils.GetTestResPath(abname, versionCode, comp);
			var uwr = UnityWebRequest.Get(prefabABUrl);
			var downloader = new AssetBundleDownloader(new AssetBundleDownloader.DownloadOptions()
			{
				RetryCount = 3,
				Timeout = 5,
			});
			var netState = await downloader.GetDownloadTask(uwr, comp);
			if (!netState.success)
			{
				Debug.LogException(netState.exception);
				return netState;
			}
			var assetBundle = downloader.GetAssetBundle(uwr);
			downloader.DisposeResource(uwr);
			var ass = assetBundle.LoadAllAssets();
			var prefab1 = ass[0];
			var canvas = GameObject.FindGameObjectWithTag("LauncherCanvas");
			GameObject.Instantiate(prefab1, canvas.transform);
			return netState;
		}
#endif

		public virtual async Task<NetState> LoadVersionedDllsAndCache(string abname, long versionCode,string resourceUrl, MonoBehaviour comp, bool keepBundle = false)
		{
			var cachePath1 = MyAddressablesUtils.GetDllABCachePath(abname, versionCode);
			var netState = await LoadVersionedDlls(abname, versionCode, resourceUrl, cachePath1, comp, keepBundle);
			return netState;
		}
		public virtual async Task<NetState> LoadVersionedDlls(string abname, long versionCode, string resourceUrl, string cachePath, MonoBehaviour comp, bool keepBundle = false)
		{
			var preloadPath = await MyAddressablesUtils.GetDllABPath(abname, versionCode, resourceUrl, comp);
			// Debug.Log($"hatill-comp: {comp==null}");
			var netState = await LoadDlls(preloadPath, cachePath, versionCode, comp);
			if (!keepBundle)
			{
				netState.Release();
			}
			return netState;
		}

		public static string GetPDBName(string dllName)
		{
			var fileName = Path.GetFileNameWithoutExtension(dllName);
			return $"{fileName}.pdb";
		}
		protected static Dictionary<string, bool> LoadedAOT = new Dictionary<string, bool>();
		//protected HashSet<string> wf=new HashSet<string>();
		public virtual bool LoadDllBundle(AssetBundle assetBundle)
		{
			//var ass = assetBundle.LoadAllAssets();
			//var manifestObj = ass.First(obj => obj.name == "manifest");
			Debug.Log($"LoadDllBundle: {assetBundle.name}");
			var manifestObj = assetBundle.LoadAsset("manifest");
			if (manifestObj is TextAsset manifestText)
			{
				var manifest = JsonUtility.FromJson<BinManifest>(manifestText.text);
				HatUpdateState.Inst.HotUpdateSize = Math.Max(manifest.hotSize, HatUpdateState.Inst.HotUpdateSize);
				var anyThingLoad=false;
				foreach (var info in manifest.aotDlls)
				{
					//wf.Add(info.name);
					//Debug.Log(wf.Count);
					if (LoadedAOT.ContainsKey(info.name))
					{
						continue;
					}
					anyThingLoad=true;
					LoadedAOT.Add(info.name, true);

					//var dllObj = ass.First(obj => obj.name == info.name);
					var dllObj = assetBundle.LoadAsset(info.name);
					if (dllObj is TextAsset dllTextAsset)
					{
						var dllBin = dllTextAsset.bytes;

						Debug.Log($"Load AOT: {info.DisplayName}");
#if UNITY_EDITOR
						Debug.Assert(dllBin != null && dllBin.Length > 0);
#else
						LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBin, HomologousImageMode.SuperSet);
						if (err == LoadImageErrorCode.OK)
						{
							Debug.Log($"Load AtoEgg OK: {info.DisplayName}");
						}
						else
						{
							Debug.LogError($"Load AtoEgg Failed: {info.DisplayName}");
						}
#endif
					}
					else
					{
						Debug.LogError($"Load AtoEgg TextAsset Failed: {info.DisplayName}");
					}
				}

				foreach (var info2 in manifest.hotDlls)
				{
					var name3 = Path.GetFileNameWithoutExtension(info2.name);
					//var gameAss = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == name3);
					//if (gameAss == null)

					if (LoadedAOT.ContainsKey(info2.name))
					{
						continue;
					}
					anyThingLoad=true;
					LoadedAOT.Add(info2.name, true);
					{
						//var dllObj = ass.First(obj => obj.name == info2.name);
						var dllObj = assetBundle.LoadAsset(info2.name);
						if (dllObj is TextAsset dllTextAsset)
						{
							var dllBin = dllTextAsset.bytes;
#if SUPPORT_PDB
							Debug.Log($"Load HatEgg: {info2.DisplayName}");
							var pdbObj = ass.FirstOrDefault(obj => obj.name == GetPDBName(info2.name));
							if (pdbObj != null)
							{
								var pdbBin = (pdbObj as TextAsset).bytes;
								gameAss = System.Reflection.Assembly.Load(dllBin, pdbBin, SecurityContextSource.CurrentAppDomain);
							}
							else
							{
								var gameAss = System.Reflection.Assembly.Load(dllBin);
							}
#else

							Debug.Log($"Load HatEgg: {info2.DisplayName}");
							try
							{
								// 新版加载会对编辑器程序集造成实质性变更(如反射信息变化)
#if UNITY_EDITOR
								Debug.Assert(dllBin != null && dllBin.Length > 0);
								Debug.Log($"Load HatEgg Skip: {info2.DisplayName}");
#else
								var gameAss = System.Reflection.Assembly.Load(dllBin);
								Debug.Log($"Load HatEgg OK: {info2.DisplayName}, {gameAss!=null}");
#endif
							}
							catch (Exception e)
							{
								Debug.LogError($"Load HatEgg Failed: {info2.DisplayName}");
								Debug.LogException(e);
							}
#endif
						}
						else
						{
							Debug.LogError($"Load HatEgg TextAsset Failed: {info2.DisplayName}");
						}
					}
					//else
					//{
					//	Debug.Log($"lib exist, skip load: {info2.DisplayName}");
					//}
				}

				if(!anyThingLoad){
					Debug.LogError("EggAB nothing load");
				}

				return true;
			}else{
				Debug.LogError("Invalid EggAB");
				return false;
			}
		}
		
		public virtual async Task<NetState> LoadDlls(string dllUrl, string cachePath, long versionCode, MonoBehaviour comp)
		{
			Debug.Log($"Loading HatURL: {dllUrl}, cachedPath: {cachePath}, versionCode: {versionCode}, comp: {comp!=null}");
			var downloader = new AssetBundleDownloader();
			Task<NetState> loadTask;
			UnityWebRequest uwr;
			var isUsingCache = false;
			if (MainResPreloader.Inst.LoadHatillsMap.TryGetValue(dllUrl, out var info))
			{
				isUsingCache = true;
				(uwr, loadTask) = info;
			}
			else
			{
				uwr = UnityWebRequest.Get(dllUrl);
				loadTask = downloader.GetDownloadTask(uwr, comp);
			}
			Debug.Log($"isUsingCache: {isUsingCache}");
			var netState = await loadTask;
			Debug.Log($"await return");
			if (!netState.success)
			{
				Debug.LogError("netState failed");
				Debug.LogException(netState.SafeException);
				if (isUsingCache)
				{
					downloader.DisposeResource(uwr);
					MainResPreloader.Inst.LoadHatillsMap.Remove(dllUrl);
				}
				return netState;
			}
			var assetBundle = downloader.GetAssetBundle(uwr);
			if (cachePath != null && assetBundle != null && dllUrl != cachePath)
			{
				try
				{
					MyAddressablesUtils.SaveFileSafe(cachePath, uwr.downloadHandler.data);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

			if (!isUsingCache)
			{
				downloader.DisposeResource(uwr);
			}
			
			try
			{
				var success=LoadDllBundle(assetBundle);
				Debug.Log($"LoadDllBundle-Result: {success}");
				if (!success)
				{
					netState.success = false;
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			// assetBundle.Unload(true);
			netState.assetBundle = assetBundle;
			
			Debug.Log($"netState: {netState.success}, {netState.needRetry}");

			return netState;
		}
	}
}
