using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MiiAsset.Runtime;
using UnityEditor;

namespace MiiAsset.Editor.Optimization
{
	public class AAPathConfigLoader
	{
		public static AAPathInfo LoadConfig(AAPathInfo pathInfo, string configPath)
		{
			var aaPathConfig = AssetDatabase.LoadAssetAtPath<AAPathConfig>(configPath);
			var paths = aaPathConfig.paths.ToList();
			paths.Sort((p1, p2) => p2.scanRoot.Length - p1.scanRoot.Length);
			paths.ForEach((item) => { item.pathRegex = new Regex(item.path); });

			pathInfo.IsEncryptAll = pathInfo.IsEncryptAll || aaPathConfig.isEncryptAll;
			pathInfo.IsEncryptBuiltin = pathInfo.IsEncryptBuiltin || aaPathConfig.isEncryptBuiltin;
			pathInfo.Paths.AddRange(paths);
			pathInfo.ExcludePaths.AddRange(aaPathConfig.excludePaths);
			pathInfo.ExcludeExtensions.AddRange(aaPathConfig.excludeExtensions);
			pathInfo.IsShaderGroupOffline = pathInfo.IsShaderGroupOffline || aaPathConfig.isShaderGroupOffline;
			pathInfo.IsMyBuiltinShaderGroupOffline =
				pathInfo.IsMyBuiltinShaderGroupOffline || aaPathConfig.isMyBuiltinShaderGroupOffline;
			foreach (var singleFile in aaPathConfig.singleFiles)
			{
				var path = AssetDatabase.GetAssetPath(singleFile.asset);
				var guid = AssetDatabase.AssetPathToGUID(path);
				pathInfo.SingleFileItems.Add(guid, singleFile);
			}

			return pathInfo;
		}

		public static string[] GetDefaultConfigPath()
		{
			var guids = AssetDatabase.FindAssets("t:AAPathConfig", new[]
			{
				"Assets/Bundles/GameConfigs/Editor/AAConfig/",
				"Assets/Editor/AAConfig/",
			});
			if (guids.Length == 0)
			{
				return null;
			}

			var assetPaths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
			return assetPaths;
		}

		// public static AAPathConfig LoadDefaultConfig()
		// {
		// 	var assetPath = GetDefaultConfigPath();
		// 	var aaPathConfig = AssetDatabase.LoadAssetAtPath<AAPathConfig>(assetPath);
		// 	return aaPathConfig;
		// }

		public static AAPathInfo LoadDefaultConfigs()
		{
			var assetPaths = GetDefaultConfigPath();
			if (assetPaths == null)
			{
				var defaultAAPathInfo = new AAPathInfo()
				{
					Paths = new List<AAPathConfigItem>(),
					ExcludeExtensions = new(),
					ExcludePaths = new(),
					IsShaderGroupOffline = false,
					IsMyBuiltinShaderGroupOffline = false
				};
				return defaultAAPathInfo;
			}

			var pathInfo = new AAPathInfo();
			foreach (var assetPath in assetPaths)
			{
				LoadConfig(pathInfo, assetPath);
			}

			return pathInfo;
		}
	}
}