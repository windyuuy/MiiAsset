using System.Collections;
using System.Collections.Generic;
using GameLib.Networking.Ext;
using UnityEngine;

public class TestJarAB : MonoBehaviour
{
	// Start is called before the first frame update
	public void OnClick()
	{
		StreamingAssetBundleLoader.LoadFromFileAsync(Application.streamingAssetsPath + "/aaa.bundle", 0, 0, false);
	}
}