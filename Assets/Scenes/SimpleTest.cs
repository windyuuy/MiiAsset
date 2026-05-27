using System;
using MiiAsset.Runtime;
using UnityEngine;

public class SimpleTest : MonoBehaviour
{
	public Canvas canvas;

	// Start is called before the first frame update
	public async void Display()
	{
		try
		{
			await AssetLoader.Init();
			Debug.Log("AssetLoader.Init");
			// await AssetLoader.LoadLocalCatalog();
			await AssetLoader.UpdateCatalog("http://192.168.137.3:8081/");
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