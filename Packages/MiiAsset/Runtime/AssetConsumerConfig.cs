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
        [Header("热更资源缓存路径")]
        public string bundleCacheDir = "hotres/";
        public string catalogName = "catalog";
        public string catalogType = "zip";
        [Header("热更通道")]
        [Tooltip("会打进包里，热更通道一致，才能通过升级版本号来升级热更资源")]
        public string updateTunnel = "default";
        [Header("资源加载方式(仅编辑器)")]
        [Tooltip("加载模式(仅编辑器中生效):\n\n1. LoadFromEditor: 直接通过编辑器加载资源\n  \n2. LoadFromBundle: 从发布的AssetBundle加载资源，用于模拟测试复现问题")]
        public LoadType loadType = LoadType.LoadFromEditor;
        public int initDownloadCoCount = 10;
        [Header("最大下载线程数")]
        public int maxDownloadCoCount = 50;
        [Header("网络超时时长")]
        public int timeout = 300;

        [Header("构建Guid映射")]
        [Tooltip("是否在构建中包含资源Guid信息, 会显著增大catalog尺寸")]
        public bool buildGuids = false;

        [Header("是否构建索引代码")]
        [Tooltip("可以通过生成的索引代码引用资源Tag等, 或观察bundle和依赖变化")]
        public bool buildCodeHint = true;
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

#if UNITY_EDITOR
        public static bool LoadInEditor(out AssetConsumerConfig config)
        {
            var configGuids =
                UnityEditor.AssetDatabase.FindAssets("t:AssetConsumerConfig", new[] { "Assets", "Packages/windy.miiasset" });
            if (configGuids.Length > 0)
            {
                var configGuid = configGuids[0];
                config =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AssetConsumerConfig>(UnityEditor.AssetDatabase.GUIDToAssetPath(configGuid));
                return true;
            }
            else
            {
                Debug.LogError("no consumer config found");
                config = null;
                return false;
            }
        }
#endif
    }
}