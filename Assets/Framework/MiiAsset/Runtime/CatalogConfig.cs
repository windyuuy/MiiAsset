using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Framework.MiiAsset.Runtime
{
	[Serializable]
	public class AssetBundleInfo
	{
		public string bundleName;
		public string fileName;
		public uint crc;
		public Hash128 hash128;
		public string[] deps;
		public string[] tags;
		public string[] entries;
		public string uri;
	}

	[Serializable]
	public class CatalogConfig
	{
		public AssetBundleInfo[] bundleInfos;
		[NonSerialized]
		public Dictionary<string, string> EntryBundleMap;
	}
}