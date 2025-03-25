using System;
using MiiAsset.AssetWeakRefer.Runtime;
using UnityEditor;

namespace MiiAsset.AssetWeakRefer.Editor
{
	public static class AssetReferenceHelper
	{
		public static T LoadFromAsset<T>(UnityEngine.Object asset1) where T : AssetReference
		{
			var path = AssetDatabase.GetAssetPath(asset1);
			var guid1 = AssetDatabase.AssetPathToGUID(path);
			var assetReference = (T)Activator.CreateInstance(typeof(T), guid1);
			return assetReference;
		}

		public static T LoadFromAssetPath<T>(string path) where T : AssetReference
		{
			var guid1 = AssetDatabase.AssetPathToGUID(path);
			var assetReference = (T)Activator.CreateInstance(typeof(T), guid1);
			return assetReference;
		}
	}
}