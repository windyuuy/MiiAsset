using System.Collections.Generic;
using System.Linq;
using MiiAsset.Runtime.Adapter;
using UnityEngine;

namespace MiiAsset.Runtime
{
	public static class AssetBundleUtils
	{
		private static readonly Dictionary<string, AssetBundle> BundlesMap = new();

		public static void AddLiveBundle(AssetBundle assetBundle)
		{
			BundlesMap[assetBundle.name] = assetBundle;
		}

		public static bool IsLoadDuplicated(string assetBundleName)
		{
			return GetLoadedBundle(assetBundleName, out _);
		}

		public static bool GetLoadedBundle(string assetBundleName, out AssetBundle assetBundle)
		{
			var allLoadedAssetBundles = UnityEngine.AssetBundle.GetAllLoadedAssetBundles();
			assetBundle = allLoadedAssetBundles.FirstOrDefault(bundle => bundle.name == assetBundleName);
			return assetBundle != null;
		}

		public static bool GetLoadedBundleByPath(string bundleFilePath, out AssetBundle assetBundle)
		{
			var index = bundleFilePath.LastIndexOf('_');
			var index2 = bundleFilePath.LastIndexOf('/', index + 1);
			if (index2 >= 0 && index > index2 + 1)
			{
				var assetBundleName = bundleFilePath.Substring(index2 + 1, index - index2 - 1);
				return GetLoadedBundle(out assetBundle, assetBundleName);
			}
			else
			{
				MyLogger.LogError($"invalid bundle path: {bundleFilePath}");
				assetBundle = null;
				return false;
			}
		}

		private static bool GetLoadedBundle(out AssetBundle assetBundle, string assetBundleName)
		{
			if (BundlesMap.TryGetValue(assetBundleName, out assetBundle))
			{
				if (assetBundle != null)
				{
					return true;
				}
				else
				{
					BundlesMap.Remove(assetBundleName);
				}
			}

			var exist = GetLoadedBundle(assetBundleName, out assetBundle);
			if (exist)
			{
				BundlesMap[assetBundleName] = assetBundle;
			}

			return exist;
		}
	}
}