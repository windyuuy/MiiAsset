using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime;
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
		AssetLoader.Init();
		var result = await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/", "catalog.zip");
		if (result.IsOk)
		{
			_ = AssetLoader.LoadTag(AssetTags.Cc, AssetTags.Bb);
			var sceneAddress = "Assets/Bundles/CC/New Scene.unity";
			await AssetLoader.LoadScene(sceneAddress, new LoadSceneParameters
			{
				loadSceneMode = LoadSceneMode.Additive,
			});
			var capsulePrefab = await AssetLoader.LoadAsset<GameObject>("Assets/Bundles/BB/Capsule.prefab");
			var capsule = GameObject.Instantiate(capsulePrefab);
			await AssetLoader.UnLoadScene(sceneAddress);
			Debug.Log("done");
		}
		else
		{
			result.Print();
		}
	}
}