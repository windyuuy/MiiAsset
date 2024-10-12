using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
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

		public static async Task<string> LoadCompressedCatalog(Stream stream)
		{
			using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
			var entry = zipArchive.GetEntry("catalog.json");
			Debug.Assert(entry != null, "entry!=null");
			using var streamReader = new StreamReader(entry.Open());
			var text = await streamReader.ReadToEndAsync();
			return text;
		}
	}
}