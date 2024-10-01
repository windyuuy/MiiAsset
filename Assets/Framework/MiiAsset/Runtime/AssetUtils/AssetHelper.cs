using System.IO;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.AssetUtils
{
	public static class AssetHelper
	{
		public static string GetInternalBuildPath()
		{
#if UNITY_EDITOR
			var internalBaseUri = Path.GetFullPath(Application.dataPath + "/../Library/MiiAssets/mii/").Replace("\\", "/");
			if (!Directory.Exists(internalBaseUri))
			{
				Directory.CreateDirectory(internalBaseUri);
			}
#else
			var internalBaseUri = Application.dataPath + "/mii/";
#endif
			return internalBaseUri;
		}
	}
}