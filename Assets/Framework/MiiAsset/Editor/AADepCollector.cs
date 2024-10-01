﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Framework.MiiAsset.Runtime;
using Framework.MiiAsset.Runtime.AssetUtils;
using Lang.Encoding;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.Build.Pipeline;
using AssetBundleInfo = Framework.MiiAsset.Runtime.AssetBundleInfo;

namespace U3DUdpater.Editor
{
	public class ExtraBuildOptions
	{
		public string UpdateTunnel = "";
	}

	public class AnalyzeItemResult
	{
		public string ResultName;
		public MessageType Severity { get; set; }
	}

	public class AnalyzeResult
	{
		public List<AnalyzeItemResult> Results = new();

		public void AddResult(AnalyzeItemResult analyzeResult)
		{
			Results.Add(analyzeResult);
		}
	}

	[Serializable]
	public class WriteRecords
	{
		public List<string> records = new();
	}

	public class AADepCollector
	{
		/// <summary>
		/// 特殊目录列表
		/// </summary>
		public static Dictionary<string, bool> SpecialFolders = new Dictionary<string, bool>()
		{
			{ "Editor", true },
			{ "Editor Default Resources", true },
			{ "Gizmos", true },
			{ "Resources", true },
			{ "Standard Assets", true },
			{ "Pro Standard Assets", true },
			{ "StreamingAssets", true },
			{ "Plugins", true },
			{ "/.", true },
			{ "/~", true },
			{ "/Hidden", true },
		};

		public static bool IsValidAsset(string assetPath, AAPathInfo pathInfo)
		{
			var isContainSpecificFolder = false;
			foreach (var kvp in SpecialFolders)
			{
				if (kvp.Value && assetPath.Contains(kvp.Key))
				{
					isContainSpecificFolder = true;
					break;
				}
			}

			var isHiddenFolder = false;
			var excludeExts = pathInfo.ExcludeExtensions;
			foreach (var ext in excludeExts)
			{
				if (assetPath.EndsWith(ext))
				{
					isHiddenFolder = true;
					break;
				}
			}

			var isFolderOnly = Directory.Exists(assetPath);
			var isNotSpecialFolder = !(isContainSpecificFolder || isHiddenFolder || isFolderOnly);
			return isNotSpecialFolder;
		}

		public class TagBundle
		{
			public HashSet<string> Guids = new();
			public string[] Tags;
			public string TagsUKey;
			public string BundleFileName => $"{GetTagsKey()}_{BuildInfo.Hash}.bundle";
			public HashSet<string> DepTagNames = new();
			public HashSet<string> Deps = new();
			public BundleDetails BuildInfo;

			public string GetTagsKey()
			{
				return string.Join("_", Tags);
			}

			public string GetBundleName()
			{
				return $"{GetTagsKey()}";
			}

			public string[] GetAssetNames()
			{
				return Guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();
			}

			public string[] GetAssetAddresses()
			{
				return GetAssetNames();
			}

			public string GetBundlePathWithHash(string dir)
			{
				return $"{dir}/{this.BundleFileName}";
			}
		}

		public class BuildAssetBundlesResult
		{
			public string Msg;
			public ReturnCode Code;
			public BuildResult BuildResult;
		}

		public static BuildAssetBundlesResult BuildAssetBundles(AAPathInfo pathInfo, ScriptCompilationSettings scriptCompilationSettings, ExtraBuildOptions options)
		{
			AnalyzeResult result = new();
			if (!BuildUtility.CheckModifiedScenesAndAskToSave())
			{
				return new BuildAssetBundlesResult()
				{
					Msg = "Cannot run Analyze with unsaved scenes",
					Code = ReturnCode.UnsavedChanges,
				};
			}

			// collect tag bundles
			Dictionary<string, TagBundle> tagBundleMap = new();
			Dictionary<string, int> tagOrderMap = new();
			Dictionary<string, TagBundle> guidBundleMap = new();
			Dictionary<string, TagBundle> tagsNameBundleMap = new();
			int tagOrderAcc = 0;

			string ToTagsKey(string[] tags)
			{
				var tagKey = string.Join("_", tags.Select(tag =>
					{
						if (!tagOrderMap.TryGetValue(tag, out var order))
						{
							order = tagOrderAcc++;
							tagOrderMap.Add(tag, order);
						}

						return (tag, order);
					}).OrderBy(item => item.order)
					.Select(item => item.order));
				return tagKey;
			}

			foreach (var scanInfo in pathInfo.GetScanRootInfos())
			{
				var guids = AssetDatabase.FindAssets("", new[] { scanInfo.ScanRoot });
				var validGroupNameInfo = guids.Select(guid => (guid, assetPath: AssetDatabase.GUIDToAssetPath(guid)))
					.Where(item => IsValidAsset(item.assetPath, pathInfo))
					.Select(item =>
					{
						var groupNameInfo = AAPathInfo.ParseGroupName(scanInfo.Item, item.assetPath, item.guid);
						return groupNameInfo;
					})
					.Where(item => item != null);
				foreach (var groupNameInfo in validGroupNameInfo)
				{
					if (groupNameInfo.AssetPath.EndsWith(".unity"))
					{
						groupNameInfo.Tags = groupNameInfo.Tags.Append("scene").ToArray();
					}

					var tagsKey = ToTagsKey(groupNameInfo.Tags);
					if (!tagBundleMap.TryGetValue(tagsKey, out var tagBundle))
					{
						tagBundle = new()
						{
							Tags = groupNameInfo.Tags,
							TagsUKey = tagsKey,
						};
						tagBundleMap.Add(tagsKey, tagBundle);
					}

					tagBundle.Guids.Add(groupNameInfo.Guid);

					if (guidBundleMap.TryGetValue(groupNameInfo.Guid, out var exist))
					{
						Debug.LogError($"conflict item: {groupNameInfo.AssetPath}");
					}

					guidBundleMap[groupNameInfo.Guid] = tagBundle;
				}
			}

			var tagBundles = tagBundleMap.Values.ToArray();

			// // analyze deps
			// var tempDir = "./Library/MiiAssets/CompiledPlayerScriptsData/";
			// if (!Directory.Exists(tempDir))
			// {
			// 	Directory.CreateDirectory(tempDir);
			// }
			// ScriptCompilationResult scriptCompilationResult = PlayerBuildInterface.CompilePlayerScripts(scriptCompilationSettings, tempDir);
			// foreach (var item in tagBundleMap)
			// {
			// 	var tagBundle = item.Value;
			// 	foreach (var guid in tagBundle.Guids)
			// 	{
			// 		var vGuid = new GUID(guid);
			// 		
			// 		var includedObjects = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(vGuid, scriptCompilationSettings.target);
			// 		var referencedObjects =
			// 			ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, scriptCompilationSettings.target, scriptCompilationResult.typeDB,
			// 				DependencyType.ValidReferences);
			// 		foreach (var referencedObject in referencedObjects)
			// 		{
			// 			if (guidBundleMap.TryGetValue(referencedObject.guid.ToString(), out var refTagBundle))
			// 			{
			// 				tagBundle.Deps.Add(refTagBundle.TagsUKey);
			// 			}
			// 		}
			// 	}
			// }

			// build content
			var bundleBuilds = tagBundles.Select(tagBundle =>
			{
				var build = new AssetBundleBuild
				{
					assetBundleName = tagBundle.GetBundleName(),
					assetBundleVariant = "",
					assetNames = tagBundle.GetAssetNames(),
					addressableNames = tagBundle.GetAssetAddresses(),
				};
				return build;
			});
			var folderPath = "AssetBundles";
			var outPath = "Temp/MiiAsset/AssetBundles";
			var tmpPath = "Temp/MiiAsset/Temp";
			if (Directory.Exists(outPath))
			{
				Directory.Delete(outPath, true);
			}

			IBundleBuildParameters buildParams =
				new BundleBuildParameters(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.selectedBuildTargetGroup, outPath);
			buildParams.TempOutputFolder = tmpPath;

			var buildOptions = new BuildOptions
			{
				GenerateBuildLayout = false,
				MonoScriptBundleName = null,
				BuiltinShaderBundleName = "builtinshader",
			};
			var buildResult = AssetBuildScript.BuildBundles(bundleBuilds, buildParams, buildOptions);

			if (buildResult.ExitCode == 0)
			{
				// associate with bundle info
				var bundleInfos = buildResult.Results.BundleInfos;
				foreach (var item in bundleInfos)
				{
					var tagBundle = tagBundles.FirstOrDefault(tagBundle => tagBundle.GetBundleName() == item.Key);
					if (tagBundle != null)
					{
						tagBundle.BuildInfo = item.Value;

						foreach (var dep in item.Value.Dependencies)
						{
							tagBundle.DepTagNames.Add(dep);
						}
					}
					else
					{
						var tags = new[] { item.Key };
						var builtinBundleInfo = new TagBundle
						{
							Guids = new(),
							Tags = tags,
							TagsUKey = ToTagsKey(tags),
							DepTagNames = new(),
							BuildInfo = item.Value
						};
						tagBundleMap.Add(builtinBundleInfo.TagsUKey, builtinBundleInfo);
					}
				}

				tagBundles = tagBundleMap.Values.ToArray();
				foreach (var tagBundle in tagBundles)
				{
					tagsNameBundleMap[tagBundle.GetTagsKey()] = tagBundle;
				}

				foreach (var tagBundle in tagBundles)
				{
					var deps = tagBundle.DepTagNames;
					foreach (var dep in deps)
					{
						var depTagBundle = tagsNameBundleMap[dep];
						var depFileName = depTagBundle.BundleFileName;
						tagBundle.Deps.Add(depFileName);
					}
				}

				// prepare out folder
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}

				var writeListFilePath = $"{folderPath}/WriteList.txt";
				if (File.Exists(writeListFilePath))
				{
					var writeListContent = File.ReadAllText(writeListFilePath, Encoding.UTF8);
					var writeRecords = JsonUtility.FromJson<WriteRecords>(writeListContent);
					foreach (var record in writeRecords.records)
					{
						if (File.Exists(record))
						{
							File.Delete(record);
						}
					}

					File.Delete(writeListFilePath);
				}

				// rename bundle with hash
				foreach (var tagBundle in tagBundles)
				{
					var sourcePath = tagBundle.BuildInfo.FileName;
					var destPath = tagBundle.GetBundlePathWithHash(folderPath);
					if (File.Exists(destPath))
					{
						File.Delete(destPath);
					}

					File.Move(sourcePath, destPath);
					Debug.Assert(!File.Exists(sourcePath));
					Debug.Assert(File.Exists(destPath));
				}

				var buildlogSourcePath = $"{outPath}/buildlogtep.json";
				var buildlogDestPath = $"{folderPath}/buildlogtep.json";
				if (File.Exists(buildlogSourcePath))
				{
					if (File.Exists(buildlogDestPath))
					{
						File.Delete(buildlogDestPath);
					}

					File.Move(buildlogSourcePath, buildlogDestPath);
				}

				// build catalog with verify info and dep 
				var catalogBundleInfos = tagBundles.Select((tagBundle, index) =>
				{
					var resultsBundleInfo = tagBundle.BuildInfo;
					return new AssetBundleInfo
					{
						bundleName = tagBundle.GetTagsKey(),
						fileName = tagBundle.BundleFileName,
						crc = resultsBundleInfo.Crc,
						hash128 = resultsBundleInfo.Hash,
						deps = tagBundle.Deps.ToArray(),
						tags = tagBundle.Tags.ToArray(),
						entries = tagBundle.GetAssetNames(),
					};
				}).ToArray();
				var catalog = new CatalogConfig
				{
					bundleInfos = catalogBundleInfos,
				};
				var catalogContent = JsonUtility.ToJson(catalog);
				var catalogFilePath = $"{folderPath}/catalog_{options.UpdateTunnel}.zip".Replace("_.", ".");
				var catalogHashFilePath = Path.ChangeExtension(catalogFilePath, "hash");
				var bytes = Encoding.UTF8.GetBytes(catalogContent);
				using (var ms = new FileStream(catalogFilePath, FileMode.Create))
				using (ZipArchive arch = new ZipArchive(ms, ZipArchiveMode.Create))
				{
					var entry = arch.CreateEntry("catalog.json");
					var stream = entry.Open();
					stream.Write(bytes);
				}

				var hash = SHA256.Create();
				using (var stream = new FileStream(catalogFilePath, FileMode.Open, FileAccess.Read))
				{
					byte[] hashByte = hash.ComputeHash(bytes);
					stream.Close();
					var hash128Str = BitConverter.ToString(hashByte).Replace("-", "").ToLower();
					File.WriteAllText(catalogHashFilePath, hash128Str, EncodingExt.UTF8WithoutBom);
				}

				// update internal file
				// var internalHashFilePath = AssetHelper.GetInternalBuildPath() + "catalog.hash";
				// File.Copy(catalogHashFilePath, internalHashFilePath, true);
				// var internalCatalogFilePath = AssetHelper.GetInternalBuildPath() + "catalog.zip";
				// File.Copy(catalogFilePath, internalCatalogFilePath, true);
				var internalBuildPath = AssetHelper.GetInternalBuildPath();
				var curFiles = Directory.GetFiles(internalBuildPath);
				foreach (var curFile in curFiles)
				{
					File.Delete(curFile);
				}

				var files = Directory.GetFiles(folderPath);
				foreach (var file in files)
				{
					var destFilePath = $"{internalBuildPath}{Path.GetRelativePath(folderPath, file)}";
					File.Copy(file, destFilePath, true);
				}

				// add file records
				var writeList = new WriteRecords();
				foreach (var tagBundle in tagBundles)
				{
					writeList.records.Add(tagBundle.GetBundlePathWithHash(folderPath));
				}

				writeList.records.Add(catalogFilePath);
				writeList.records.Add(catalogHashFilePath);

				if (File.Exists(buildlogDestPath))
				{
					writeList.records.Add(buildlogDestPath);
				}

				{
					var writeListContent = JsonUtility.ToJson(writeList);
					File.WriteAllText(writeListFilePath, writeListContent, EncodingExt.UTF8WithoutBom);
				}

				// gen code hint
				var codeOutputDir = "Assets/Bundles/GameConfigs/MiiConfigs/";
				var codeFileName = "MiiAssetHint.cs";
				var codeFilePath = $"{codeOutputDir}/{codeFileName}";
				if (!Directory.Exists(codeOutputDir))
				{
					Directory.CreateDirectory(codeOutputDir);
				}

				string ToFirstUpperCase(string tag)
				{
					var key = char.ToUpper(tag[0]) + tag[1..];
					return key;
				}

				var assetTags = string.Join("", tagOrderMap.Keys.Select(tag => $@"		public static string {ToFirstUpperCase(tag)} = ""{tag}"";
"));
				// var sceneKeys = string.Join("",tagBundles.Where(tagBundle=>tagBundle.Tags.Contains("scene")).Select(tagBundle=>tagBundle.))
				var content = @$"
namespace MiiAssetHint
{{
	public interface AssetTags
	{{
{assetTags}
	}}

	public interface SceneKeys
	{{
	}}
}}
";
				var content0 = File.ReadAllText(codeFilePath, Encoding.UTF8);
				if (content0 != content)
				{
					File.WriteAllText(codeFilePath, content, EncodingExt.UTF8WithoutBom);
				}

				// refersh
				AssetDatabase.Refresh();
			}

			return new BuildAssetBundlesResult
			{
				BuildResult = buildResult,
				Msg = "",
				Code = buildResult.ExitCode
			};
		}

		[MenuItem("Tools/BuildAssetBundlesWithPathInfo")]
		internal static void BuildAssetBundlesWithPathInfo1()
		{
			BuildAssetBundlesWithPathInfo();
			Debug.Log("Build Done.");
		}

		public static BuildAssetBundlesResult BuildAssetBundlesWithPathInfo()
		{
			ScriptCompilationSettings scriptCompilationSettings = new()
			{
				target = EditorUserBuildSettings.activeBuildTarget,
				group = EditorUserBuildSettings.selectedBuildTargetGroup,
				options = ScriptCompilationOptions.DevelopmentBuild,
			};

			var pathInfo = AAPathConfigLoader.LoadDefaultConfigs();
			var buildAssetBundlesResult = BuildAssetBundles(pathInfo, scriptCompilationSettings, new()
			{
			});
			return buildAssetBundlesResult;
		}
	}
}