using System;
using Editor.BuildPlayerProcessor;
using MiiAsset.Runtime.Adapter;
using UnityEngine;

namespace HatNetwork.Editor
{
	[CreateAssetMenu(fileName = "CLRBuildConfig", menuName = "Mii/CLRBuildConfig", order = 0)]
	public class CLRBuildConfig : ScriptableObject
	{
		[Header("构建安装包时构建代码包")] public bool buildCodeBundleInBuilding = true;
		[Header("补充AOT程序集")] public string[] aotAssemblies = Array.Empty<string>();

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
				MyLogger.LogError("no consumer config found");
				config = null;
				return false;
			}
		}
	}
}