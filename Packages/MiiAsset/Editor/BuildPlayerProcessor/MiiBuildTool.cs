using System;
using Editor.BuildPlayerProcessor;
using HatNetwork.Editor;
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

			try
			{
				bool checkAssetBundlesAfterBuild;
				if (BuildAssetBundleConfig.Load(out var buildOptions))
				{
					checkAssetBundlesAfterBuild = buildOptions.checkAssetBundlesAfterBuild;
				}
				else
				{
					checkAssetBundlesAfterBuild = true;
				}

				if (checkAssetBundlesAfterBuild)
				{
					AssetBundleTester.TestLoadAssetBundle();
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}

			MyLogger.Log("BuildAssetBundlesWithPathInfo Done.");
			return ret;
		}

		public static bool BuildCodeBundles()
		{
			try
			{
			#if SUPPORT_HYBRIDCLR
				MyLogger.Log("RebuildHotDllDefault Begin.");
				CLRPreBuildPipeline.RebuildHotDllDefault();
				MyLogger.Log("RebuildHotDllDefault Done.");
			#else
				MyLogger.Log("RebuildHotDllDefault Skip - 未开启 SUPPORT_HYBRIDCLR 宏.");
			#endif
				return true;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				return false;
			}
		}
	}
}