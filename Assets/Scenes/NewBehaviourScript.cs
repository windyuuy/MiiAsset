using System;
using System.Threading.Tasks;
using GameLib.MonoUtils;
using lang.time;
using MiiAsset.MiiAssetHint;
using MiiAsset.Runtime;
using MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

public class NewBehaviourScript : MonoBehaviour
{
	// Start is called before the first frame update
	async void Start()
	{
		TaskScheduler.UnobservedTaskException += (s, e) => { Debug.LogException(e.Exception); };
		AppDomain.CurrentDomain.UnhandledException += (s, args) => { Debug.LogError((Exception)args.ExceptionObject); };
		LoomMG.Init();
		await AsyncUtils.WaitForFrames(1);
		var dt1 = Date.Now();
		await TestProgress();
		var dt2 = Date.Now();
		Debug.Log($"test-timecost: {dt2 - dt1}");
	}

	private static async Task Test1_1()
	{
		await AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/");
		if (result.IsOk)
		{
			var bundleSet = AssetLoader.GetTagsBundleSet(AssetTags.Cc, AssetTags.Bb);
			bundleSet.AllowTags();
			var sceneAddress = "Assets/Bundles/CC/New Scene.unity";
			await bundleSet.LoadScene(sceneAddress, new LoadSceneParameters
			{
				loadSceneMode = LoadSceneMode.Additive,
			});
			var capsulePrefab = await bundleSet.LoadAsset<GameObject>("Assets/Bundles/BB/Capsule.prefab");
			var capsule = GameObject.Instantiate(capsulePrefab);
			await bundleSet.UnLoadScene(sceneAddress);
			Debug.Log("done");
			await bundleSet.UnLoadTags();
		}
		else
		{
			result.Print();
		}
	}

	private static async Task Test1_2()
	{
		await AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/");
		if (result.IsOk)
		{
			var bundleSet = AssetLoader.GetTagsBundleSet(AssetTags.Cc, AssetTags.Bb);
			_ = bundleSet.DownloadTags();
			_ = bundleSet.LoadTags();
			_ = bundleSet.DownloadTags();
			var sceneAddress = "Assets/Bundles/CC/New Scene.unity";
			await bundleSet.LoadScene(sceneAddress, new LoadSceneParameters
			{
				loadSceneMode = LoadSceneMode.Additive,
			});
			var capsulePrefab = await bundleSet.LoadAsset<GameObject>("Assets/Bundles/BB/Capsule.prefab");
			var capsule = GameObject.Instantiate(capsulePrefab);
			await bundleSet.UnLoadScene(sceneAddress);
			Debug.Log("done");
			await bundleSet.UnLoadTags();
		}
		else
		{
			result.Print();
		}
	}

	private static async Task Test2()
	{
		await AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/");
		if (result.IsOk)
		{
			var sceneAddress = "Assets/Bundles/CC/New Scene.unity";
			await AssetLoader.LoadSceneByRefer(sceneAddress, new LoadSceneParameters
			{
				loadSceneMode = LoadSceneMode.Additive,
			});
			var capsulePrefab = await AssetLoader.LoadAssetByRefer<GameObject>("Assets/Bundles/BB/Capsule.prefab");
			var capsule = GameObject.Instantiate(capsulePrefab);
			Debug.Log("unload scene");
			await AssetLoader.UnLoadSceneByRefer(sceneAddress);
			Debug.Log("unload asset");
			await AssetLoader.UnLoadAssetByRefer("Assets/Bundles/BB/Capsule.prefab");
			Debug.Log("done");
		}
		else
		{
			result.Print();
		}
	}

	private static async Task Test3()
	{
		await AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/");
		if (result.IsOk)
		{
			var sceneAddress = "Assets/Bundles/CC/New Scene.unity";
			await AssetLoader.LoadSceneByRefer(sceneAddress, new LoadSceneParameters
			{
				loadSceneMode = LoadSceneMode.Additive,
			});
			var capsulePrefab = await AssetLoader.LoadAssetByRefer<GameObject>("Assets/Bundles/BB/Capsule.prefab");
			var capsule = GameObject.Instantiate(capsulePrefab);
			Debug.Log("unload scene");
			var task1 = AssetLoader.UnLoadSceneByRefer(sceneAddress);
			Debug.Log("unload asset");
			var task2 = AssetLoader.UnLoadAssetByRefer("Assets/Bundles/BB/Capsule.prefab");
			Debug.Log("unload all");
			await Task.WhenAll(task1, task2);
			Debug.Log("done");
		}
		else
		{
			result.Print();
		}
	}

	private static async Task Test4()
	{
		await AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/");
		if (result.IsOk)
		{
			var sceneAddress = "Assets/Bundles/CC/New Scene.unity";
			await AssetLoader.LoadSceneByRefer(sceneAddress, new LoadSceneParameters
			{
				loadSceneMode = LoadSceneMode.Additive,
			});

			async Task Load1()
			{
				var capsulePrefab = await AssetLoader.LoadAssetByRefer<GameObject>("Assets/Bundles/BB/Capsule.prefab");
				var capsule = GameObject.Instantiate(capsulePrefab);
			}

			_ = Load1();
			var task1 = AssetLoader.UnLoadSceneByRefer(sceneAddress);
			var task2 = AssetLoader.UnLoadAssetByRefer("Assets/Bundles/BB/Capsule.prefab");
			_ = Load1();
			Debug.Log("done");
		}
		else
		{
			result.Print();
		}
	}

	private static async Task TestProgress()
	{
		// var files = Directory.GetFiles(IOManager.LocalIOProto.CacheDir);
		// foreach (var file in files)
		// {
		// 	File.Delete(file);
		// }

		var loadStatus = new AssetLoadStatusGroup();
		var dt1 = Date.Now();
		var isInitOk = await AssetLoader.Init();
		if (!isInitOk)
		{
			Debug.LogError("init failed");
			return;
		}

		var dt2 = Date.Now();
		var result = await AssetLoader.UpdateCatalog("http://192.168.110.59:8081/");
		var dt3 = Date.Now();
		if (result.IsOk)
		{
			var downloadSize = AssetLoader.GetDownloadSize(AssetTags.Cc, AssetTags.Bb);
			var dt4 = Date.Now();
			Debug.Log($"DownloadSize: {downloadSize}");
			await AssetLoader.CleanUpOldVersionFiles();
			var dt5 = Date.Now();

			var sceneAddress = "Assets/Bundles/CC/New Scene.unity";

			async Task TypeProgress()
			{
				float p = 0;
				while (true)
				{
					var downloadProgress = loadStatus.DownloadProgress;
					if (p > downloadProgress.Percent || (100 < downloadProgress.Total && downloadProgress.Total < 4148766))
					{
						Debug.Log("lkwjef");
					}

					p = downloadProgress.Percent;
					Debug.Log($"progress: {downloadProgress.Count}/{downloadProgress.Total}={downloadProgress.Percent}");
					if (downloadProgress.IsDone)
					{
						break;
					}

					await AsyncUtils.WaitForFrames(1);
				}
			}

			_ = TypeProgress();
			await AssetLoader.LoadSceneByRefer(sceneAddress, new LoadSceneParameters
			{
				loadSceneMode = LoadSceneMode.Additive,
			}, loadStatus);
			var dt6 = Date.Now();

			async Task Load1()
			{
				var capsulePrefab = await AssetLoader.LoadAssetByRefer<GameObject>("Assets/Bundles/BB/Capsule.prefab");
				var capsule = GameObject.Instantiate(capsulePrefab);
			}

			var unloadTask1 = Load1();
			var dt7 = Date.Now();
			var task1 = AssetLoader.UnLoadSceneByRefer(sceneAddress);
			var task2 = AssetLoader.UnLoadAssetByRefer("Assets/Bundles/BB/Capsule.prefab");
			var dt8 = Date.Now();
			var unloadTask2 = Load1();
			var dt9 = Date.Now();
			var task3 = AssetLoader.UnLoadAssetByRefer("Assets/Bundles/BB/Capsule.prefab");
			var dt10 = Date.Now();
			Debug.Log($"done: {dt2 - dt1}, {dt3 - dt2}, {dt4 - dt3}, {dt5 - dt4}, {dt6 - dt5}, {dt7 - dt6}, {dt8 - dt7}, {dt9 - dt8}, {dt10 - dt9}");
			
			await Task.WhenAll(task1, task2, task3, unloadTask1, unloadTask2);
			
			await Load1();
			await AssetLoader.UnLoadAssetByRefer("Assets/Bundles/BB/Capsule.prefab");
			
			var atlas = await AssetLoader.LoadAssetByRefer<SpriteAtlas>("Assets/Bundles/BB/jfew/FVE.spriteatlasv2");
			var sprite = atlas.GetSprite("login_btn_kanjian1");
			Debug.Assert(sprite != null);
			await AssetLoader.UnLoadAssetByRefer("Assets/Bundles/BB/jfew/FVE.spriteatlasv2");
		}
		else
		{
			result.Print();
		}
	}
}