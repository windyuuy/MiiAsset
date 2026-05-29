using MiiAsset.Runtime.Adapter;
using UnityEditor;
using UnityEngine;

namespace Editor.BuildPlayerProcessor
{
	[CreateAssetMenu(fileName = "BuildAssetBundleConfig", menuName = "Mii/BuildAssetBundleConfig", order = 0)]
	public class BuildAssetBundleConfig : ScriptableObject
	{
		[Header("构建安装包时构建AssetBundle")] public bool buildResBundleInBuilding = true;
		[Header("构建安装包后检查AssetBundle")] public bool checkAssetBundlesAfterBuild = true;

		public static bool Load(out BuildAssetBundleConfig config)
		{
			var configGuids =
				UnityEditor.AssetDatabase.FindAssets("t:BuildAssetBundleConfig",
					new[] { "Assets", "Packages/windy.miiasset.core" });
			if (configGuids.Length > 0)
			{
				var configGuid = configGuids[0];
				config =
					UnityEditor.AssetDatabase.LoadAssetAtPath<BuildAssetBundleConfig>(
						UnityEditor.AssetDatabase.GUIDToAssetPath(configGuid));
				return true;
			}
			else
			{
				MyLogger.LogError("no consumer config found");
				config = null;
				return false;
			}
		}
	}
}