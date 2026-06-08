using System;
using System.IO;
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

		[MenuItem("Tools/MiiAsset/清理代码构建缓存", false, 10018)]
		public static void CleanBuildCodeCache()
		{
			try
			{
				if (Directory.Exists("Library/BurstCache"))
				{
					Directory.Delete("Library/BurstCache", true);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		[MenuItem("Tools/MiiAsset/清理资源构建缓存", false, 10018)]
		public static void CleanBuildAssetCache()
		{
			try
			{
				if (Directory.Exists("Library/BuildCache"))
				{
					Directory.Delete("Library/BuildCache", true);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}

			try
			{
				if (Directory.Exists("Library/ShaderCache"))
				{
					Directory.Delete("Library/ShaderCache", true);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		[MenuItem("Tools/MiiAsset/清理构建缓存", false, 10018)]
		public static void CleanBuildCache()
		{
			CleanBuildCodeCache();
			CleanBuildAssetCache();
		}
	}
}