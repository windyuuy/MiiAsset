using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var bundle = AssetBundle.LoadFromFile("E:\\DATA\\Projects\\test\\MiiAsset\\AssetBundles\\cc_scene_00000000000000000000000000000000.bundle");
        // var assets = bundle.LoadAllAssets();
        // Debug.Log(assets);
        SceneManager.LoadScene("Bundles/CC/New Scene.unity", LoadSceneMode.Additive);
    }

}
