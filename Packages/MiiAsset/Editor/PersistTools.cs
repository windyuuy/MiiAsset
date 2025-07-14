using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiiAsset.Editor
{
	internal static class PersistTools
	{
		[MenuItem("Tools/Test/打开可写目录", false, 1)]
		public static void OpenPersistFolder()
		{
			var persistentDataPath = Application.persistentDataPath;
			Debug.Log($"可写目录: {persistentDataPath}");
			Application.OpenURL(persistentDataPath);
		}

		[MenuItem("Tools/Test/打开缓存目录", false, 1)]
		public static void OpenCacheFolder()
		{
			var cachePath = Caching.currentCacheForWriting.path;
			if (Directory.Exists(cachePath))
			{
				Debug.Log($"打开缓存目录: {cachePath}");
				Application.OpenURL(cachePath);
			}
			else
			{
				Debug.LogError($"缓存目录不存在: {cachePath}");
			}
		}
	}
}