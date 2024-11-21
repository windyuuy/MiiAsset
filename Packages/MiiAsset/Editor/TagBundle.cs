using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEngine.Build.Pipeline;

namespace MiiAsset.Editor.Build
{
	public class BuildAssetBundlesResult
	{
		public string Msg;
		public ReturnCode Code;
		public BuildResult BuildResult;
	}

	public class ExtraBuildOptions
	{
		public string UpdateTunnel = "";
		public string CatalogType = "";
		public bool BuildGuids = false;
		public bool BuildHintCode = true;
	}

	[Serializable]
	public class WriteRecords
	{
		public List<string> records = new();
	}

	public class TagBundle
	{
		public HashSet<string> Guids = new();
		public string[] Tags;
		public string[] TagsAdditional;

		/// <summary>
		/// for debug
		/// </summary>
		public string[] Addresses => Guids.Select(guid => AssetDatabase.GUIDToAssetPath((string)guid)).ToArray();

		public string TagsUKey;
		public string BundleFileName => $"{GetTagsKey()}_{BuildInfo.Hash}.bundle";
		public HashSet<string> DepTagNames = new();
		public HashSet<string> Deps = new();
		public BundleDetails BuildInfo;
		public bool IsOffline = false;

		/// <summary>
		/// 多少byte
		/// </summary>
		public long FileSize;

		public string GetTagsKey()
		{
			return string.Join("_", Tags);
		}

		public string GetBundleName()
		{
			return $"{GetTagsKey()}";
		}

		public string[] GetAssetNames()
		{
			return Guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();
		}

		public string[] GetAssetAddresses()
		{
			return GetAssetNames();
		}

		public string GetBundlePathWithHash(string dir)
		{
			return $"{dir}/{this.BundleFileName}";
		}
	}
}