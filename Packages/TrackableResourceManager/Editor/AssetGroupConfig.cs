using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiiAsset.Runtime;
using MonoExtLib.LinqExt;
using UnityEditor;
using UnityEngine;

namespace TrackableResourceManager.Runtime
{
	[CreateAssetMenu(fileName = "资源分组.asset", menuName = "UISys/资源分组")]
	public class AssetGroupConfig : ScriptableObject
	{
		[Header("命名空间")] public string ns = "SharedResIds";
		[Header("分组名前缀")] public string groupPrefix;

		[Header("分组名")] public string groupName;
		public string GetGroupName()
		{
			return $"{groupPrefix}{groupName}";
		}
		public string GetGroupFileName()
		{
			return groupName.Replace("/", "_").ToLower();
		}

		// split with ;
		[Header("附加标签")]
		[Tooltip("split with ;")]
		[SerializeField]
		protected string tags;

		public string[] Tags => tags.Split(";");

		/// <summary>
		/// 是否打进包内
		/// </summary>
		[Header("是否也打进包内")] public bool isOffline;

		/// <summary>
		/// 帮助缩减搜索范围，加快搜索速度
		/// </summary>
		[Header("资源清单搜索路径")]
		[Tooltip("帮助缩减搜索范围，加快搜索速度")]
		public string scanPath = "Assets";

		/// <summary>
		/// 重载连接符
		/// </summary>
		public string joinKey = "";

		public bool CheckValid()
		{
			if (string.IsNullOrEmpty(this.groupName))
			{
				Debug.LogError("请为分组填写名称");
				return false;
			}

			return true;
		}

		public readonly struct UKey
		{
			public readonly string Guid;
			public readonly int Order;

			public UKey(string guid, int order)
			{
				Guid = guid;
				Order = order;
			}

			public override int GetHashCode()
			{
				return Guid.GetHashCode() ^ Order;
			}

			public override bool Equals(object obj)
			{
				if (obj is UKey uKey)
				{
					return this.Guid == uKey.Guid && this.Order == uKey.Order;
				}

				return false;
			}
		}

		/// <summary>
		/// key-manifest asset guid
		/// </summary>
		protected Dictionary<UKey, string> KeyNamespace = new();

		protected bool IsLoaded = false;

		public IEnumerable<(ResourceManifestConfig.ResourceItemSet itemSet, ResourceManifestConfig.ResourceItem item)> LoadAllManifestItems()
		{
			var assets = FindResourceManifestConfigs();
			var items = assets.MergeGroup(resourceManifestConfig => resourceManifestConfig.CollectResourceItemsWithSetName());
			return items;
		}

		public bool LoadAllManifestNamespace()
		{
			if (IsLoaded)
			{
				return false;
			}

			IsLoaded = true;

			var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
			var assets = FindResourceManifestConfigs();
			foreach (var asset in assets)
			{
				foreach (var resourceItemSet in asset.itemSets)
				{
					foreach (var resourceItem in resourceItemSet.outputItems)
					{
						if (!KeyNamespace.TryAdd(new UKey(resourceItem.ResUri, asset.SortOrder), guid))
						{
							Debug.LogError($"conflict key: {resourceItem}");
						}
					}
				}
			}

			return true;
		}

		private IEnumerable<ResourceManifestConfig> FindResourceManifestConfigs()
		{
			var scanPath0 = scanPath;
			if (scanPath0.StartsWith("."))
			{
				var assetDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
				var scanPath1 = Path.GetRelativePath(Path.GetFullPath("./"), assetDir)
					.Replace("\\", "/");
				if (!scanPath1.EndsWith('/'))
				{
					scanPath1 += '/';
				}
				scanPath0 = scanPath1;
			}
			var assets = AssetDatabase
				.FindAssets("t:ResourceManifestConfig", new[] { scanPath0 })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<ResourceManifestConfig>)
				.Where(asset => asset.group == this);
			return assets;
		}

		public bool ResolveNamespace(string guid, string manifestGuid, int order, out string conflictKey)
		{
			var uKey = new UKey(guid, order);
			if (KeyNamespace.TryGetValue(uKey, out var assetGuid0))
			{
				if (manifestGuid != assetGuid0)
				{
					// namespace conflict
					var asset = AssetDatabase.LoadAssetAtPath<AssetGroupConfig>(
						AssetDatabase.GUIDToAssetPath(assetGuid0));
					ResourceManifestConfig.ResourceItem item = null;
					foreach (var findResourceManifestConfig in asset.FindResourceManifestConfigs())
					{
						item = findResourceManifestConfig.GetItemByGuid(guid);
						if (item != null)
						{
							break;
						}
					}
					Debug.Assert(item != null, "item!=null");
					conflictKey = item.key;
					return false;
				}

				conflictKey = null;
				return true;
			}

			conflictKey = null;
			return true;
		}

		public bool AddKey(string key, string guid, int order, string manifestGuid)
		{
			var uKey = new UKey(guid, order);
			if (KeyNamespace.TryGetValue(uKey, out var assetGuid0))
			{
				if (manifestGuid != assetGuid0)
				{
					// namespace conflict
					return false;
				}

				KeyNamespace[uKey] = manifestGuid;
			}
			else
			{
				KeyNamespace.Add(uKey, manifestGuid);
			}

			return true;
		}

		public bool RemoveKey(string guid, int order, string manifestGuid)
		{
			var uKey = new UKey(guid, order);
			if (KeyNamespace.TryGetValue(uKey, out var assetGuid0))
			{
				if (manifestGuid != assetGuid0)
				{
					// namespace conflict
					return false;
				}

				KeyNamespace.Remove(uKey);
			}

			return true;
		}

		public bool FindBelong(
			ResourceManifestConfig.ResourceItem item,
			out ResourceManifestConfig belongManifestConfig)
		{
			var configs = FindResourceManifestConfigs();
			foreach (var resourceManifest in configs)
			{
				if (resourceManifest.GetBelongedSet(item, out var itemSet, out var itemOut))
				{
					belongManifestConfig = resourceManifest;
					return true;
				}
			}

			belongManifestConfig = null;
			return false;
		}

		public static bool GetEntryUKey(ResourceManifestConfig.ResourceItem item, out string uKey)
		{
			var configs = FindGroupConfigs();

			foreach (var groupConfig in configs)
			{
				if (groupConfig.FindBelong(item, out var belongManifest))
				{
					return belongManifest.GetEntryUKey(item, out uKey);
				}
			}

			uKey = "";
			return false;
		}

		private static IEnumerable<AssetGroupConfig> FindGroupConfigs()
		{
			var configs = AssetDatabase.FindAssets("t:AssetGroupConfig")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<AssetGroupConfig>);
			return configs;
		}

		public string GetEntryUKey(ResourceManifestConfig.ResourceItemSet itemSet, ResourceManifestConfig.ResourceItem item)
		{
			var uKey = ResourceManifestConfig.ToAddress(this.groupName, itemSet.setName, item.key, this.joinKey, itemSet.joinKey);
			return uKey;
		}

		public void InjectToAAGroups()
		{
			var asset = this;

			// AAUriMapConfig
			var uriMapConfigPath = $"Assets/Bundles/GameConfigs/Editor/AAConfig/umcs/{asset.GetGroupFileName()}_aaumc.asset";
			var dir = Path.GetDirectoryName(uriMapConfigPath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			AAPathConfig pathConfig;
			if (false == File.Exists(uriMapConfigPath))
			{
				pathConfig = ScriptableObject.CreateInstance<AAPathConfig>();
				AssetDatabase.CreateAsset(pathConfig, uriMapConfigPath);
			}
			else
			{
				pathConfig = AssetDatabase.LoadAssetAtPath<AAPathConfig>(uriMapConfigPath);
			}

			var items = asset.LoadAllManifestItems();
			if (string.IsNullOrWhiteSpace(asset.groupName) == false && items.Any())
			{
				var groupName = asset.GetGroupName();
				pathConfig.singleFiles.Clear();

				if (items.Any())
				{
					foreach (var (itemSet, resourceItem) in items)
					{
						var key = this.GetEntryUKey(itemSet, resourceItem);
						var item = new AASingleFileItem()
						{
							group = groupName,
							key = key,
							asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(resourceItem.ResUri)),
						};
						pathConfig.singleFiles.Add(item);
						// var entry = aaSettings.CreateOrMoveEntry(resourceItem.ResUri, group, true);
						// entry.SetAddress(ResourceManifestConfig.ToAddress(groupName, setName, resourceItem.key));

						// entry.labels.Clear();
						// foreach (var assetTag in asset.Tags)
						// {
						// 	entry.SetLabel(assetTag, true, true);
						// }
					}
					EditorUtility.SetDirty(pathConfig);
					AssetDatabase.SaveAssetIfDirty(pathConfig);
				}
			}
		}

		public static void InjectAllToAAGroups()
		{
			var assets = FindGroupConfigs();
			foreach (var resourceManifestConfig in assets)
			{
				resourceManifestConfig.InjectToAAGroups();
			}
		}

		[MenuItem("Tools/Resources/InjectAllToAAGroups")]
		public static void InjectAllToAAGroupsTool()
		{
			InjectAllToAAGroups();
		}
		public static void AllToAAGroups()
		{
			var assets = FindGroupConfigs();
			foreach (var resourceManifestConfig in assets)
			{
				resourceManifestConfig.InjectToAAGroups();
			}
		}
	}
}