using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using EditorLib.ShellUtils;
using HybridCLR.Editor;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Player;
using UnityEngine;

namespace HatNetwork.Editor
{
	public class CLRBuildAssetBundle : MonoBehaviour
	{
		[MenuItem("HybridCLR/CompileDll/ActiveBuildTarget2", priority = 100)]
		[MenuItem("Tools/DotnetHotLoader/ActiveBuildTarget2", priority = 100)]
		public static void CompileDllActiveBuildTarget()
		{
			CompileDll(EditorUserBuildSettings.activeBuildTarget);
		}

		public static void CompileDll(BuildTarget target)
		{
			CompileDll(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target), target);
		}

		public static void CompileDll(string buildDir, BuildTarget target)
		{
			var group = UnityEditor.BuildPipeline.GetBuildTargetGroup(target);

			ScriptCompilationSettings scriptCompilationSettings = new ScriptCompilationSettings();
			scriptCompilationSettings.group = group;
			scriptCompilationSettings.target = target;
			Directory.CreateDirectory(buildDir);
			ScriptCompilationResult scriptCompilationResult =
				PlayerBuildInterface.CompilePlayerScripts(scriptCompilationSettings, buildDir);
			foreach (var ass in scriptCompilationResult.assemblies)
			{
				//Debug.LogFormat("compile assemblies:{1}/{0}", ass, buildDir);
			}

			Debug.Log("compile finish!!!");
		}

		public static string StreamABOutPAthDir => $"{Application.streamingAssetsPath}/HLtLib/";

		public static void CleanDllBundles()
		{
			if (Directory.Exists(StreamABOutPAthDir))
			{
				Directory.Delete(StreamABOutPAthDir, true);
			}

			Directory.CreateDirectory(StreamABOutPAthDir);
		}

		public static void CopyDest(string copyDest, string ResUpdateTunnel, long versionCode)
		{
			if (!Directory.Exists(copyDest))
			{
				Directory.CreateDirectory(copyDest);
			}

			var prefix = "";
			if (!string.IsNullOrEmpty(ResUpdateTunnel))
			{
				prefix = $"{ResUpdateTunnel}_";
			}

			MyAddressablesUtils.CopyDirectory($"{StreamABOutPAthDir}{versionCode}/", copyDest, prefix, true, true);
		}

		public static void AdaptAppV1(string copyDest, string ResUpdateTunnel, long versionCode)
		{
			var dir = new DirectoryInfo(copyDest);
			var files = dir.GetFiles()
				.Where(file => file.Name.StartsWith($"{ResUpdateTunnel}_") && file.Name.EndsWith("hatill")).ToArray();
			foreach (var file in files)
			{
				var extraFile = $"{file.Directory.FullName}/{file.Name.Substring(ResUpdateTunnel.Length + 1)}";
				File.Copy(file.FullName, extraFile, true);
			}
		}

		public static void CopyRuntimeLib(string copyDest)
		{
		#if UNITY_IOS ||UNITY_STANDALONE_OSX||UNITY_IPHONE
			var appleLibPath = "./HybridCLRData/iOSBuild/build/libil2cpp.a";
			var destPath = $"{copyDest}/Libraries/";
			File.Copy(appleLibPath,destPath,true);
		#endif
		}

		[MenuItem("Tools/DotnetHotLoader/CompileIL2CPPForIOS")]
		public static bool CompileIL2CPPForIOS()
		{
			return ShellUtils.ExecuteCmdline("构建il2cpp.a", "构建il2cpp.a", "bash", "./build_libil2cpp.sh",
				"./HybridCLRData/iOSBuild",
				s =>
				{
					if (s.Contains(" error: "))
					{
						return true;
					}

					return false;
				});
		}

		public static ReturnCode BuildAssetBundles(IBundleBuildParameters parameters, IBundleBuildContent content,
			out IBundleBuildResults result)
		{
			var buildTasks = new List<IBuildTask>();

			// Setup
			buildTasks.Add(new SwitchToBuildPlatform());

			// Player Scripts
			// buildTasks.Add(new BuildPlayerScripts());
			buildTasks.Add(new PostScriptsCallback());

			// Dependency
			buildTasks.Add(new CalculateSceneDependencyData());
// #if UNITY_2019_3_OR_NEWER
// 			buildTasks.Add(new CalculateCustomDependencyData());
// #endif
			buildTasks.Add(new CalculateAssetDependencyData());
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
			buildTasks.Add(new AppendBundleHash());
			// buildTasks.Add(new GenerateLinkXml());
			buildTasks.Add(new PostWritingCallback());
			// var taskList = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.PlayerScriptsOnly);
			return ContentPipeline.BuildAssetBundles(parameters, content, out result, buildTasks);
		}

		/// <summary>
		/// 按照程序集依赖关系重新排序, 确保加载顺序正确, 避免手工排序出错
		/// </summary>
		/// <param name="dlls"></param>
		public static string[] ResortAssemblyOrders(string[] dlls, bool needNormalize)
		{
			var depsMap = new Dictionary<Assembly, HashSet<Assembly>>();
			var dllNames = needNormalize ? dlls.Select(dll => Path.GetFileNameWithoutExtension(dll)).ToArray() : dlls;
			var ass = AppDomain.CurrentDomain.GetAssemblies();
			var inputAss = new List<Assembly>();
			foreach (var dllName in dllNames)
			{
				var dll = ass.FirstOrDefault(a => a.GetName().Name == dllName);
				if (dll != null)
				{
					inputAss.Add(dll);
				}
				else
				{
					try
					{
						dll = Assembly.Load(dllName);
						if (dll != null)
						{
							inputAss.Add(dll);
						}
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}

			foreach (var a in inputAss)
			{
				var referAssNames = a.GetReferencedAssemblies().Select(a => a.Name)
					.Where(name => dllNames.Contains(name)).ToArray();
				var referAss = ass.Where(a => referAssNames.Contains(a.GetName().Name)).ToHashSet();
				depsMap[a] = referAss;
			}

			var noDepsOrder = new Queue<Assembly>();
			var orderedAss = new Queue<Assembly>();
			while (depsMap.Count > 0)
			{
				// 准备缓存
				noDepsOrder.Clear();

				// 检查所有无依赖项
				foreach (var kv in depsMap)
				{
					if (kv.Value.Count == 0)
					{
						noDepsOrder.Enqueue(kv.Key);
					}
				}

				if (noDepsOrder.Count == 0)
				{
					throw new Exception("wrong deps map");
				}

				var noDepsOrderArr = noDepsOrder.OrderBy(a => Array.IndexOf(dllNames, a.GetName().Name));
				// 转当前无依赖项 
				foreach (var a in noDepsOrderArr)
				{
					orderedAss.Enqueue(a);
					depsMap.Remove(a);
				}

				// 清除当前无依赖项所有被依赖关系
				foreach (var a in noDepsOrder)
				{
					foreach (var depKV in depsMap)
					{
						if (depKV.Value.Contains(a))
						{
							depKV.Value.Remove(a);
						}
					}
				}
			}

			var orderedDlls = orderedAss.Select(a => a.GetName().Name + ".dll").ToArray();

			return orderedDlls;
		}

		/// <summary>
		/// 分析程序集依赖
		/// </summary>
		public static string[] AnalyzeAssemblyDeps(string[] dlls, string[] limits)
		{
			var dllNames = dlls.Select(dll => Path.GetFileNameWithoutExtension(dll)).ToArray();
			var limitNames = limits.Select(dll => Path.GetFileNameWithoutExtension(dll)).ToArray();
			var ass = AppDomain.CurrentDomain.GetAssemblies();
			var inputAss = ass.Where(a => dllNames.Contains(a.GetName().Name)).ToArray();
			var allAss = new List<Assembly>(inputAss);
			foreach (var a in inputAss)
			{
				var referAssNames = a.GetReferencedAssemblies().Select(a => a.Name);
				var referAss = ass.Where(a => referAssNames.Contains(a.GetName().Name)).ToArray();
				allAss.AddRange(referAss);
			}

			var depNames = allAss.Distinct()
				.Select(a => a.GetName().Name)
				.Where(name => limitNames.Contains(name))
				.ToArray();
			var depDlls = depNames.Select(name => name + ".dll").Concat(dlls).Distinct().ToArray();
			depDlls = depDlls.OrderBy(dll => Array.IndexOf(limits, dll)).ToArray();
			return depDlls;
		}

		// [MenuItem("Tools/DotnetHotLoader/Test")]
		public static void TestResort()
		{
			var hotDlls0 = SettingsUtil.HotUpdateAssemblyNamesIncludePreserved.ToArray();
			var hotDlls = ResortAssemblyOrders(hotDlls0, false);

			// test only
			var preDlls = new string[]
			{
				// "HatLoader.dll",
				"GameLib.dll",
				"GameUILib.dll",
			};
			preDlls = AnalyzeAssemblyDeps(preDlls, hotDlls);
		}

		/// <summary>
		/// 此程序集必须热更
		/// </summary>
		const string HatLoaderAssemblyName = "MiiAsset.HatLoader.dll";

		public static void BuildDllsAB(BuildPlayerOptions BuildPlayerOptions, string ResUpdateTunnel, long versionCode)
		{
			var hotDlls = SettingsUtil.HotUpdateAssemblyFilesExcludePreserved.Append(HatLoaderAssemblyName).Distinct().ToArray();
			hotDlls = ResortAssemblyOrders(hotDlls, true);
			// var atoDlls0 = new string[]
			// {
			// 	"System.Core.dll",
			// 	"System.dll",
			// 	"UnityEngine.AndroidJNIModule.dll",
			// 	"mscorlib.dll",
			// 	"Google.Protobuf.dll",
			// 	"MemoryPack.Core.dll",
			// 	"SDKAdapter.Common.dll",
			// 	"UnityEngine.CoreModule.dll",
			// };
			var aotDlls0 = Array.Empty<string>();
			if (CLRBuildConfig.Load(out var config))
			{
				aotDlls0 = config.aotAssemblies;
			}

			var aotDlls = aotDlls0.Where(dll => !hotDlls.Contains(dll)).ToArray();
			// var preDlls = new string[]
			// {
			// 	// "HatLoader.dll",
			// 	"GameLib.dll",
			// 	"GameUILib.dll",
			// 	"NativeMG.dll",
			// 	"UISys.dll",
			// 	"AppConfigs.dll",
			// };
			var preDlls = hotDlls.ToArray();
			preDlls = AnalyzeAssemblyDeps(preDlls, hotDlls);

			// 判断文件夹是否存在，不存在则新建
			if (Directory.Exists(StreamABOutPAthDir) == false)
			{
				Directory.CreateDirectory(StreamABOutPAthDir);
			}

			// 打热更AB
			{
				var hotDllsWithoutPre = hotDlls.Where(s => !preDlls.Contains(s)).ToArray();
				var dest2 = CopyDllAB("hatill", ResUpdateTunnel, versionCode, hotDllsWithoutPre, new string[0], 0);
				AssetDatabase.Refresh();

				var hatillGroup = MarkDestGroup(dest2, "hatill", ResUpdateTunnel);
				hatillGroup.addressableNames = hatillGroup.assetNames
					.Select(p => Path.GetFileNameWithoutExtension(p))
					.ToArray();
				var abs = new AssetBundleBuild[]
				{
					hatillGroup,
				};

				// 打包生成AB包 (目标平台根据需要设置即可)
				var buildParams = new BundleBuildParameters(BuildPlayerOptions.target, BuildPlayerOptions.targetGroup,
					StreamABOutPAthDir + $"{versionCode}/")
				{
					DisableVisibleSubAssetRepresentations = true,
				};
				var exitCode = BuildAssetBundles(buildParams, new BundleBuildContent(abs), out var results);
				if (exitCode == ReturnCode.Success)
				{
					Debug.Log("BuildAllDllsAB done");
				}
				else
				{
					Debug.Log("BuildAllDllsAB failed");
				}
			}

			// 打prehotab
			{
				var hotPath = StreamABOutPAthDir + $"{versionCode}/hatill";
				var hotSize = new FileInfo(hotPath).Length;

				var hotLoaderDll = hotDlls.Where(s => preDlls.Contains(s)).ToArray();
				var dest1 = CopyDllAB("prehatill", ResUpdateTunnel, versionCode, hotLoaderDll, aotDlls, hotSize);
				AssetDatabase.Refresh();

				var prehatillGroup = MarkDestGroup(dest1, "prehatill", ResUpdateTunnel);
				var hatLoaderPrefabPath = GetHatLoaderPrefabPath();
				prehatillGroup.assetNames = prehatillGroup.assetNames.Prepend(hatLoaderPrefabPath)
					.ToArray();
				prehatillGroup.addressableNames = prehatillGroup.assetNames
					.Select(p => Path.GetFileNameWithoutExtension(p))
					.ToArray();
				var abs = new AssetBundleBuild[]
				{
					prehatillGroup,
				};

				// 打包生成AB包 (目标平台根据需要设置即可)
				var buildParams = new BundleBuildParameters(BuildPlayerOptions.target, BuildPlayerOptions.targetGroup,
					StreamABOutPAthDir + $"{versionCode}/")
				{
					DisableVisibleSubAssetRepresentations = true,
				};
				var exitCode = BuildAssetBundles(buildParams, new BundleBuildContent(abs), out var results);
				if (exitCode == ReturnCode.Success)
				{
					Debug.Log("BuildAllAB done");
				}
				else
				{
					Debug.Log("BuildAllAB failed");
				}
			}

			File.WriteAllText($"{StreamABOutPAthDir}version.txt", $"{versionCode}");
		}

		public static void BuildOtherABs(BuildPlayerOptions BuildPlayerOptions)
		{
			var abs = new AssetBundleBuild[]
			{
				new AssetBundleBuild()
				{
					assetBundleName = "testhot",
					assetNames = new string[] { "Assets/HotTest/Prefabs/TestHotUpdate.prefab" },
				},
			};
			// 打包生成AB包 (目标平台根据需要设置即可)
			var buildParams = new BundleBuildParameters(BuildPlayerOptions.target, BuildPlayerOptions.targetGroup,
				StreamABOutPAthDir)
			{
			};
			var exitCode = BuildAssetBundles(buildParams, new BundleBuildContent(abs), out var results);
			if (exitCode == ReturnCode.Success)
			{
				Debug.Log("BuildOtherABs done");
			}
			else
			{
				Debug.Log("BuildOtherABs failed");
			}
		}

		public static AssetBundleBuild MarkDestGroup(string dest, string groupName, string ResUpdateTunnel)
		{
			var prefabAssets = Directory.GetFiles(dest).Where(p => AssetImporter.GetAtPath(p) != null).ToArray();
			var projectPath = Path.GetDirectoryName(Application.dataPath);
			var ab = new AssetBundleBuild
			{
				assetBundleName = groupName,
				assetNames = prefabAssets.Select(s => Path.GetRelativePath(projectPath, s)).ToArray(),
			};
			return ab;
		}

		public static BinManifest.FileInfo GetFileInfo(string name, string path)
		{
			var bytes = File.ReadAllBytes(path);
			var hashTool = new Hash128();
			hashTool.Append(bytes);
			var hash = hashTool.ToString();
			return new BinManifest.FileInfo()
			{
				hash = hash,
				name = name,
			};
		}

		public static string GetPDBName(string dllName)
		{
			var fileName = Path.GetFileNameWithoutExtension(dllName);
			return $"{fileName}.pdb";
		}

		public static string CopyDllAB(string groupName, string ResUpdateTunnel, long versionCode, string[] hotDlls0,
			string[] aotDlls0, long size0)
		{
			var copyDest = $"Assets/HybridCLRData/HotDll/{groupName}";
			if (Directory.Exists(copyDest))
			{
				Directory.Delete(copyDest, true);
			}

			if (!Directory.Exists(copyDest))
			{
				Directory.CreateDirectory(copyDest);
			}

			var target = EditorUserBuildSettings.activeBuildTarget;
			var aotCopySource = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
			var hotCopySource = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
			foreach (var aotDll in aotDlls0)
			{
				if (File.Exists($"{aotCopySource}/{aotDll}"))
				{
					File.Copy($"{aotCopySource}/{aotDll}", $"{copyDest}/{aotDll}.txt", true);
				}
				else
				{
					Debug.LogError($"缺少 aot dll: {aotCopySource}/{aotDll}, 请重试打包一次");
				}
			#if SUPPORT_PDB
			if (File.Exists($"{aotCopySource}/{GetPDBName(aotDll)}"))
			{
				File.Copy($"{aotCopySource}/{GetPDBName(aotDll)}", $"{copyDest}/{GetPDBName(aotDll)}.txt", true);
			}
			#endif
			}

			foreach (var hotDll in hotDlls0)
			{
				File.Copy($"{hotCopySource}/{hotDll}", $"{copyDest}/{hotDll}.txt", true);
			#if SUPPORT_PDB
			if (File.Exists($"{hotCopySource}/{GetPDBName(hotDll)}"))
			{
				File.Copy($"{hotCopySource}/{GetPDBName(hotDll)}", $"{copyDest}/{GetPDBName(hotDll)}.txt", true);
			}
			#endif
			}

			var aotDlls = aotDlls0
				.Where(name => File.Exists($"{aotCopySource}/{name}"))
				.Select(name => GetFileInfo(name, $"{aotCopySource}/{name}")).ToArray();
			var hotDlls = hotDlls0
				.Select(name => GetFileInfo(name, $"{hotCopySource}/{name}")).ToArray();
			var manifest = new BinManifest()
			{
				ResUpdateTunnel = ResUpdateTunnel,
				versionCode = versionCode,
				aotDlls = aotDlls,
				hotDlls = hotDlls,
				hotSize = size0,
			};
			var manifestText = JsonUtility.ToJson(manifest);
			File.WriteAllText($"{copyDest}/manifest.txt", manifestText, Encoding.UTF8);

			return copyDest;
		}


		[System.Serializable]
		public class ScriptingAssemblies
		{
			public List<string> names;
			public List<int> types;
		}

		class AssemblyDefinitionData
		{
			public string name;
		}


		[MenuItem("Tools/DotnetHotLoader/TestModifyStreamAssemblyLimitation", false, 1)]
		public static void ModifyStreamAssemblyLimitation()
		{
			var reg = new Regex(@"PHotil(\d+)");

			var usedPHotilSet = new HashSet<string>();

			var hotUpdateAssemblyDefinitions = HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions;
			for (int ii2 = 0; ii2 < hotUpdateAssemblyDefinitions.Length; ii2++)
			{
				var adef = hotUpdateAssemblyDefinitions[ii2];
				if (adef.text.Contains("PHotil"))
				{
					var aname = JsonUtility.FromJson<AssemblyDefinitionData>(adef.text).name;
					if (reg.IsMatch(aname))
					{
						usedPHotilSet.Add(adef.name);
					}
				}
			}

			for (var i = 0; i < 160; i++)
			{
				usedPHotilSet.Add($"PHotil{i}");
			}

			var hotUpdateAssemblies = SettingsUtil.HotUpdateAssemblyFilesExcludePreserved.ToArray();
			var maxIndex = 0;
			if (hotUpdateAssemblies.Length > 0)
			{
				maxIndex = hotUpdateAssemblies.Max(ass =>
				{
					var m = reg.Match(ass);
					if (m.Success)
					{
						usedPHotilSet.Remove(m.Groups[0].Value);
						var num = m.Groups[1];
						var n = int.Parse(num.Value);
						return n + 1;
					}

					return 0;
				});
			}

			RecorverStreamAssemblyLimitation();
			var manuals = HybridCLRSettings.Instance.preserveHotUpdateAssemblies.ToArray();
			HybridCLRSettings.Instance.preserveHotUpdateAssemblies =
				manuals.Concat(usedPHotilSet.ToArray()).Distinct().ToArray();
			// EditorUtility.SetDirty(HybridCLRSettings.Instance);
			// AssetDatabase.SaveAssetIfDirty(HybridCLRSettings.Instance);
		}

		[MenuItem("Tools/DotnetHotLoader/TestRecorverStreamAssemblyLimitation", false, 1)]
		public static void RecorverStreamAssemblyLimitation()
		{
			var reg = new Regex(@"PHotil(\d+)");

			var manuals = HybridCLRSettings.Instance.preserveHotUpdateAssemblies
				.Where(name => !name.StartsWith("PHotil"))
				.Distinct()
				.ToList();
			var hotUpdateAssemblyDefinitions = HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions;
			for (int ii2 = 0; ii2 < hotUpdateAssemblyDefinitions.Length; ii2++)
			{
				var adef = hotUpdateAssemblyDefinitions[ii2];
				if (adef.text.Contains("PHotil"))
				{
					var aname = JsonUtility.FromJson<AssemblyDefinitionData>(adef.text).name;
					if (reg.IsMatch(aname))
					{
						manuals.Remove(adef.name);
					}
				}
			}

			HybridCLRSettings.Instance.preserveHotUpdateAssemblies = manuals.ToArray();
			// EditorUtility.SetDirty(HybridCLRSettings.Instance);
			// AssetDatabase.SaveAssetIfDirty(HybridCLRSettings.Instance);
		}

		public static string GetHatLoaderPrefabPath()
		{
			var localPath = "Assets/Framework/AHotLoader/HotLoader/HatLoader.prefab";
			return localPath;
		}
	}
}