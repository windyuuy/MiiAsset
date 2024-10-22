using System;
using UnityEngine;

namespace MiiAsset.Runtime
{
    [Serializable]
    [CreateAssetMenu(fileName = "AssetConsumerConfig", menuName = "Mii/AssetConsumerConfig", order = 0)]
    public class AssetConsumerConfig : ScriptableObject, IAssetProvider.IProviderInitOptions
    {
        public enum LoadType
        {
            LoadFromBundle = 0,
            LoadFromEditor = 1,
        }

        public string internalBaseUri = "mii/";
        public string externalBaseUri = "mii/";
        public string bundleCacheDir = "hotres/";
        public string catalogName = "catalog";
        public string catalogType = "zip";
        public string updateTunnel = "default";
        public LoadType loadType = LoadType.LoadFromEditor;
        public int initDownloadCoCount = 10;
        public int maxDownloadCoCount = 50;
        [Header("网络超时时长")]
        public int timeout = 300;

        [Header("构建Guid映射")]
        [Tooltip("是否在构建中包含资源Guid信息, 会显著增大catalog尺寸")]
        public bool buildGuids = false;
        public string InternalBaseUri => internalBaseUri;
        public string ExternalBaseUri => externalBaseUri;
        public string BundleCacheDir => bundleCacheDir;
        public string CatalogName => $"{catalogName}_{updateTunnel}.{catalogType}";
        public int InitDownloadCoCount => initDownloadCoCount;
        public int MaxDownloadCoCount => maxDownloadCoCount;
        public int Timeout => timeout;

        public static AssetConsumerConfig Load()
        {
            var config = Resources.Load<AssetConsumerConfig>("MiiConfig/AssetConsumerConfig");
            return config;
        }
//
// #if UNITY_EDITOR
//         public static AssetConsumerConfig LoadInEditor()
//         {
//             var configGuids = UnityEditor.AssetDatabase.FindAssets("t:AssetConsumerConfig",
//                 new[] { "Assets", "Packages/windy.miiasset" });
//             if (configGuids.Length > 0)
//             {
//                 var configGuid = configGuids[0];
//                 var config =
//                     UnityEditor.AssetDatabase.LoadAssetAtPath<AssetConsumerConfig>(
//                         UnityEditor.AssetDatabase.GUIDToAssetPath(configGuid));
//                 return config;
//             }
//             else
//             {
//                 Debug.LogError("no consumer config found");
//                 return null;
//             }
//         }
// #endif
    }
}