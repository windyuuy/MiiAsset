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
			ExtraAddressInfoMap.Add(address, extraAddressInfo);
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
				SingleFileMap.Add(value.key, value);
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
				var validGroupNameInfo = guids
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
					.Where(item => item != null);
				foreach (var groupNameInfo in validGroupNameInfo)
				{
					if (singleFileItems.ContainsKey(groupNameInfo.Guid))
					{
						continue;
					}

					if (GuidBundleMap.ContainsKey(groupNameInfo.Guid))
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
					if (!TagBundleMap.TryGetValue(tagsKey, out var tagBundle))
					{
						tagBundle = new TagBundle()
						{
							Tags = groupNameInfo.Tags,
							TagsAdditional = Array.Empty<string>(),
							TagsUKey = tagsKey,
							// SingleFileItems = pathInfo.SingleFileItems,
							IsEncrypt = pathInfo.IsEncryptAll || groupNameInfo.IsEncrypt,
						};
						TagBundleMap.Add(tagsKey, tagBundle);
					}

					if (GuidBundleMap.TryAdd(groupNameInfo.Guid, tagBundle))
					{
						tagBundle.IsOffline = tagBundle.IsOffline || !groupNameInfo.IsRemote;
						tagBundle.IsEncrypt = pathInfo.IsEncryptAll || tagBundle.IsEncrypt || groupNameInfo.IsEncrypt;
						tagBundle.Guids.Add(groupNameInfo.Guid);

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
				var items = group.Where(item => { return false == GuidBundleMap.ContainsKey(item.GetGuid()); })
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
						};
						TagBundleMap.Add(tagsKey, tagBundle);
					}

					foreach (var item in items)
					{
						tagBundle.Guids.Add(item.GetGuid());
						tagBundle.AddressMap.Add(item.GetGuid(), item.key);
						tagBundle.IsEncrypt = tagBundle.IsEncrypt || item.isEncrypt;
						GuidBundleMap.Add(item.GetGuid(), tagBundle);
					}
				}
			}

			return;
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