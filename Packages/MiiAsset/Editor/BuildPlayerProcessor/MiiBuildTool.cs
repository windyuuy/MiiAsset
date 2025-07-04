using MiiAsset.Editor;
using MiiAsset.Editor.Build;
using MiiAsset.Runtime.Adapter;
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
			MyLogger.Log("BuildAssetBundlesWithPathInfo Begin.");
			var ret = AADepBuilder.BuildAssetBundlesWithPathInfo();
			MyLogger.Log("BuildAssetBundlesWithPathInfo Done.");
			return ret;
		}
	}
}