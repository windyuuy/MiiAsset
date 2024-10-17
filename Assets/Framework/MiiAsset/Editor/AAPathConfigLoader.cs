using System.Linq;
using System.Text.RegularExpressions;
using Framework.MiiAsset.Runtime;
using UnityEditor;

namespace MiiAsset.Editor.Optimization
{
    public class AAPathConfigLoader
    {
        public static AAPathInfo LoadConfig(string configPath)
        {
            var aaPathConfig = AssetDatabase.LoadAssetAtPath<AAPathConfig>(configPath);
            var paths = aaPathConfig.paths.ToList();
            paths.Sort((p1, p2) => p2.scanRoot.Length - p1.scanRoot.Length);
            paths.ForEach((item) => { item.pathRegex = new Regex(item.path); });

            var pathInfo = new AAPathInfo();
            pathInfo.Paths = paths;
            pathInfo.ExcludePaths = aaPathConfig.excludePaths;
            pathInfo.ExcludeExtensions = aaPathConfig.excludeExtensions;
            pathInfo.IsShaderGroupOffline = aaPathConfig.isShaderGroupOffline;
            pathInfo.IsMyBuiltinShaderGroupOffline = aaPathConfig.isMyBuiltinShaderGroupOffline;
            return pathInfo;
        }

        public static string GetDefaultConfigPath()
        {
            var guids = AssetDatabase.FindAssets("t:AAPathConfig", new[]
            {
                "Assets/Bundles/GameConfigs/Editor/AAConfig/",
                "Assets/Editor/AAConfig/",
            });
            var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return assetPath;
        }

        public static AAPathConfig LoadDefaultConfig()
        {
            var assetPath = GetDefaultConfigPath();
            var aaPathConfig = AssetDatabase.LoadAssetAtPath<AAPathConfig>(assetPath);
            return aaPathConfig;
        }

        public static AAPathInfo LoadDefaultConfigs()
        {
            var assetPath = GetDefaultConfigPath();
            return LoadConfig(assetPath);
        }
    }
}