using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MiiAsset.Runtime
{
	public class CatalogInfo
	{
		public Dictionary<string, AssetBundleInfo> NameBundleMap = new();
		public Dictionary<string, string> AddressBundleMap = new();
		public Dictionary<string, string> GuidAddressMap = new();

		public Dictionary<string, HashSet<string>> BundleFlatRelationMap = new();
		public Dictionary<string, HashSet<string>> TagFlatBundlesMap = new();

		/// <summary>
		/// 合并后的bundle加载清单
		/// </summary>
		public Dictionary<string, IResourceLoadSource> BundleLoadSourceMap = new();

		public Dictionary<string, IResourceLoadSource> InternalBundles = new();

		// record bundles cleaned before update
		public List<string> BundlesToClean = new();

		public void GetTagsDependBundles(IEnumerable<string> tags, HashSet<string> depBundles)
		{
			foreach (var tag in tags)
			{
				if (TagFlatBundlesMap.TryGetValue(tag, out var deps1))
				{
					foreach (var se in deps1)
					{
						depBundles.Add(se);
					}
				}
			}
		}

		public HashSet<string> GetTagDependBundles(string tag)
		{
			if (TagFlatBundlesMap.TryGetValue(tag, out var deps1))
			{
				return deps1;
			}

			return null;
		}

		public void GetAssetDependBundles(string address, out HashSet<string> deps)
		{
			if (AddressBundleMap.TryGetValue(address, out var bundle))
			{
				if (BundleFlatRelationMap.TryGetValue(bundle, out var deps1))
				{
					deps = deps1;
					return;
				}
			}
			else
			{
				Debug.LogError($"asset not exist in1 any bundle: {address}");
			}

			deps = null;
		}

		public void GetAssetsDependBundles(IEnumerable<string> addresses, HashSet<string> deps)
		{
			foreach (var address in addresses)
			{
				if (AddressBundleMap.TryGetValue(address, out var bundle))
				{
					if (BundleFlatRelationMap.TryGetValue(bundle, out var deps1))
					{
						foreach (var se in deps1)
						{
							deps.Add(se);
						}
					}
				}
			}
		}

		void ParseDeps(AssetBundleInfo bundleInfo, HashSet<string> deps)
		{
			var bundleInfoDeps = bundleInfo.deps;
			bundleInfoDeps.Add(bundleInfo.fileName);
			foreach (var dep in bundleInfoDeps)
			{
				if (!deps.Contains(dep))
				{
					deps.Add(dep);
					var depAssetBundleInfo = this.NameBundleMap[dep];
					ParseDeps(depAssetBundleInfo, deps);
				}
			}
		}

		public void LoadCatalogInfo(CatalogConfig catalog)
		{
			foreach (var bundleInfo in catalog.bundleInfos)
			{
				this.NameBundleMap.Add(bundleInfo.fileName, bundleInfo);
				foreach (var entry in bundleInfo.entries)
				{
					this.AddressBundleMap.Add(entry, bundleInfo.fileName);
				}

				for (var index = 0; index < bundleInfo.guids.Length; index++)
				{
					var guid = bundleInfo.guids[index];
					this.GuidAddressMap.Add(guid, bundleInfo.entries[index]);
				}
			}

			var flatRelationMap = this.BundleFlatRelationMap;
			foreach (var bundleInfo in catalog.bundleInfos)
			{
				// this.BundleFlatRelationMap
				if (!flatRelationMap.TryGetValue(bundleInfo.fileName, out var deps))
				{
					deps = new HashSet<string>();
					deps.Add(bundleInfo.fileName);
					flatRelationMap.Add(bundleInfo.fileName, deps);
				}

				if (bundleInfo.deps.Length > 0)
				{
					ParseDeps(bundleInfo, deps);
				}
			}

			var tagFlatBundlesMap = this.TagFlatBundlesMap;
			foreach (var bundleInfo in catalog.bundleInfos)
			{
				// this.TagFlatBundlesMap

				foreach (var tag in bundleInfo.tags.Append("all"))
				{
					if (!tagFlatBundlesMap.TryGetValue(tag, out var deps))
					{
						deps = new();
						tagFlatBundlesMap.Add(tag, deps);
					}

					deps.Add(bundleInfo.fileName);

					var bundleDeps = flatRelationMap[bundleInfo.fileName];
					if (bundleDeps.Count > 0)
					{
						foreach (var bundleDep in bundleDeps)
						{
							deps.Add(bundleDep);
						}
					}
				}
			}
		}

		public AssetBundleInfo GetAssetBundleInfo(string bundleName)
		{
			if (NameBundleMap.TryGetValue(bundleName, out var bundleInfo))
			{
				return bundleInfo;
			}
			else
			{
				Debug.LogError($"invalid bundle not exist: {bundleName}");
				return null;
			}
		}

		public long GetFileSize(string bundleName)
		{
			if (NameBundleMap.TryGetValue(bundleName, out var bundleInfo))
			{
				return bundleInfo.size;
			}
			else
			{
				Debug.LogError($"invalid bundle not exist: {bundleName}");
				return -1;
			}
		}

		public bool IsInternalBundle(string bundleName)
		{
			return this.InternalBundles.ContainsKey(bundleName);
		}

		public string GetAddressFromGuid(string guid)
		{
			if (this.GuidAddressMap.TryGetValue(guid, out var address))
			{
				return address;
			}

			return null;
		}
	}
}