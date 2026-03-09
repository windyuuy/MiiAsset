using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using MonoExtLib.LinqExt;
using UnityEditor;
using UnityEngine;
using MiiAsset.AssetWeakRefer.Runtime;

namespace TrackableResourceManager.Runtime
{
	[CreateAssetMenu(fileName = "资源清单.asset", menuName = "Mii/资源清单")]
	public class ResourceManifestConfig : ScriptableObject
	{
		[Serializable]
		public class ResourceItem
		{
			[Header("分组范围内唯一Key")]
			// 唯一性范围？？
			public string key = "default";

			// [Header("引用资源")] 
			public AssetReference resRefer;

			public ResourceItem()
			{
			}

			public ResourceItem(string guid, string key)
			{
				this.key = key;
				this.resRefer = new AssetReference(guid);
			}

			public ResourceItem(string guid)
			{
				this.resRefer = new AssetReference(guid);
				this.key = "";
			}

			/// <summary>
			/// guid
			/// </summary>
			public string ResUri
			{
				get => resRefer.AssetGUID;
				set => resRefer = new AssetReference(value);
			}

			public override string ToString()
			{
				return $"{{key: {key}, resUri: {ResUri}}}";
			}

			public string DefaultKey
			{
				get
				{
					var path = AssetDatabase.GUIDToAssetPath(this.ResUri);
					var key1 = FormatKey(Path.GetFileName(path));
					return key1;
				}
			}

			public override bool Equals(object obj)
			{
				if (obj is ResourceItem item)
				{
					return item.key == this.key && item.resRefer == this.resRefer;
				}

				return false;
			}

			public override int GetHashCode()
			{
				return key.GetHashCode() ^ resRefer.GetHashCode();
			}
		}

		[Serializable]
		public class EnumResourceItem : ResourceItem
		{
		}

		[Serializable]
		public class ResourceScanRule
		{
			[Header("标题")] [Tooltip("标题，任意文本，自己看看")]
			public string title;

			[Header("路径匹配规则(正则表达式)")] public string pathRegex;

			public Regex GetPathRegex(string assetPath)
			{
				var p = ReplaceWithAssetPath(pathRegex, assetPath);
				var pr = new Regex(p);
				return pr;
			}

			protected static readonly Regex PathRegexPrefixRegex = new Regex(@"^(\.[\./]*/)(.*)$");

			private string ReplaceWithAssetPath(string pathRegex0, string assetPath)
			{
				var p = pathRegex0;
				var m = PathRegexPrefixRegex.Match(pathRegex0);
				if (m.Success)
				{
					p = Path.Combine(assetPath, m.Groups[1].Value);
					p = Path.GetFullPath(p);
					p = Path.GetRelativePath(Path.GetFullPath("./"), p);
					p = p.Replace('\\', '/');
					if (!p.EndsWith('/'))
					{
						p += '/';
					}

					p = p + m.Groups[2].Value;
				}

				return p;
			}

			[Header("遍历根节点")] [Tooltip("限制遍历根节点, 提高遍历速度")]
			public string scanRoot;

			public string GetScanRoot(string assetPath)
			{
				var p = ReplaceWithAssetPath(scanRoot, assetPath);
				return p;
			}

			public bool IsInvalid()
			{
				return string.IsNullOrWhiteSpace(pathRegex) || string.IsNullOrWhiteSpace(scanRoot);
			}
		}

		[Serializable]
		public class ResourceItemSet
		{
			public string setName = "default";

		#if UNITY_EDITOR
			/// <summary>
			/// 零散资源
			/// </summary>
			public EnumResourceItem[] enumItems;

			public ResourceScanRule[] scanRules;
		#endif
			/// <summary>
			/// 汇总清单
			/// </summary>
			public ResourceItem[] outputItems;

			public string joinKey;

			public bool IsValidResource(string guid)
			{
				var uri = AssetDatabase.GUIDToAssetPath(guid);
				var asset1 = AssetDatabase.LoadAssetAtPath<AssetGroupConfig>(uri);
				if (asset1 != null)
				{
					return false;
				}

				var asset2 = AssetDatabase.LoadAssetAtPath<ResourceManifestConfig>(uri);
				if (asset2 != null)
				{
					return false;
				}

				var asset3 = AssetDatabase.LoadAssetAtPath<MonoScript>(uri);
				if (asset3 != null)
				{
					return false;
				}

				return true;
			}

			public IEnumerable<ResourceItem> SearchMatchedResources(string assetCurPath)
			{
				// guid - key
				var itemsDict = new Dictionary<string, string>();
				foreach (var resourceItem in enumItems)
				{
					itemsDict.Add(resourceItem.ResUri, resourceItem.key);
				}

				foreach (var config in scanRules)
				{
					if (config.IsInvalid())
					{
						continue;
					}

					var assetFolder = Path.GetDirectoryName(assetCurPath);
					var pathRegex = config.GetPathRegex(assetFolder);
					var scanRoot = config.GetScanRoot(assetFolder);
					var guids = AssetDatabase.FindAssets("t:Object", new[] { scanRoot });
					foreach (var guid in guids)
					{
						var assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (Directory.Exists(assetPath))
						{
							continue;
						}

						var m = pathRegex.Match(assetPath);
						if (!m.Success)
						{
							m = pathRegex.Match(assetPath + "/");
						}

						if (m.Success)
						{
							if (IsValidResource(guid))
							{
								itemsDict.TryAdd(guid, null);
							}
						}
					}
				}

				var oldItems = outputItems.Select(item =>
				{
					if (string.IsNullOrWhiteSpace(item.key) && itemsDict.TryGetValue(item.ResUri, out var key))
					{
						if (!string.IsNullOrWhiteSpace(key))
						{
							return new ResourceItem
							{
								key = key,
								resRefer = item.resRefer,
							};
						}
					}

					return item;
				}).ToArray();

				foreach (var resourceItem in outputItems)
				{
					itemsDict.Remove(resourceItem.ResUri);
				}

				var newItems = itemsDict.Select(pair => new ResourceItem()
				{
					key = pair.Value,
					ResUri = pair.Key,
				});

				return oldItems.Concat(newItems).ToArray();
			}
		}

		[Header("作为代码清单")] [Tooltip("选中此项，则资源key优先级降低，能够被未选中此项的覆盖")]
		public bool isForCoder = true;

		public int SortOrder => isForCoder ? 0 : 1;

		/// <summary>
		/// belonged group
		/// </summary>
		public AssetGroupConfig group;

		public ResourceItemSet[] itemSets;

		public bool CheckValid()
		{
			if (group == null)
			{
				Debug.LogError("请填写分组字段");
				return false;
			}

			if (!group.CheckValid())
			{
				return false;
			}

			return true;
		}

		public ResourceItem GetItemByGuid(string guid)
		{
			var items = CollectResourceItems();
			var item = items.SingleOrDefault(item => item.ResUri == guid && !string.IsNullOrWhiteSpace(item.key));
			return item;
		}

		public ResourceItem GetItemByGuid(string guid, string key)
		{
			var items = CollectResourceItems();
			var item = items.FirstOrDefault(item =>
				item.ResUri == guid && item.key != key && !string.IsNullOrWhiteSpace(item.key));
			return item;
		}

		public IEnumerable<ResourceItem> SearchMatchedResources()
		{
			string assetCurPath = GetAssetPath();
			IEnumerable<ResourceItem> items = this.itemSets.MergeGroup(itemSet =>
				itemSet.SearchMatchedResources(assetCurPath)).Distinct();
			return items;
		}

		public IEnumerable<ResourceItem> CollectResourceItems()
		{
			IEnumerable<ResourceItem> items = this.itemSets.MergeGroup(itemSet =>
				itemSet.outputItems.Where(item => !string.IsNullOrWhiteSpace(item.key))).Distinct();
			return items;
		}

		public IEnumerable<(ResourceItemSet itemSet, ResourceItem item)> CollectResourceItemsWithSetName()
		{
			IEnumerable<(ResourceItemSet itemSet, ResourceItem item)> items = this.itemSets.MergeGroup(itemSet =>
				itemSet.outputItems.Where(item => !string.IsNullOrWhiteSpace(item.key))
					.Select(item => (itemSet, item))).Distinct();
			return items;
		}

		private string GetAssetPath()
		{
			return AssetDatabase.GetAssetPath(this);
		}

		protected static readonly Regex RegexCamel = new Regex(@"([a-zA-Z0-9\u4E00-\u9FA5]+)");

		public static string FormatMemberName(string memberName)
		{
			if (string.IsNullOrWhiteSpace(memberName))
			{
				return "";
			}

			memberName = memberName.Replace("/", "_");

			var memberName1 = string.Join("", RegexCamel.Matches(memberName).Select(m =>
			{
				var key = m.Groups[1].Value;
				key = char.ToUpper(key[0]) + key.Substring(1);
				return key;
			}));

			if (new Regex(@"^\d").IsMatch(memberName1))
			{
				memberName1 = "_" + memberName1;
			}

			return memberName1;
		}

		public static string FormatFileName(string groupName)
		{
			return groupName.Replace("/", "_");
		}

		public void GenerateCode()
		{
			if (!CheckValid())
			{
				return;
			}

			// generate for sets without name
			// var codeFilePath = Path.ChangeExtension(AssetDatabase.GetAssetPath(this), "cs");
			var groupName = group.groupName;
			var saveDir = $"Assets/Bundles/GameConfigs/R/";
			if (!Directory.Exists(saveDir))
			{
				Directory.CreateDirectory(saveDir);
			}

			var codeFilePath = $"{saveDir}{FormatFileName(groupName)}Group.R.cs";
			var ns = string.IsNullOrWhiteSpace(group.ns) ? "SharedResIds" : group.ns;
			var groupJoinKey = string.IsNullOrEmpty(group.joinKey) ? ":" : group.joinKey;
			var groupNameMembered = FormatMemberName(groupName);

			var setClassesCode = string.Join('\n', itemSets
				.Where(itemSet => itemSet.outputItems.Length > 0)
				.GroupBy(itemSet =>
				{
					if (string.IsNullOrEmpty(itemSet.setName))
					{
						return "default";
					}

					return itemSet.setName;
				})
				.Where(setGroup => setGroup.Any())
				.Select(setGroup =>
				{
					var setName = setGroup.Key;
					var setNameFormat = FormatMemberName(setName);
					if (string.IsNullOrEmpty(setNameFormat))
					{
						setNameFormat = "Default";
					}

					var fieldsCode = string.Join('\n', setGroup
						.Select(itemSet =>
						{
							var joinKey = string.IsNullOrEmpty(itemSet.joinKey) ? groupJoinKey : itemSet.joinKey;
							var fieldsCode = string.Join('\n', itemSet.outputItems
								.Where(item => !string.IsNullOrWhiteSpace(item.key))
								.Select(item =>
								{
									var fieldCode = @"		public static readonly ResourceKey " +
									                $"{ToFieldName(item.key, groupNameMembered, setName, setNameFormat)}" +
									                @" = ResourceKey.ParseFromLiteral(" +
									                $"\"{ToAddress(groupName, setName, item.key, groupJoinKey, joinKey)}\"" +
									                @");
";
									return fieldCode;
								}));
							return fieldsCode;
						}));

					var setClassCode = @"		#region " + setName + @"

" + fieldsCode + @"
		#endregion
";
					return setClassCode;
				}));
			var groupClassCode = @"	public static partial class R
	{
	#region " + groupName + @"
" + setClassesCode + @"	#endregion
	}";
			var fileCode = @"
// ReSharper disable InconsistentNaming

using TrackableResourceManager.Runtime;

namespace " + ns + @"
{
" + groupClassCode + @"
}
";

			File.WriteAllText(codeFilePath, fileCode, Encoding.UTF8);
			AssetDatabase.Refresh();
			// generate for named sets
		}

		public static string GetJoinKey(string joinKey)
		{
			if (string.IsNullOrEmpty(joinKey))
			{
				return ":";
			}

			return joinKey;
		}

		public static string ToAddress(string groupName, string setName, string itemKey, string groupJoinKey,
			string joinKey)
		{
			if (setName == "default")
			{
				return $"@{FormatKey(groupName)}{GetJoinKey(groupJoinKey)}{FormatKey(itemKey)}";
			}
			else
			{
				return
					$"@{FormatKey(groupName)}{GetJoinKey(groupJoinKey)}{FormatKey(setName)}{GetJoinKey(joinKey)}{FormatKey(itemKey)}";
			}
		}

		public static string ToFieldName(string itemKey, string groupNameMembered, string setName, string setNameFormat)
		{
			if (setName == "default")
			{
				return $"{FormatMemberName(itemKey)}_{groupNameMembered}";
			}
			else
			{
				return $"{FormatMemberName(itemKey)}_{groupNameMembered}_{setNameFormat}";
			}
		}

		public static string FormatKey(string itemKey)
		{
			// return string.Join("",itemKey.Replace(' ', '_').Split("_").Select(key => char.ToUpper(key[0]) + key.Substring(1)));
			return itemKey.Replace(" ", "");
		}

		public bool ResolveNamespace(string guid, string key, int order, out string conflictKey)
		{
			conflictKey = null;
			return true;

			// TODO: 修复BUG
			var assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
			group.LoadAllManifestNamespace();
			if (group.ResolveNamespace(guid, assetGuid, order, out conflictKey))
			{
				var item = this.GetItemByGuid(guid, key);
				if (item == null)
				{
					return true;
				}
				else
				{
					conflictKey = key;
				}
			}

			return false;
		}

		public bool GetBelongedSet(ResourceItem item, out ResourceItemSet itemSet, out ResourceItem itemOut)
		{
			foreach (var resourceItemSet in itemSets)
			{
				foreach (var item0 in resourceItemSet.outputItems)
				{
					if (item0.ResUri == item.ResUri)
					{
						itemSet = resourceItemSet;
						itemOut = item0;
						return true;
					}
				}
			}

			itemSet = null;
			itemOut = null;

			return false;
		}


		public bool GetEntryUKey(ResourceItem item, out string uKey)
		{
			if (GetBelongedSet(item, out var itemSet, out var itemOut))
			{
				if (!string.IsNullOrWhiteSpace(itemOut.key))
				{
					uKey = ToAddress(this.group.groupName, itemSet.setName, itemOut.key, this.group.joinKey,
						itemSet.joinKey);
					return true;
				}
			}

			uKey = "";
			return false;
		}
	}
}