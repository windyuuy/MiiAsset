using System.Collections.Generic;
using MiiAsset.Runtime.Adapter;
using UnityEngine;

namespace MiiAsset.Runtime
{
	public class AssetMonitor
	{
		public static readonly AssetMonitor Inst = new AssetMonitor();
		internal IAssetProvider Consumer => AssetLoader.Consumer;

		/// <summary>
		/// 仅用于开发时观测加载状态, 正式版本要移除
		/// </summary>
		/// <returns></returns>
		public BundledAssetProvider GetBundledAssetProvider()
		{
			if (Consumer is BundledAssetProvider bundledAssetProvider)
			{
				return bundledAssetProvider;
			}
			else
			{
				MyLogger.LogError("cannot get BundledAssetProvider in EditorMode");
				return null;
			}
		}

		/// <summary>
		/// 所有asset加载状态
		/// </summary>
		public BundledAssetProvider BundledAssetProvider => GetBundledAssetProvider();

		public Dictionary<string, IAssetBundleStatus> GetAllBundleLoadStatus()
		{
			var provider = GetBundledAssetProvider();
			if (provider != null)
			{
				return new Dictionary<string, IAssetBundleStatus>(provider.GetAllBundleLoadStatus());
			}

			return null;
		}

		/// <summary>
		/// 所有bundle加载状态
		/// </summary>
		public Dictionary<string, IAssetBundleStatus> AllBundleLoadStatus => GetAllBundleLoadStatus();

		/// <summary>
		/// 仅用于开发时观测加载状态, 正式版本要移除
		/// </summary>
		/// <returns></returns>
		public IAssetBundleStatus GetAssetBundleStatus(string bundleName)
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
		public LoadAddressStatus GetAddressStatus(string address)
		{
			var provider = GetBundledAssetProvider();
			if (provider != null)
			{
				return provider.GetAddressStatus(address);
			}

			return null;
		}

		/// <summary>
		/// 获取asset依赖的bundle
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public HashSet<string> GetAssetDependBundles(string address)
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