using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiiAsset.Editor.BuildPipelineUtilities;
using MonoExtLib.LinqExt;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiiAsset.Editor.Analyze
{
	public class ResDeps
	{
		[MenuItem("Tools/MiiAsset/PrintDeps _F12", false, 10001)]
		public static void PrintDeps()
		{
			var assetGUIDs0 = Selection.assetGUIDs;
			if (assetGUIDs0.Length == 0)
			{
				Debug.LogError("请选择分析对象");
				return;
			}

			var assetPaths = assetGUIDs0.Select(g => AssetDatabase.GUIDToAssetPath(g)).ToArray();
			var uniquePaths = new List<string>();
			foreach (var assetPath in assetPaths)
			{
				var assetPath2 = assetPath + "/";
				var exist = false;
				for (var index = 0; index < uniquePaths.Count; index++)
				{
					var uniquePath = uniquePaths[index];
					if (uniquePath.StartsWith(assetPath2))
					{
						uniquePaths.RemoveAt(index);
						index--;
						exist = true;
						continue;
					}

					if (assetPath.StartsWith(uniquePath + "/"))
					{
						exist = true;
						break;
					}
				}

				if (!exist)
				{
					uniquePaths.Add(assetPath);
				}
			}

			var guidFiles = new List<string>();
			var guids = new List<string>();
			foreach (var uniquePath in uniquePaths)
			{
				if (Directory.Exists(uniquePath))
				{
					var files = Directory.GetFiles(uniquePath, "*", SearchOption.AllDirectories);
					foreach (var file in files)
					{
						var guid = AssetDatabase.AssetPathToGUID(file);
						if (false == string.IsNullOrEmpty(guid))
						{
							guids.Add(guid);
							guidFiles.Add(file);
						}
					}
				}
				else
				{
					var guid = AssetDatabase.AssetPathToGUID(uniquePath);
					guids.Add(guid);
					guidFiles.Add(uniquePath);
				}
			}

			if (guids.Count == 0)
			{
				Debug.LogError("请选择分析对象");
				return;
			}

			var assetGUIDs = guids.ToArray();

			BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
			ScriptCompilationSettings scriptCompilationSettings = new ScriptCompilationSettings();
			scriptCompilationSettings.group = buildTargetGroup;
			scriptCompilationSettings.target = buildTarget;
			var tempDir = "./Library/MiiAssets/AnalyzeData/CompiledPlayerScriptsData/";
			Directory.CreateDirectory(tempDir);
			ScriptCompilationResult scriptCompilationResult =
				PlayerBuildInterface.CompilePlayerScripts(scriptCompilationSettings, tempDir);
			EditorUtility.ClearProgressBar();

			HashSet<string> depGuids = new();

			void CollectDeps(string[] assetGUIDs)
			{
				foreach (var assetGUID in assetGUIDs)
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
					if (assetPath.EndsWith(".unity"))
					{
						// var buildSettings = new BuildSettings
						// {
						// 	typeDB = scriptCompilationResult.typeDB,
						// 	target = buildTarget,
						// 	subtarget = 0,
						// 	group = buildTargetGroup,
						// 	buildFlags = ContentBuildFlags.None
						// };
						// var buildUsageTagSet = new BuildUsageTagSet();
						// var buildUsageCache = new BuildUsageCache();
						// ContentBuildInterface.CalculatePlayerDependenciesForScene(
						// 	assetPath,
						// 	buildSettings,
						// 	buildUsageTagSet,
						// 	buildUsageCache,
						// 	DependencyType.ValidReferences
						// );
						// var vGuid = new GUID(assetGUID);
						// var referencedObjects = buildUsageTagSet.GetObjectIdentifiers();
						// var deps2 = ExtensionMethods.FilterReferencedObjectIDs(vGuid, referencedObjects, buildTarget,
						// 	scriptCompilationResult.typeDB, new HashSet<GUID>(new[] { vGuid }));
						// var referPaths2 = deps2.Select(obj => AssetDatabase.GUIDToAssetPath(obj.guid)).Distinct().ToArray();

						var deps = AssetDatabase.GetDependencies(assetPath);
						var referPaths = deps.Where(d => !d.EndsWith(".cs")).ToArray();
						var guids = referPaths.Select(p => AssetDatabase.AssetPathToGUID(p)).ToArray();
						var validGuids = guids.Where(g => !depGuids.Contains(g)).ToArray();
						depGuids.AddRange(validGuids);
						CollectDeps(validGuids);
					}
					else
					{
						var vGuid = new GUID(assetGUID);
						var includedObjects =
							ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(vGuid, buildTarget);
						var referencedObjects = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects,
							buildTarget, scriptCompilationResult.typeDB, DependencyType.ValidReferences);
						var deps = ExtensionMethods.FilterReferencedObjectIDs(vGuid, referencedObjects, buildTarget,
							scriptCompilationResult.typeDB, new HashSet<GUID>(new[] { vGuid }));
						var depsGuids1 = deps.Select(obj => obj.guid.ToString()).ToArray();
						depGuids.AddRange(depsGuids1);
						var referPaths = deps
							.Select(obj => AssetDatabase.GUIDToAssetPath(obj.guid))
							.Distinct()
							.ToArray();
						// depPaths.AddRange(referPaths);
					}
				}
			}

			CollectDeps(assetGUIDs);

			foreach (var assetGUID in assetGUIDs)
			{
				depGuids.Remove(assetGUID);
			}

			var depPaths = depGuids
				.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
				.Distinct()
				.Where(p => !p.EndsWith(".cs"))
				.OrderBy(p => p)
				.ToArray();
			var mainAssetsPaths = depPaths
				.Where(p => p.StartsWith("Assets"))
				.OrderBy(p => p)
				.ToArray();
			Debug.Log($"deps-for [{string.Join(",\n", guidFiles)}] is \n\n" +
			          $"[{string.Join(",\n", mainAssetsPaths)}]");
		}
	}
}