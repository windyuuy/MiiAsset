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
		[NonSerialized] public bool IsOffline;
	}

	public enum AddressLoadType
	{
		Common = 0,
		AtlasSprite = 1,
	}

	[Serializable]
	public class ExtraAddressInfo
	{
		public AddressLoadType loadType;
		public string address;
		public string guid;
		public string sourceAddress;
		public string key;
	}

	[Serializable]
	public class CatalogConfig
	{
		public AssetBundleInfo[] bundleInfos;
		public ExtraAddressInfo[] extraAddressInfos;
		[NonSerialized] public Dictionary<string, string> EntryBundleMap;
	}
}