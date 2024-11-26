using System;
using System.Collections.Generic;
using System.Linq;
using MiiAsset.Runtime;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace MiiAsset.Editor.Build
{
	public class DepCollector
	{
		// collect tag bundles
		public readonly Dictionary<string, TagBundle> TagBundleMap = new();
		public readonly Dictionary<string, int> TagOrderMap = new();
		public readonly Dictionary<string, TagBundle> GuidBundleMap = new();
		public readonly Dictionary<string, TagBundle> TagsNameBundleMap = new();

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

		public readonly Dictionary<string, bool> AddressExistMap = new();

		protected AAPathInfo PathInfo;
		protected HashSet<string> FilterMap0 = new();

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
			CollectSpriteAtlas(FilterMap0);

			PathInfo = pathInfo;
		}

		public bool IsValidAsset(string address)
		{
			if (AddressExistMap.TryGetValue(address, out var ret))
			{
				return ret;
			}

			var isValid = IsValidAssetInternal(address);
			AddressExistMap.Add(address, isValid);
			return isValid;
		}

		protected bool IsValidAssetInternal(string address)
		{
			if (FilterMap0.Contains(address))
			{
				return false;
			}

			var pathInfo = PathInfo;
			if (!AAPathInfo.IsValidAsset(pathInfo, address))
			{
				return false;
			}

			var groupInfo = AAPathInfo.ParseGroupName(pathInfo, address, AssetDatabase.AssetPathToGUID(address));
			if (groupInfo == null)
			{
				return false;
			}

			return true;
		}

		void CollectSpriteAtlas(HashSet<string> filterMap0)
		{
			var guids = AssetDatabase.FindAssets("t:spriteatlas", new string[] { "Assets" });
			foreach (var guid in guids)
			{
				var spriteatlas =
					AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid));
				var objs = spriteatlas.GetPackables();
				foreach (var o in objs)
				{
					filterMap0.Add(AssetDatabase.GetAssetPath(o));
				}
			}
		}

		public void CollectDeps(AAPathInfo pathInfo)
		{
			Reset();

			// collect sprites in spriteatlas
			var filterMap = new HashSet<string>();
			CollectSpriteAtlas(filterMap);

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
							var groupNameInfo = AAPathInfo.ParseGroupName(scanInfo.Item, item.assetPath, item.guid);
							return groupNameInfo;
						}

						return null;
					})
					.Where(item => item != null);
				foreach (var groupNameInfo in validGroupNameInfo)
				{
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
						};
						TagBundleMap.Add(tagsKey, tagBundle);
					}

					if (GuidBundleMap.TryAdd(groupNameInfo.Guid, tagBundle))
					{
						tagBundle.IsOffline |= !groupNameInfo.IsRemote;
						tagBundle.Guids.Add(groupNameInfo.Guid);

						// Debug.LogError($"conflict item: {groupNameInfo.AssetPath}");
					}
					//
					// guidBundleMap[groupNameInfo.Guid] = tagBundle;
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
		}
	}
}