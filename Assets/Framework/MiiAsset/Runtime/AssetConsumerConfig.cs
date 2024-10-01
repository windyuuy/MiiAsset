using System;
using UnityEngine;

namespace Framework.MiiAsset.Runtime
{
	[Serializable]
	[CreateAssetMenu(fileName = "AssetConsumerConfig", menuName = "Mii/AssetConsumerConfig", order = 0)]
	public class AssetConsumerConfig : ScriptableObject
	{
		public string internalBaseUri = "mii/";
		public string externalBaseUri = "mii/";
	}
}