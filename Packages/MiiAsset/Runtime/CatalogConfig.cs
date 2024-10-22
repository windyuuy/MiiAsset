using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MiiAsset.Runtime
{
	[Serializable]
	public class AssetBundleInfo
	{
		public string bundleName;
		public string fileName;
		public uint crc;
		/// <summary>
		/// 文件大小, 多少byte
		/// </summary>
		public long size;
		public Hash128 hash128;
		public string[] deps;
		public string[] tags;
		public string[] entries;
		public string[] guids;
		// public string uri;
		[NonSerialized]
		public bool IsOffline;
	}

	[Serializable]
	public class CatalogConfig
	{
		public AssetBundleInfo[] bundleInfos;
		[NonSerialized]
		public Dictionary<string, string> EntryBundleMap;
	}
}