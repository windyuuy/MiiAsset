using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiiAsset.Runtime;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace MiiAsset.Editor.Build
{
	public static class AASingleFileItemExt
	{
		public static string GetGuid(this AASingleFileItem item)
		{
			return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(item.asset));
		}

		public static string GetLoadPath(this AASingleFileItem item)
		{
			return AssetDatabase.GetAssetPath(item.asset);
		}
	}

	public class DepCollector
	{
		// collect tag bundles
		public readonly Dictionary<string, TagBundle> TagBundleMap = new();
		public readonly Dictionary<string, int> TagOrderMap = new();
		public readonly Dictionary<string, TagBundle> GuidBundleMap = new();
		public readonly Dictionary<string, TagBundle> TagsNameBundleMap = new();

		/// <summary>
		/// 零散文件
		/// </summary>
		public readonly Dictionary<string, ExtraAddressInfo> ExtraAddressInfoMap = new();

		public readonly Dictionary<string, AASingleFileItem> SingleFileMap = new();
		public readonly Dictionary<string, AASingleFileItem> InvalidSingleFileAddressMap = new();

		protected int TagOrderAcc = 0;

		public string ToTagsKey(string[] tags)
		{
			var tagKey = string.Join("_", tags.Select(tag =>
				{
					if (!TagOrderMap.TryGetValue(tag, out var order))
					{
						order = TagOrderAcc++;
						TagOrderMap.Add(tag, order);
					}

					return (tag, order);
				}).OrderBy(item => item.order)
				.Select(item => item.order));
			return tagKey;
		}

		public readonly Dictionary<string, string> AddressExistMap = new();

		protected AAPathInfo PathInfo;
		protected HashSet<string> FilterMap0 = new();

		protected void AddExtraAddressInfo(string address, string atlasAddress)
		{
			var extraAddressInfo = new ExtraAddressInfo
			{
				loadType = AddressLoadType.AtlasSprite,
				address = address,
				guid = AssetDatabase.AssetPathToGUID(address),
				sourceAddress = atlasAddress,
				key = Path.GetFileNameWithoutExtension(address),
			};
			if (!ExtraAddressInfoMap.TryAdd(address, extraAddressInfo))
			{
				if (ExtraAddressInfoMap.TryGetValue(address, out var extraAtlasInfo))
				{
					Debug.LogException(
						new Exception(
							$"重复的资源: {address}, 可能在不同的 .spriteatlas 文件中引用了同一个纹理资源: {extraAtlasInfo.address}, {atlasAddress}"));
				}
				else
				{
					Debug.LogException(
						new Exception(
							$"重复的资源: {address}, 可能在不同的 .spriteatlas 文件中引用了同一个纹理资源: 未发现现有spriteatlas资源, {atlasAddress}"));
				}
			}
		}

		public void CollectValidAssets(AAPathInfo pathInfo)
		{
			// CollectDeps(pathInfo);
			//
			// foreach (var item in GuidBundleMap)
			// {
			// 	var path = AssetDatabase.GUIDToAssetPath(item.Key);
			// 	if (!AddressBundleMap.TryAdd(path, item.Value))
			// 	{
			// 		Debug.LogError($"GUID asset path {path} has already been added.");
			// 	}
			// }
			//
			Reset();
			// collect sprites in spriteatlas
			FilterMap0.Clear();
			CollectSpriteAtlas(FilterMap0, AddExtraAddressInfo);
			var singleFileItems = pathInfo.SingleFileItems;
			foreach (var (guid, value) in singleFileItems)
			{
				if (!SingleFileMap.TryAdd(value.key, value))
				{
					var exception = new Exception($"重复的零散资源: {value.key}");
					if (Application.isPlaying)
					{
						throw exception;
					}
					else
					{
						Debug.LogException(exception);
					}
				}

				var address = value.GetLoadPath();
				InvalidSingleFileAddressMap.Add(address, value);
			}

			PathInfo = pathInfo;
		}

		public bool TryGetLoadUri(string address, out string loadUri)
		{
			if (AddressExistMap.TryGetValue(address, out loadUri))
			{
				return false == string.IsNullOrEmpty(loadUri);
			}

			var isValid = TryGetLoadUriInternal(address, out loadUri);
			if (isValid)
			{
				AddressExistMap.Add(address, loadUri);
			}
			else
			{
				AddressExistMap.Add(address, null);
			}

			return isValid;
		}

		protected bool TryGetLoadUriInternal(string address, out string loadUri)
		{
			if (FilterMap0.Contains(address))
			{
				loadUri = null;
				return false;
			}

			var pathInfo = PathInfo;
			if (!AAPathInfo.IsValidAsset(pathInfo, address))
			{
				loadUri = null;
				return false;
			}

			if (SingleFileMap.TryGetValue(address, out var singleFileItem))
			{
				loadUri = singleFileItem.GetLoadPath();
				return true;
			}

			if (InvalidSingleFileAddressMap.TryGetValue(address, out var _))
			{
				loadUri = null;
				return false;
			}

			var groupInfo = AAPathInfo.ParseGroupName(pathInfo, address, AssetDatabase.AssetPathToGUID(address));
			if (groupInfo == null)
			{
				loadUri = null;
				return false;
			}

			loadUri = address;
			return true;
		}

		void CollectSpriteAtlas(HashSet<string> filterMap0, Action<string, string> handle)
		{
			var guids = AssetDatabase.FindAssets("t:spriteatlas", new string[] { "Assets" });
			foreach (var guid in guids)
			{
				var atlasAddress = AssetDatabase.GUIDToAssetPath(guid);
				var spriteatlas =
					AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasAddress);
				var objs = spriteatlas.GetPackables();
				foreach (var o in objs)
				{
					var spriteAddress = AssetDatabase.GetAssetPath(o);
					filterMap0.Add(spriteAddress);

					handle(spriteAddress, atlasAddress);
				}
			}
		}

		public void CollectDeps(AAPathInfo pathInfo)
		{
			Reset();

			// collect sprites in spriteatlas
			var singleFileItems = pathInfo.SingleFileItems;
			var filterMap = new HashSet<string>();
			CollectSpriteAtlas(filterMap, AddExtraAddressInfo);

			foreach (var scanInfo in pathInfo.GetScanRootInfos(true))
			{
				var guids = AssetDatabase.FindAssets("", new[] { scanInfo.ScanRoot });
				var validAssetGroupNameInfo = guids
					.Select(guid => (guid, assetPath: AssetDatabase.GUIDToAssetPath(guid)))
					.Where(item =>
						!filterMap.Contains(item.assetPath) && AAPathInfo.IsValidAsset(pathInfo, item.assetPath))
					.Select(item =>
					{
						if (string.IsNullOrEmpty(scanInfo.ScanRoot) || item.assetPath.StartsWith(scanInfo.ScanRoot))
						{
							var groupNameInfo = AAPathInfo.ParseGroupName(scanInfo.Item, item.assetPath, item.guid,
								pathInfo.IsEncryptAll);
							return groupNameInfo;
						}

						return null;
					})
					.Where(item => item != null)
					.Where(item => !IsExcludeItem(pathInfo, item, scanInfo));
				foreach (var assetGroupNameInfo in validAssetGroupNameInfo)
				{
					if (singleFileItems.ContainsKey(assetGroupNameInfo.Guid))
					{
						continue;
					}

					if (GuidBundleMap.ContainsKey(assetGroupNameInfo.Guid))
					{
						continue;
					}

					assetGroupNameInfo.Tags = assetGroupNameInfo.Tags
						.Select(tag => tag.ToLower())
						.ToArray();
					if (assetGroupNameInfo.AssetPath.EndsWith(".unity"))
					{
						assetGroupNameInfo.Tags = assetGroupNameInfo.Tags.Append("scene").ToArray();
					}

					var tagsKey = ToTagsKey(assetGroupNameInfo.Tags);
					if (!TagBundleMap.TryGetValue(tagsKey, out var tagBundle))
					{
						tagBundle = new TagBundle()
						{
							Tags = assetGroupNameInfo.Tags,
							TagsAdditional = Array.Empty<string>(),
							TagsUKey = tagsKey,
							// SingleFileItems = pathInfo.SingleFileItems,
							IsEncrypt = pathInfo.IsEncryptAll || assetGroupNameInfo.IsEncrypt,
							IsKeepInMemory = pathInfo.IsKeepInMemory,
						};
						TagBundleMap.Add(tagsKey, tagBundle);
					}

					if (GuidBundleMap.TryAdd(assetGroupNameInfo.Guid, tagBundle))
					{
						tagBundle.IsOffline = tagBundle.IsOffline || !assetGroupNameInfo.IsRemote;
						tagBundle.IsEncrypt =
							pathInfo.IsEncryptAll || tagBundle.IsEncrypt || assetGroupNameInfo.IsEncrypt;
						tagBundle.IsKeepInMemory = pathInfo.IsKeepInMemory;
						tagBundle.Guids.Add(assetGroupNameInfo.Guid);

						// Debug.LogError($"conflict item: {groupNameInfo.AssetPath}");
					}
					//
					// guidBundleMap[groupNameInfo.Guid] = tagBundle;
				}
			}


			foreach (var group in singleFileItems.Values.GroupBy(item => item.GetGroupName()))
			{
				var groupName = group.Key;
				var tags = new string[]
				{
					groupName,
				};
				var tagsKey = ToTagsKey(tags);
				var items = group
					.Where(item => { return false == GuidBundleMap.ContainsKey(item.GetGuid()); })
					.Where(item => !IsExcludeItem(pathInfo, item.GetLoadPath()))
					.ToArray();
				if (items.Length > 0)
				{
					if (!TagBundleMap.TryGetValue(tagsKey, out var tagBundle))
					{
						tagBundle = new TagBundle()
						{
							Tags = tags,
							TagsAdditional = Array.Empty<string>(),
							TagsUKey = tagsKey,
							// SingleFileItems = pathInfo.SingleFileItems,
							IsEncrypt = pathInfo.IsEncryptAll,
							IsKeepInMemory = pathInfo.IsKeepInMemory,
						};
						TagBundleMap.Add(tagsKey, tagBundle);
					}

					foreach (var item in items)
					{
						tagBundle.IsEncrypt = tagBundle.IsEncrypt || item.isEncrypt;
						tagBundle.IsOffline = tagBundle.IsOffline || item.isOffline;
						tagBundle.IsKeepInMemory = tagBundle.IsKeepInMemory;
						tagBundle.Guids.Add(item.GetGuid());

						tagBundle.AddressMap.Add(item.GetGuid(), item.key);
						GuidBundleMap.Add(item.GetGuid(), tagBundle);
					}
				}
			}

			return;
		}

		private bool IsExcludeItem(AAPathInfo pathInfo, AssetGroupNameInfo item, GroupScanInfo scanInfo)
		{
			var assetPath = item.AssetPath;
			var excludePaths = pathInfo.ExcludePaths;
			foreach (var excludeItem in excludePaths)
			{
				// 子目录或者更长的路径优先包含或排除, 路径相同则优先排除
				if (excludeItem.scanRoot.StartsWith(scanInfo.ScanRoot))
				{
					// 需要满足 scanRoot 前缀
					if (assetPath.StartsWith(excludeItem.scanRoot))
					{
						// 匹配表达式, 则需排除
						if (excludeItem.pathRegex.IsMatch(assetPath))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		private bool IsExcludeItem(AAPathInfo pathInfo, string assetPath)
		{
			var excludePaths = pathInfo.ExcludePaths;
			foreach (var excludeItem in excludePaths)
			{
				// 需要满足 scanRoot 前缀
				if (assetPath.StartsWith(excludeItem.scanRoot))
				{
					// 匹配表达式, 则需排除
					if (excludeItem.pathRegex.IsMatch(assetPath))
					{
						return true;
					}
				}
			}

			return false;
		}

		private void Reset()
		{
			TagBundleMap.Clear();
			TagOrderMap.Clear();
			GuidBundleMap.Clear();
			TagsNameBundleMap.Clear();
			TagOrderAcc = 0;
			ExtraAddressInfoMap.Clear();
		}
	}
}