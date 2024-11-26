using System.Collections.Generic;
using UnityEngine;

namespace MiiAsset.Runtime
{
	public static class AssetMonitor
	{
		internal static IAssetProvider Consumer => AssetLoader.Consumer;

		/// <summary>
		/// 仅用于开发时观测加载状态, 正式版本要移除
		/// </summary>
		/// <returns></returns>
		public static BundledAssetProvider GetBundledAssetProvider()
		{
			if (Consumer is BundledAssetProvider bundledAssetProvider)
			{
				return bundledAssetProvider;
			}
			else
			{
				Debug.LogError("cannot get BundledAssetProvider in EditorMode");
				return null;
			}
		}

		public static BundledAssetProvider BundledAssetProvider => GetBundledAssetProvider();

		public static Dictionary<string, IAssetBundleStatus> GetAllBundleLoadStatus()
		{
			var provider = GetBundledAssetProvider();
			if (provider != null)
			{
				return new Dictionary<string, IAssetBundleStatus>(provider.GetAllBundleLoadStatus());
			}

			return null;
		}

		public static Dictionary<string, IAssetBundleStatus> AllBundleLoadStatus => GetAllBundleLoadStatus();

		/// <summary>
		/// 仅用于开发时观测加载状态, 正式版本要移除
		/// </summary>
		/// <returns></returns>
		public static IAssetBundleStatus GetAssetBundleStatus(string bundleName)
		{
			var provider = GetBundledAssetProvider();
			if (provider != null)
			{
				return provider.GetBundleStatus(bundleName);
			}

			return null;
		}

		/// <summary>
		/// 仅用于开发时观测加载状态, 正式版本要移除
		/// </summary>
		/// <returns></returns>
		public static LoadAddressStatus GetAddressStatus(string address)
		{
			var provider = GetBundledAssetProvider();
			if (provider != null)
			{
				return provider.GetAddressStatus(address);
			}

			return null;
		}

		public static HashSet<string> GetAssetDependBundles(string address)
		{
			var provider = GetBundledAssetProvider();
			if (provider != null)
			{
				return new HashSet<string>(provider.GetAssetDependBundles(address));
			}

			return null;
		}
	}
}