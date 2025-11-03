using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MiiAsset.Runtime.AssetUtils
{
	public static class AssetHelper
	{
		public static string GetInternalBuildPath()
		{
		#if UNITY_EDITOR
			var internalBaseUri = Path.GetFullPath(Application.dataPath + "/../Library/MiiAssets/mii/")
				.Replace("\\", "/");
			if (!Directory.Exists(internalBaseUri))
			{
				Directory.CreateDirectory(internalBaseUri);
			}
		#else
			var internalBaseUri = Application.streamingAssetsPath + "/mii/";
		#endif
			return internalBaseUri;
		}

		public const string CatalogFileName = "catalog.json";

		public static async Task<string> LoadCompressedCatalog(Stream stream)
		{
			using var reader = new StreamReader(
				new BrotliStream(stream, CompressionMode.Decompress));
			var text = await reader.ReadToEndAsync();
			return text;
		}
	}
}