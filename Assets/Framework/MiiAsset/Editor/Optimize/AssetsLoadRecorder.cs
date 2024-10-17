using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Framework.MiiAsset.Runtime;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MiiAsset.Runtime.Optimization
{
    [Serializable]
    public class AALoadOrderBatchConfig
    {
        public int index;
        public string name;
        public string[] batch = Array.Empty<string>();

        public string BatchName => string.IsNullOrWhiteSpace(name) ? $"batch{index}" : name;

        public AALoadOrderBatchConfig(int index1)
        {
            this.index = index1;
        }

        public void AddRecord(string key)
        {
            this.batch = this.batch.Append(key).ToArray();
        }
    }

    [Serializable]
    public class AALoadOrderConfig
    {
        public AALoadOrderBatchConfig[] batches = Array.Empty<AALoadOrderBatchConfig>();

        public AALoadOrderBatchConfig GetBatch(int index)
        {
            return batches[index];
        }

        public static readonly AALoadOrderConfig Default = new();
    }

    public class AssetsOrderLoader
    {
        public static readonly string LoadPath = "Assets/Bundles/GameConfigs/AALoaderConfigs/AALoadConfig.json.txt";
        protected static AALoadOrderConfig Config;

        public static AALoadOrderConfig LoadOrderConfig()
        {
            if (!File.Exists(LoadPath))
            {
                return null;
            }

            var content = File.ReadAllText(LoadPath, Encoding.UTF8);
            var target = JsonUtility.FromJson<AALoadOrderConfig>(content);
            return target;
        }
    }

    public class AssetsOrderRecorder
    {
        public AssetsOrderRecorder()
        {
            BundleStatusNotify.OnBundleLoad -= OnBundleLoad;
            BundleStatusNotify.OnBundleLoad += OnBundleLoad;
        }

        public void OnBundleLoad(AssetBundleStatus status)
        {
            Debug.Assert(status.AssetBundle != null, $"status.AssetBundle!=null, {status.BundleName}");
            this.AddRecords(status.BundleName);
        }

        public static readonly AssetsOrderRecorder Inst = new();

        protected AALoadOrderConfig AALoadOrderConfig = new();
        protected int RecordIndex = 0;
        protected Dictionary<string, string> FilterMap = new();

        [Conditional("UNITY_EDITOR")]
        public void AddRecords(string record)
        {
            var name = Path.GetFileNameWithoutExtension(record);
            var index = name.LastIndexOf("_", StringComparison.Ordinal);
            if (index == -1)
            {
                return;
            }

            var key = name.Substring(0, index);
            if (FilterMap.ContainsKey(key))
            {
                return;
            }

            if (AALoadOrderConfig.batches.Length <= RecordIndex)
            {
                AALoadOrderConfig.batches =
                    AALoadOrderConfig.batches.Append(new AALoadOrderBatchConfig(RecordIndex)).ToArray();
            }

            AALoadOrderConfig.batches[RecordIndex].AddRecord(key);
        }

        public void Clear()
        {
            AALoadOrderConfig = new();
            FilterMap.Clear();
            ResetIndex();
        }

        public void PushIndex()
        {
            RecordIndex++;
        }

        public void SaveRecords()
        {
            AALoadOrderConfig.batches[0].batch = AALoadOrderConfig.batches[0].batch.OrderBy(s => s).ToArray();
            var jsonStr = JsonUtility.ToJson(AALoadOrderConfig, true);
            var directoryName = Path.GetDirectoryName(AssetsOrderLoader.LoadPath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName!);
            }

            File.WriteAllText(AssetsOrderLoader.LoadPath, jsonStr, Encoding.UTF8);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        public void ResetIndex()
        {
            this.RecordIndex = 0;
        }

        public void LoadRecords()
        {
            AALoadOrderConfig = AssetsOrderLoader.LoadOrderConfig();
            foreach (var batchConfig in AALoadOrderConfig.batches)
            {
                batchConfig.batch = batchConfig.batch.Distinct()
                    .Where(se => !FilterMap.ContainsKey(se)).ToArray();
                foreach (var se in batchConfig.batch)
                {
                    FilterMap[se] = se;
                }
            }

            ResetIndex();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/MiiAsset/BundleBatchTools/SaveRecords", false, 10001)]
#endif
        [Conditional("UNITY_EDITOR")]
        public static void SaveRecords2()
        {
            AssetsOrderRecorder.Inst.SaveRecords();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/MiiAsset/BundleBatchTools/PushIndex", false, 10003)]
#endif
        [Conditional("UNITY_EDITOR")]
        public static void PushIndex2()
        {
            AssetsOrderRecorder.Inst.PushIndex();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/MiiAsset/BundleBatchTools/LoadRecords", false, 10004)]
#endif
        [Conditional("UNITY_EDITOR")]
        public static void LoadRecords2()
        {
            AssetsOrderRecorder.Inst.LoadRecords();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/MiiAsset/BundleBatchTools/ResetIndex", false, 10005)]
#endif
        [Conditional("UNITY_EDITOR")]
        public static void ResetIndex2()
        {
            AssetsOrderRecorder.Inst.ResetIndex();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/MiiAsset/BundleBatchTools/ClearRecords", false, 10006)]
#endif
        [Conditional("UNITY_EDITOR")]
        public static void Clear2()
        {
            AssetsOrderRecorder.Inst.Clear();
        }
    }
}