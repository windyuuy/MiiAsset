using System;
using MiiAsset.Runtime.Adapter;
using UnityEngine;

namespace HatNetwork.Editor
{
	[CreateAssetMenu(fileName = "CLRBuildConfig", menuName = "Mii/CLRBuildConfig", order = 0)]
	public class CLRBuildConfig : ScriptableObject
	{
		[Header("构建安装包时构建代码包")] public bool buildCodeBundleInBuilding = true;
		[Header("补充AOT程序集")] public string[] aotAssemblies = Array.Empty<string>();
		[Header("构建安装包后检查AssetBundle")] public bool checkAssetBundlesAfterBuild = true;

		public static bool Load(out CLRBuildConfig config)
		{
			var configGuids =
				UnityEditor.AssetDatabase.FindAssets("t:CLRBuildConfig",
					new[] { "Assets", "Packages/windy.miiasset.core" });
			if (configGuids.Length > 0)
			{
				var configGuid = configGuids[0];
				config =
					UnityEditor.AssetDatabase.LoadAssetAtPath<CLRBuildConfig>(
						UnityEditor.AssetDatabase.GUIDToAssetPath(configGuid));
				return true;
			}
			else
			{
				MyLogger.LogInfo("no CLRBuildConfig found, use default config");
				config = new CLRBuildConfig();
				return true;
			}
		}
	}
}