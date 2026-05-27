using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace HatNetwork
{
	public class AsyncOp<T>
	{
		public Task<T> Task { get; set; }
		public long Total;
		public long Current;
		public float Progress => (float)Current / (float)Total;
	}

	public class HatLoader : MonoBehaviour
	{
		protected LoadTool loadTool = new LoadTool();

		protected virtual void Awake()
		{
			if (MyAddressablesUtils.SkipLoadHotCode)
			{
				return;
			}
			
			var go = GameObject.FindWithTag("HotCodeLoader");
			if (go != null)
			{
				go.SendMessage("OnRemoteCodeLoaded", 101);
			}
			HotUpdate();
		}

		// private void OnDisable()
		// {
		// 	Debug.LogWarning("HatLoader Comp OnDisable");
		// }
		//
		// private void OnDestroy()
		// {
		// 	Debug.LogWarning("HatLoader Comp OnDestroy");
		// }

		public virtual AsyncOp<string> HotUpdate()
		{
			var asyncOp = new AsyncOp<string>();
			var task = HotUpdateAsync((cur, total) =>
			{
				asyncOp.Current = cur;
				asyncOp.Total = total;

				Debug.Log($"hot update progress: {asyncOp.Progress}");
			});
			asyncOp.Task = task;
			return asyncOp;
		}

		protected Task<MyAddressablesUtils.GetVersionResp> GetRemoteVersionCodeNew(MonoBehaviour comp)
		{
			return MainResPreloader.Inst.GetRemoteVersionCode(comp);
		}

		protected Task<MyAddressablesUtils.GetVersionResp> GetRemoteVersionCodeCompat(MonoBehaviour comp)
		{
			var fModuleVersion =
				typeof(LocalLoader).GetField("ModuleVersion", BindingFlags.Instance | BindingFlags.Public);
			if (fModuleVersion != null)
			{
				return GetRemoteVersionCodeNew(comp);
			}
			else
			{
				return MyAddressablesUtils.GetRemoteVersionCode(comp);
			}
		}

		/// <summary>
		/// addressables ab path -> preload dlls -> addressables hot update -> load reset dlls -> goon next
		/// </summary>
		/// <returns></returns>
		protected virtual async Task<string> HotUpdateAsync(Action<long, long> onProgress)
		{
			var versionResSize = 1000;
			var preUpdateSize = 764128;
			var hotUpdateSize = HatUpdateState.Inst.HotUpdateSize + preUpdateSize + versionResSize;
			var hotLoadState = new HatLoadState();
			long versionCode = -1;
			string resourceUrl = null;
			try
			{
				if (!HatUpdateState.Inst.IsRemoteVersionCodeValid)
				{
					Debug.Log("强制加载远端版本号");
					// 如果之前未获取到远程版本号, 那么此处强制获取
					while (true)
					{
						var remoteVersion = await GetRemoteVersionCodeCompat(this);
						if (remoteVersion.Succeed)
						{
							HatUpdateState.Inst.IsNeedRestart |= remoteVersion.isNeedRestart;
							versionCode = remoteVersion.versionCode;
							resourceUrl = remoteVersion.resourceUrl;
							break;
						}
					}

					HatUpdateState.Inst.RemoteVersionCode = versionCode;
					HatUpdateState.Inst.RemoteResourceUrl = resourceUrl;
				}
				else
				{
					versionCode = HatUpdateState.Inst.RemoteVersionCode;
					resourceUrl = HatUpdateState.Inst.RemoteResourceUrl;
				}

				onProgress(versionResSize, hotUpdateSize);

				// 强制更新逻辑更新包
				Debug.Log("检查强制加载prehatill");
				var neetLoadPrehatill = HatUpdateState.Inst.RemoteVersionCode != HatUpdateState.Inst.LocalVersionCode &&
				                        false == HatUpdateState.Inst.IsPreRemoteLoaded;
				if (neetLoadPrehatill)
				{
					while (true)
					{
						Debug.Log("强制加载prehatill begin");
						var netState1 =
							await loadTool.LoadVersionedDllsAndCache("prehatill", versionCode, resourceUrl, this);
						if (!netState1.needRetry)
						{
							hotLoadState.IsPreLoaded = netState1.success;
							if (!netState1.success)
							{
								Debug.Log("强制加载prehatill失败");
								Debug.LogException(netState1.exception ?? new Exception("unkown"));
							}
							else
							{
								onProgress(preUpdateSize + versionResSize, hotUpdateSize);
							}

							break;
						}
					}
				}

				Debug.Log($"强制加载hatill: size: {HatUpdateState.Inst.HotUpdateSize}bytes");
				while (true)
				{
					// Debug.Log($"hatill-comp: {this==null}");
					var netState2 = await loadTool.LoadVersionedDllsAndCache("hatill", versionCode, resourceUrl, this);
					if (!netState2.needRetry)
					{
						Debug.Log("load hatill done");
						hotLoadState.IsHotLoaded = netState2.success;
						if (!netState2.success)
						{
							Debug.LogException(netState2.exception ?? new Exception("unkown"));
						}
						else
						{
							onProgress(hotUpdateSize, hotUpdateSize);
						}

						break;
					}
					else
					{
						Debug.Log("retring load hatill");
					}
				}

				// 加载远端新版逻辑成功, 清除旧版缓存
				// Debug.Log("清除旧版缓存");
				// if (hotLoadState.IsRemoteLoaded && HatUpdateState.Inst.LocalVersionCode != HatUpdateState.Inst.RemoteVersionCode)
				// {
				// 	var startVersion = HatUpdateState.Inst.LocalVersionCode;
				// 	for (var iVersion = startVersion; iVersion == startVersion || iVersion < HatUpdateState.Inst.RemoteVersionCode; iVersion++)
				// 	{
				// 		MyAddressablesUtils.ClearOldVersionCache(iVersion);
				// 	}
				// 	MyAddressablesUtils.SaveLocalVersionCode(versionCode);
				// }
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			// copy state
			{
				hotLoadState.LocalVersionCode = HatUpdateState.Inst.LocalVersionCode;
				hotLoadState.RemoteVersionCode = HatUpdateState.Inst.RemoteVersionCode;
				hotLoadState.RemoteResourceUrl = HatUpdateState.Inst.RemoteResourceUrl;
				hotLoadState.IsPreRemoteLoaded = HatUpdateState.Inst.IsPreRemoteLoaded;
				hotLoadState.IsNeedRestart = HatUpdateState.Inst.IsNeedRestart;
			}

			var isOk = hotLoadState.IsRemoteLoaded;
			var go = GameObject.FindWithTag("HotCodeLoader");
			go.SendMessage("OnRemoteCodeLoaded", isOk ? 201 : 202);

			var sResult = JsonUtility.ToJson(hotLoadState);
			Debug.Log($"返回更新结果-HUResult: {sResult}");

		#if TEST_HYBRIDCLR
			Debug.Log($"是否需要重启: {hotLoadState.IsNeedRestartIndeed()}");

			Debug.Log("加载testhot");
			try
			{
				await loadTool.LoadTestPrefabs("testhot", versionCode, this);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		#endif

			return sResult;
		}
	}
}