using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;

namespace U3DUdpater.Editor.BuildPipelineTasks
{
	/// <summary>
	/// The BuildTask used to append the asset hash to the internal bundle name.
	/// </summary>
	public class AddHashToBundleNameTask : IBuildTask
	{
		/// <summary>
		/// The task version.
		/// </summary>
		public int Version
		{
			get { return 1; }
		}

#pragma warning disable 649
		[InjectContext(ContextUsage.In)] IBuildParameters m_Parameters;

		[InjectContext(ContextUsage.In)] IBundleBuildContent m_BuildContent;

		[InjectContext] IDependencyData m_DependencyData;

		[InjectContext(ContextUsage.InOut, true)]
		IBuildSpriteData m_SpriteData;

		[InjectContext(ContextUsage.In, true)] IBuildCache m_Cache;
#pragma warning restore 649

		/// <summary>
		/// Runs the AddHashToBundleNameTask.
		/// </summary>
		/// <returns>Success.</returns>
		public ReturnCode Run()
		{
			var newBundleLayout = new Dictionary<string, List<GUID>>();
			foreach (var bid in m_BuildContent.BundleLayout)
			{
				var hash = GetAssetsHash(bid.Value);
				var newName = $"{bid.Key}_{hash}.bundle";
				newBundleLayout.Add(newName, bid.Value);
			}

			m_BuildContent.BundleLayout.Clear();

			foreach (var bid in newBundleLayout)
				m_BuildContent.BundleLayout.Add(bid.Key, bid.Value);
			return ReturnCode.Success;
		}

		internal RawHash GetAssetsHash(List<GUID> assets)
		{
			assets.Sort();
			var hashes = new HashSet<Hash128>();
			foreach (var g in assets)
			{
				AssetLoadInfo assetInfo;
				if (m_DependencyData.AssetInfo.TryGetValue(g, out assetInfo))
				{
					GetAssetHashes(hashes, g, m_Cache != null && m_Parameters.UseCache);
				}
			}

			return HashingMethods.Calculate(hashes.ToArray());
		}

		void GetAssetHashes(HashSet<Hash128> hashes, GUID g, bool useCache)
		{
			if (useCache)
			{
				hashes.Add(m_Cache.GetCacheEntry(g, Version).Hash);
			}
			else
				hashes.Add(AssetDatabase.GetAssetDependencyHash(AssetDatabase.GUIDToAssetPath(g.ToString())));
		}
	}
}