using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime;
using Framework.MiiAsset.Runtime.IOManagers;
using Framework.MiiAsset.Runtime.Status;
using GameLib.MonoUtils;
using MiiAssetHint;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewBehaviourScript : MonoBehaviour
{
	// Start is called before the first frame update
	async void Start()
	{
		TaskScheduler.UnobservedTaskException += (s, e) => { Debug.LogException(e.Exception); };
		AppDomain.CurrentDomain.UnhandledException += (s, args) => { Debug.LogError((Exception)args.ExceptionObject); };
		LoomMG.Init();
		await TestProgress();
	}

	private static async Task Test1_1()
	{
		AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/", "catalog.zip");
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
		AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/", "catalog.zip");
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
		AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/", "catalog.zip");
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
		AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/", "catalog.zip");
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
		AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/", "catalog.zip");
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
		AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/", "catalog.zip");
		if (result.IsOk)
		{
			var downloadSize = AssetLoader.GetDownloadSize(AssetTags.Cc, AssetTags.Bb);
			Debug.Log($"DownloadSize: {downloadSize}");

			var sceneAddress = "Assets/Bundles/CC/New Scene.unity";

			async Task TypeProgress()
			{
				float p = 0;
				while (true)
				{
					var downloadProgress = loadStatus.DownloadProgress;
					if (p > downloadProgress.Progress || (100 < downloadProgress.Total && downloadProgress.Total < 4148766))
					{
						Debug.Log("lkwjef");
					}

					p = downloadProgress.Progress;
					Debug.Log($"progress: {downloadProgress.Count}/{downloadProgress.Total}={downloadProgress.Progress}");
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
}