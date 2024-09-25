using System;
using System.Collections;
using System.Collections.Generic;
using U3DUdpater.Editor.BuildPipelineTasks;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace U3DUdpater.Editor
{
	struct SBPSettingsOverwriterScope : IDisposable
	{
		bool m_PrevSlimResults;

		public SBPSettingsOverwriterScope(bool forceFullWriteResults)
		{
			m_PrevSlimResults = ScriptableBuildPipeline.slimWriteResults;
			if (forceFullWriteResults)
				ScriptableBuildPipeline.slimWriteResults = false;
		}

		public void Dispose()
		{
			ScriptableBuildPipeline.slimWriteResults = m_PrevSlimResults;
		}
	}

	public class BuildOptions
	{
		public bool GenerateBuildLayout;
		public string MonoScriptBundleName;
		public string BuiltinShaderBundleName;
	}

	public class BuildResult
	{
		public ReturnCode ExitCode;
		public IBundleBuildResults Results;

		public BuildResult(ReturnCode exitCode, IBundleBuildResults results)
		{
			ExitCode = exitCode;
			Results = results;
		}
	}

	public class AssetBuildScript
	{
		static IList<IBuildTask> RuntimeDataBuildTasks(string builtinShaderBundleName, string monoScriptBundleName)
		{
			var buildTasks = new List<IBuildTask>();

			// Setup
			buildTasks.Add(new SwitchToBuildPlatform());
			buildTasks.Add(new RebuildSpriteAtlasCache());

			// Player Scripts
			// if (!s_SkipCompilePlayerScripts)
			buildTasks.Add(new BuildPlayerScripts());
			buildTasks.Add(new PostScriptsCallback());

			// Dependency
			buildTasks.Add(new CalculateSceneDependencyData());
			buildTasks.Add(new CalculateAssetDependencyData());
			// buildTasks.Add(new AddHashToBundleNameTask());
			buildTasks.Add(new StripUnusedSpriteSources());
			buildTasks.Add(new CreateBuiltInShadersBundle(builtinShaderBundleName));
			if (!string.IsNullOrEmpty(monoScriptBundleName))
				buildTasks.Add(new CreateMonoScriptBundle(monoScriptBundleName));
			buildTasks.Add(new PostDependencyCallback());

			// Packing
			buildTasks.Add(new GenerateBundlePacking());
			buildTasks.Add(new UpdateBundleObjectLayout());
			buildTasks.Add(new GenerateBundleCommands());
			buildTasks.Add(new GenerateSubAssetPathMaps());
			buildTasks.Add(new GenerateBundleMaps());
			buildTasks.Add(new PostPackingCallback());

			// Writing
			buildTasks.Add(new WriteSerializedFiles());
			buildTasks.Add(new ArchiveAndCompressBundles());
			// buildTasks.Add(new BuildEncryptionTask());
			// buildTasks.Add(new GenerateLocationListsTask());
			buildTasks.Add(new PostWritingCallback());

			ExtractDataTask extractData = new ExtractDataTask();
			buildTasks.Add(extractData);

			return buildTasks;
		}

		public static BuildResult CreateBuildResult(ReturnCode exitCode, IBundleBuildResults results)
		{
			return new BuildResult(exitCode, results);
		}

		public static BuildResult BuildBundles(IEnumerable<AssetBundleBuild> bundleBuilds, IBundleBuildParameters buildParams, BuildOptions options,
			params IContextObject[] contextObjects)
		{
			var buildTasks = RuntimeDataBuildTasks(options.BuiltinShaderBundleName, options.MonoScriptBundleName);
			IBundleBuildResults results;
			using (new SBPSettingsOverwriterScope(options.GenerateBuildLayout)) // build layout generation requires full SBP write results
			{
				var buildContent = new BundleBuildContent(bundleBuilds);
				var exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out results, buildTasks, contextObjects);

				return CreateBuildResult(exitCode, results);
			}
		}
	}
}
