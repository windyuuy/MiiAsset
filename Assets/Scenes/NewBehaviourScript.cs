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
		await AssetLoader.UpdateCatalog("http://127.0.0.1:8080/", "catalog.zip");
		_ = AssetLoader.LoadTag(AssetTags.Cc, AssetTags.Bb);
		await AssetLoader.LoadScene("Assets/Bundles/CC/New Scene.unity");
		var capsulePrefab = await AssetLoader.LoadAsset<GameObject>("Assets/Bundles/BB/Capsule.prefab");
		var capsule = GameObject.Instantiate(capsulePrefab);
		Debug.Log("lwkje");
	}
}