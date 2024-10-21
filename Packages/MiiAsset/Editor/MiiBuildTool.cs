using MiiAsset.Editor;
using MiiAsset.Editor.Build;
using UnityEditor;
using UnityEngine;

namespace MiiAsset.Editor.Build
{
    public static class MiiBuildTool
    {
        [MenuItem("Tools/MiiAsset/BuildAssetBundlesWithPathInfo")]
        internal static void BuildAssetBundlesWithPathInfo1()
        {
            BuildAssetBundlesWithPathInfo();
        }
        
        public static BuildAssetBundlesResult BuildAssetBundlesWithPathInfo()
        {
            var ret = AADepCollector.BuildAssetBundlesWithPathInfo();
            Debug.Log("Build Done.");
            return ret;
        }

    }
}