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

		public string[] GetAllBundleNames()
		{
			var provider = GetBundledAssetProvider();
			if (provider != null)
			{
				return provider.GetAllBundleNames();
			}
			else
			{
				return null;
			}
		}

		public AssetBundleInfo[] GetAllAssetBundleInfos()
		{
			var provider = GetBundledAssetProvider();
			if (provider != null)
			{
				return provider.GetAllAssetBundleInfos();
			}

			return null;
		}

		/// <summary>
		/// 获取所有AssetBundle加载状态(包括已卸载的)
		/// </summary>
		/// <returns></returns>
		public BundledAssetProvider BundledAssetProvider => GetBundledAssetProvider();

		/// <summary>
		/// 获取所有AssetBundle加载状态(包括已卸载的)
		/// </summary>
		/// <returns></returns>
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
		/// 正在使用中的AssetBundle加载状态
		/// </summary>
		/// <value></value>
		public Dictionary<string, IAssetBundleStatus> UsingBundleLoadStatus => GetUsingBundleLoadStatus();

		/// <summary>
		/// 正在使用中的AssetBundle加载状态
		/// </summary>
		/// <value></value>
		private Dictionary<string, IAssetBundleStatus> GetUsingBundleLoadStatus()
		{
			var dictionary = new Dictionary<string, IAssetBundleStatus>();
			foreach (var (key, value) in GetAllBundleLoadStatus())
			{
				if (value.IsUsing)
				{
					dictionary.Add(key, value);
				}
			}

			return dictionary;
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