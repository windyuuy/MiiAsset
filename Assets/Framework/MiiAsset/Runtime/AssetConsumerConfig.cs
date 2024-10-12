using System;
using UnityEngine;

namespace Framework.MiiAsset.Runtime
{
	[Serializable]
	[CreateAssetMenu(fileName = "AssetConsumerConfig", menuName = "Mii/AssetConsumerConfig", order = 0)]
	public class AssetConsumerConfig : ScriptableObject
	{
		public enum LoadType
		{
			LoadFromBundle = 0,
			LoadFromEditor = 1,
		}

		public string internalBaseUri = "mii/";
		public string externalBaseUri = "mii/";
		public string bundleCacheDir = "hotres/";
		public LoadType loadType;
	}
}