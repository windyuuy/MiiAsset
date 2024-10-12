﻿using System;
using UnityEngine;

namespace Framework.MiiAsset.Runtime
{
	[Serializable]
	[CreateAssetMenu(fileName = "AssetConsumerConfig", menuName = "Mii/AssetConsumerConfig", order = 0)]
	public class AssetConsumerConfig : ScriptableObject, IAssetProvider.IProviderInitOptions
	{
		public enum LoadType
		{
			LoadFromBundle = 0,
			LoadFromEditor = 1,
		}

		public string internalBaseUri = "mii/";
		public string externalBaseUri = "mii/";
		public string bundleCacheDir = "hotres/";
		public string catalogName = "catalog.zip";
		public LoadType loadType;
		public string InternalBaseUri =>internalBaseUri;
		public string ExternalBaseUri =>externalBaseUri;
		public string BundleCacheDir =>bundleCacheDir;
		public string CatalogName =>catalogName;
	}
}