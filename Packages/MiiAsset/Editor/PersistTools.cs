using UnityEditor;
using UnityEngine;

namespace MiiAsset.Editor
{
	internal static class PersistTools
	{
		[MenuItem("Tools/Test/打开可写目录", false, 1)]
		public static void OpenPersistFolder()
		{
			Application.OpenURL(Application.persistentDataPath);
		}

		[MenuItem("Tools/Test/打开缓存目录", false, 1)]
		public static void OpenCacheFolder()
		{
			Application.OpenURL(Caching.currentCacheForWriting.path);
		}
	}
}