using System;
using MiiAsset.Editor.Build;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HatNetwork.Editor
{
	public class ModifyPipeline : IPostGenerateGradleAndroidProject
	{
		public int callbackOrder => -1;

		public void OnPostGenerateGradleAndroidProject(string path)
		{
			CLRBuildAssetBundle.ModifyStreamAssemblyLimitation();
		}
	}


	public class CleanupPipeline : IPostGenerateGradleAndroidProject
	{
		public int callbackOrder => 4;

		public void OnPostGenerateGradleAndroidProject(string path)
		{
			CLRBuildAssetBundle.RecorverStreamAssemblyLimitation();
		}
	}

	public class CLRPreBuildPipeline : BuildPlayerProcessor
		, IPostprocessBuildWithReport
	//, IPostGenerateGradleAndroidProject
	//, IPostBuildPlayerScriptDLLs
	//, IFilterBuildAssemblies
	{
		public override int callbackOrder => 1;

		public void OnPostprocessBuild(BuildReport report)
		{
// #if UNITY_IOS ||UNITY_STANDALONE_OSX||UNITY_IPHONE
// 			BuildAssetBundle.CompileIL2CPPForIOS();
// 			BuildAssetBundle.CopyRuntimeLib(report.summary.outputPath);
// #endif
		}

		public static int ResVersionCode
		{
			get => UnityEngine.PlayerPrefs.GetInt("ChannelPublisher::VersionCode", -1);
		}

		static BuildPlayerOptions BuildPlayerOptions0;

		public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
		{
		#if SUPPORT_HYBRIDCLR
			bool buildCodeBundleInBuilding;
			bool checkAssetBundlesAfterBuild;
			if (CLRBuildConfig.Load(out var buildOptions))
			{
				buildCodeBundleInBuilding = buildOptions.buildCodeBundleInBuilding;
				checkAssetBundlesAfterBuild = buildOptions.checkAssetBundlesAfterBuild;
			}
			else
			{
				buildCodeBundleInBuilding = true;
				checkAssetBundlesAfterBuild = true;
			}

			if (buildCodeBundleInBuilding)
			{
				RebuildHotDllForCurrentTarget(buildPlayerContext.BuildPlayerOptions);
			}

			if (checkAssetBundlesAfterBuild)
			{
				try
				{
					CodeBundleTester.TestLoadAssetBundle();
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}

		#endif
		}

		private static void RebuildHotDllForCurrentTarget(BuildPlayerOptions buildPlayerOptions)
		{
		#if SUPPORT_HYBRIDCLR
			if (!HybridCLR.Editor.Settings.HybridCLRSettings.Instance.enable)
			{
				Debug.Log("未启用 HybridCLR, 跳过代码热更包构建");
				return;
			}
		#else
			return;
		#endif
		#if TEST_HYBRIDCLR
			// TODO: simulate local settings
			long versionCode = 1;
		#else
			long versionCode = ResVersionCode;
		#endif

			RebuildHotDlls(versionCode, buildPlayerOptions);
		}

		[MenuItem("Tools/DotnetHotLoader/构建代码热更包")]
		public static void RebuildHotDllDefault()
		{
			var buildPlayerOptions = new BuildPlayerOptions()
			{
				target = EditorUserBuildSettings.activeBuildTarget,
				targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup,
			};
			RebuildHotDllForCurrentTarget(buildPlayerOptions);

			try
			{
				bool checkAssetBundlesAfterBuild;
				if (CLRBuildConfig.Load(out var buildOptions))
				{
					checkAssetBundlesAfterBuild = buildOptions.checkAssetBundlesAfterBuild;
				}
				else
				{
					checkAssetBundlesAfterBuild = true;
				}

				if (checkAssetBundlesAfterBuild)
				{
					CodeBundleTester.TestLoadAssetBundle();
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		public static void RebuildHotDlls(long versionCode, BuildPlayerOptions buildPlayerOptions)
		{
			var fastBuild = true;
		#if SUPPORT_HYBRIDCLR
			if (!fastBuild)
			{
				HybridCLR.Editor.Commands.PrebuildCommand.GenerateAll();
			}
			else
			{
				HybridCLR.Editor.Commands.CompileDllCommand.CompileDllActiveBuildTarget();
			}
		#else
			Debug.Log("未启用 SUPPORT_HYBRIDCLR 宏");
			return;
		#endif

			CLRBuildAssetBundle.CleanDllBundles();

			var ResUpdateTunnel = "default";
			// TODO: 暂时不修正, 和旧包兼容
			// var ResUpdateTunnel = U3DUdpater.AASettingsManager.Settings.ResUpdateTunnel;

			UnityEngine.Debug.Log($"BuildDllsAB");
			CLRBuildAssetBundle.BuildDllsAB(buildPlayerOptions, ResUpdateTunnel, versionCode);

		#if TEST_HYBRIDCLR
			UnityEngine.Debug.Log($"BuildOtherABs");
			BuildAssetBundle.BuildOtherABs(buildPlayerOptions);

			var copyDest = $"{Path.GetDirectoryName(buildPlayerOptions.locationPathName)}/HotRes/{versionCode}";
			UnityEngine.Debug.Log($"CopyDest -> {copyDest}");
			BuildAssetBundle.CopyDest(copyDest);
		#else
			var platform = MyAddressablesUtils.GetPlatformPathSubFolder();
			var copyDest = $"AssetBundles/{platform}/";
			UnityEngine.Debug.Log($"CopyDest -> {copyDest}");
			CLRBuildAssetBundle.CopyDest(copyDest, ResUpdateTunnel, versionCode);
			CLRBuildAssetBundle.AdaptAppV1(copyDest, ResUpdateTunnel, versionCode);
		#endif

			UnityEngine.Debug.Log("CLRPreBuildPipeline done");
		}
	}
}