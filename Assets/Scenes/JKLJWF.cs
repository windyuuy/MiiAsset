using System;
using MiiAsset.Runtime;
using UnityEngine;

public class JKLJWF : MonoBehaviour
{
	public Canvas canvas;

	// Start is called before the first frame update
	async void Start()
	{
		try
		{
			await AssetLoader.Init();
			Debug.Log("AssetLoader.Init");
			// await AssetLoader.LoadLocalCatalog();
			await AssetLoader.UpdateCatalog("http://127.0.0.1:8081/bundles/");
			Debug.Log("AssetLoader.LoadLocalCatalog");
			var obj = await AssetLoader.InstantiateAsync("Assets/Bundles/BB/TestLoad.prefab", canvas.transform).Task;
			Debug.Log("AssetLoader.InstantiateAsync");
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}
}