using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MiiAsset.Runtime;
using MiiAsset.Runtime.AssetUtils;
using MiiAsset.Runtime.IOManagers;
using Lang.Encoding;
using MiiAsset.Editor.Optimization;
using MiiAsset.Runtime.Optimization;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Player;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Build.Pipeline;
using UnityEngine.U2D;
using AssetBundleInfo = MiiAsset.Runtime.AssetBundleInfo;
using BuildOptions = UnityEditor.BuildOptions;
using BuildResult = UnityEditor.Build.Reporting.BuildResult;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace MiiAsset.Editor.Build
{
    public class BuildAssetBundlesResult
    {
        public string Msg;
        public ReturnCode Code;
        public BuildResult BuildResult;
    }

    public class ExtraBuildOptions
    {
        public string UpdateTunnel = "";
        public string CatalogType = "";
        public bool BuildGuids = false;
        public bool BuildHintCode = true;
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
        public class TagBundle
        {
            public HashSet<string> Guids = new();
            public string[] Tags;
            public string[] TagsAdditional;

            /// <summary>
            /// for debug
            /// </summary>
            public string[] Addresses => Guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();

            public string TagsUKey;
            public string BundleFileName => $"{GetTagsKey()}_{BuildInfo.Hash}.bundle";
            public HashSet<string> DepTagNames = new();
            public HashSet<string> Deps = new();
            public BundleDetails BuildInfo;
            public bool IsOffline = false;

            /// <summary>
            /// 多少byte
            /// </summary>
            public long FileSize;

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

        public static BuildAssetBundlesResult BuildAssetBundles(AAPathInfo pathInfo,
            ScriptCompilationSettings scriptCompilationSettings, ExtraBuildOptions options)
        {
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

            // collect sprites in spriteatlas
            var filterMap = new HashSet<string>();

            void CollectSpriteAtlas(HashSet<string> filterMap0)
            {
                var guids = AssetDatabase.FindAssets("t:spriteatlas", new string[] { "Assets" });
                foreach (var guid in guids)
                {
                    var spriteatlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid));
                    var objs = spriteatlas.GetPackables();
                    foreach (var o in objs)
                    {
                        filterMap0.Add(AssetDatabase.GetAssetPath(o));
                    }
                }
            }

            CollectSpriteAtlas(filterMap);

            foreach (var scanInfo in pathInfo.GetScanRootInfos())
            {
                var guids = AssetDatabase.FindAssets("", new[] { scanInfo.ScanRoot });
                var validGroupNameInfo = guids.Select(guid => (guid, assetPath: AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(item => !filterMap.Contains(item.assetPath))
                    .Where(item => AAPathInfo.IsValidAsset(pathInfo, item.assetPath))
                    .Select(item =>
                    {
                        if (string.IsNullOrEmpty(scanInfo.ScanRoot) || item.assetPath.StartsWith(scanInfo.ScanRoot))
                        {
                            var groupNameInfo = AAPathInfo.ParseGroupName(scanInfo.Item, item.assetPath, item.guid);
                            return groupNameInfo;
                        }

                        return null;
                    })
                    .Where(item => item != null);
                foreach (var groupNameInfo in validGroupNameInfo)
                {
                    if (guidBundleMap.ContainsKey(groupNameInfo.Guid))
                    {
                        continue;
                    }

                    groupNameInfo.Tags = groupNameInfo.Tags
                        .Select(tag => tag.ToLower())
                        .ToArray();
                    if (groupNameInfo.AssetPath.EndsWith(".unity"))
                    {
                        groupNameInfo.Tags = groupNameInfo.Tags.Append("scene").ToArray();
                    }

                    var tagsKey = ToTagsKey(groupNameInfo.Tags);
                    if (!tagBundleMap.TryGetValue(tagsKey, out var tagBundle))
                    {
                        tagBundle = new TagBundle()
                        {
                            Tags = groupNameInfo.Tags,
                            TagsAdditional = Array.Empty<string>(),
                            TagsUKey = tagsKey,
                        };
                        tagBundleMap.Add(tagsKey, tagBundle);
                    }

                    if (guidBundleMap.TryAdd(groupNameInfo.Guid, tagBundle))
                    {
                        tagBundle.IsOffline |= !groupNameInfo.IsRemote;
                        tagBundle.Guids.Add(groupNameInfo.Guid);

                        // Debug.LogError($"conflict item: {groupNameInfo.AssetPath}");
                    }
                    //
                    // guidBundleMap[groupNameInfo.Guid] = tagBundle;
                }
            }

            var tagBundles = tagBundleMap.Values.ToArray();

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
            var folderPath = $"AssetBundles/{AssetHelper.GetBuildTarget(EditorUserBuildSettings.activeBuildTarget)}";
            var outPath = "Temp/MiiAsset/AssetBundles";
            var tmpPath = "Temp/MiiAsset/Temp";
            if (Directory.Exists(outPath))
            {
                Directory.Delete(outPath, true);
            }

            BuildCompression bundleCompression;

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
            {
                bundleCompression = BuildCompression.LZ4;
            }
            else
            {
                bundleCompression = BuildCompression.LZMA;
            }

            IBundleBuildParameters buildParams =
                new BundleBuildParameters(EditorUserBuildSettings.activeBuildTarget,
                    EditorUserBuildSettings.selectedBuildTargetGroup, outPath)
                {
                    BundleCompression = bundleCompression,
                };
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
                    var fileSize = new FileInfo(item.Value.FileName).Length;
                    if (tagBundle != null)
                    {
                        tagBundle.BuildInfo = item.Value;
                        tagBundle.FileSize = fileSize;

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
                            TagsAdditional = Array.Empty<string>(),
                            TagsUKey = ToTagsKey(tags),
                            DepTagNames = new(),
                            BuildInfo = item.Value,
                            FileSize = fileSize,
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

                // add load batch tag
                {
                    var config = AssetsOrderLoader.LoadOrderConfig();
                    if (config != null)
                    {
                        var batchMap = new Dictionary<string, AALoadOrderBatchConfig>();
                        foreach (var batchConfig in config.batches)
                        {
                            foreach (var path in batchConfig.batch)
                            {
                                batchMap.TryAdd(path, batchConfig);
                            }
                        }

                        foreach (var tagBundle in tagBundles)
                        {
                            if (batchMap.TryGetValue(tagBundle.GetBundleName(), out var batchConfig))
                            {
                                tagBundle.TagsAdditional = tagBundle.TagsAdditional.Append(batchConfig.BatchName).ToArray();
                            }
                        }
                    }
                }

                // prepare out folder
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var internalBuildPath = AssetHelper.GetInternalBuildPath();

                // collect link.xml
                var m_Linker = UnityEditor.Build.Pipeline.Utilities.LinkXmlGenerator.CreateDefault();
                m_Linker.AddAssemblies(new[]
                    { typeof(AssetLoader).Assembly, typeof(IOManager).Assembly, typeof(WXAdapter).Assembly });

                foreach (var r in buildResult.Results.WriteResults)
                {
                    var resultValue = r.Value;
                    m_Linker.AddTypes(resultValue.includedTypes);
#if UNITY_2021_1_OR_NEWER
                    m_Linker.AddSerializedClass(resultValue.includedSerializeReferenceFQN);
#else
                        if (resultValue.GetType().GetProperty("includedSerializeReferenceFQN") != null)
                            m_Linker.AddSerializedClass(resultValue.GetType().GetProperty("includedSerializeReferenceFQN").GetValue(resultValue) as System.Collections.Generic.IEnumerable<string>);
#endif
                }

                m_Linker.AddTypes(typeof(AssetLoader));
                Directory.CreateDirectory(internalBuildPath + "/Link/");
                m_Linker.Save(internalBuildPath + "/Link/link.xml");

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
                        tags = tagBundle.Tags.Concat(tagBundle.TagsAdditional).ToArray(),
                        entries = tagBundle.GetAssetNames(),
                        guids = options.BuildGuids ? tagBundle.Guids.ToArray() : null,
                        IsOffline = tagBundle.IsOffline,
                        size = tagBundle.FileSize,
                    };
                }).ToArray();

                var catalog = new CatalogConfig
                {
                    bundleInfos = catalogBundleInfos,
                };
                var catalogFilePath =
                    $"{folderPath}/catalog_{options.UpdateTunnel}.{options.CatalogType}".Replace("_.", ".");
                var catalogHashFilePath = Path.ChangeExtension(catalogFilePath, "hash");
                var bytes = SaveCatalog(catalog, catalogFilePath);

                SaveCatalogHash(catalogFilePath, catalogHashFilePath);

                // update internal file
                var curFiles = Directory.GetFiles(internalBuildPath);
                foreach (var curFile in curFiles)
                {
                    File.Delete(curFile);
                }

                var internalCatalog = new CatalogConfig
                {
                    bundleInfos = catalogBundleInfos.Where(info => info.IsOffline).ToArray(),
                    EntryBundleMap = null
                };
                var internalCatalogFilePath = $"{internalBuildPath}{Path.GetRelativePath(folderPath, catalogFilePath)}";
                SaveCatalog(internalCatalog, internalCatalogFilePath);
                var internalCatalogHashFilePath = Path.ChangeExtension(internalCatalogFilePath, "hash");
                SaveCatalogHash(internalCatalogFilePath, internalCatalogHashFilePath);

                foreach (var bundleInfo in internalCatalog.bundleInfos)
                {
                    var fileName = bundleInfo.fileName;
                    var sourceFilePath = $"{folderPath}/{fileName}";
                    var destFilePath = $"{internalBuildPath}{fileName}";
                    File.Copy(sourceFilePath, destFilePath, true);
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
                if (options.BuildHintCode)
                {
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

                    string ToTagKey(string tag)
                    {
                        return ToFirstUpperCase(tag).Replace(".", "_");
                    }

                    var assetTags = string.Join("", tagOrderMap.Keys.Select(tag =>
                        $@"		public const string {ToTagKey(tag)} = ""{tag}"";
"));
                    // var sceneKeys = string.Join("",tagBundles.Where(tagBundle=>tagBundle.Tags.Contains("scene")).Select(tagBundle=>tagBundle.))
                    var content = @$"
namespace MiiAsset.MiiAssetHint
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
                    var needOverwrite = true;
                    if (File.Exists(codeFilePath))
                    {
                        var content0 = File.ReadAllText(codeFilePath, Encoding.UTF8);
                        needOverwrite = content0 != content;
                    }

                    if (needOverwrite)
                    {
                        File.WriteAllText(codeFilePath, content, EncodingExt.UTF8WithoutBom);
                    }
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

        private static void SaveCatalogHash(string catalogFilePath, string catalogHashFilePath)
        {
            var hash = SHA256.Create();
            using var stream = new FileStream(catalogFilePath, FileMode.Open, FileAccess.Read);
            byte[] hashByte = hash.ComputeHash(stream);
            var hash128Str = BitConverter.ToString(hashByte).Replace("-", "").ToLower();
            var hashByteLength = stream.Length;
            var hashStr = $"{hash128Str},{hashByteLength}";
            File.WriteAllText(catalogHashFilePath, hashStr, EncodingExt.UTF8WithoutBom);
        }

        private static byte[] SaveCatalog(CatalogConfig catalog, string catalogFilePath)
        {
            var catalogContent = JsonUtility.ToJson(catalog);
            var bytes = Encoding.UTF8.GetBytes(catalogContent);
            using (var ms = new FileStream(catalogFilePath, FileMode.Create))
            using (ZipArchive arch = new ZipArchive(ms, ZipArchiveMode.Create))
            {
                var entry = arch.CreateEntry("catalog.json", CompressionLevel.Optimal);
                var stream = entry.Open();
                stream.Write(bytes);
            }

            return bytes;
        }

        public static BuildAssetBundlesResult BuildAssetBundlesWithPathInfo()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("cannot build on playmode");
                return null;
            }

            ScriptCompilationSettings scriptCompilationSettings = new()
            {
                target = EditorUserBuildSettings.activeBuildTarget,
                group = EditorUserBuildSettings.selectedBuildTargetGroup,
                options = ScriptCompilationOptions.DevelopmentBuild,
            };

            var configGuids =
                AssetDatabase.FindAssets("t:AssetConsumerConfig", new[] { "Assets", "Packages/windy.miiasset" });
            var tunnel = "";
            var catalogType = "";
            var buildGuids = false;
            var buildCodeHint = true;
            if (configGuids.Length > 0)
            {
                var configGuid = configGuids[0];
                var config =
                    AssetDatabase.LoadAssetAtPath<AssetConsumerConfig>(AssetDatabase.GUIDToAssetPath(configGuid));
                tunnel = config.updateTunnel;
                catalogType = config.catalogType;
                buildGuids = config.buildGuids;
                buildCodeHint = config.buildCodeHint;
            }
            else
            {
                Debug.LogError("no consumer config found");
                return null;
            }

            var pathInfo = AAPathConfigLoader.LoadDefaultConfigs();
            var buildAssetBundlesResult = BuildAssetBundles(pathInfo, scriptCompilationSettings, new()
            {
                UpdateTunnel = tunnel,
                CatalogType = catalogType,
                BuildGuids = buildGuids,
                BuildHintCode = buildCodeHint,
            });
            return buildAssetBundlesResult;
        }
    }
}