using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.MiiAsset.Runtime
{
	[Serializable]
	public class AssetBundleInfo
	{
		public string bundleName;
		public uint crc;
		public Hash128 hash128;
		public string[] deps;
		public string[] entries;
	}

	[Serializable]
	public class CatalogConfig
	{
		public AssetBundleInfo[] bundleInfos;
		[NonSerialized]
		public Dictionary<string, string> EntryBundleMap;
	}
}