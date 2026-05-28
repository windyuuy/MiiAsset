using System.IO;
using Editor.BuildPlayerProcessor;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Settings;
using U3DUdpater;
using U3DUdpater.Editor.BuildPipeline;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

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
			bool buildCodeBundleInBuilding;
			if (CLRBuildConfig.Load(out var buildOptions))
			{
				buildCodeBundleInBuilding = buildOptions.buildCodeBundleInBuilding;
			}
			else
			{
				buildCodeBundleInBuilding = true;
			}

			if (buildCodeBundleInBuilding)
			{
				RebuildHotDllForCurrentTarget(buildPlayerContext.BuildPlayerOptions);
			}
		}

		private static void RebuildHotDllForCurrentTarget(BuildPlayerOptions buildPlayerOptions)
		{
			if (!HybridCLRSettings.Instance.enable)
			{
				return;
			}
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
		}

		public static void RebuildHotDlls(long versionCode, BuildPlayerOptions buildPlayerOptions)
		{
			var fastBuild = true;
			if (!fastBuild)
			{
				PrebuildCommand.GenerateAll();
			}
			else
			{
				CompileDllCommand.CompileDllActiveBuildTarget();
			}

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