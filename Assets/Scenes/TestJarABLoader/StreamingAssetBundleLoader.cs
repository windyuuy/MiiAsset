using System;
using UnityEngine;

namespace GameLib.Networking.Ext
{
	public class StreamingAssetBundleLoader
	{
		public static AssetBundleCreateRequest LoadFromFileAsync(string path, uint crc, ulong offset, bool isEncrypt)
		{
			JarFileStream fileStream;
			{
				JarFileStream fileStream1 = new();
				fileStream = fileStream1;
			}
			fileStream.Open(path);

			var isQuiting = false;
			Action clean = () =>
			{
				fileStream.Close();
				fileStream.Dispose();
			};
			Application.quitting += () =>
			{
				isQuiting = true;
				clean();
			};

			{
				// 不计入进度展示
				var op4 = AssetBundle.LoadFromStreamAsync(fileStream, crc);
				op4.completed += opR =>
				{
					if (op4.assetBundle != null)
					{
						var assetBundle = op4.assetBundle;
						TestAssetBundle(assetBundle);
						assetBundle.Unload(true);
					}
				};
				return op4;
			}
		}

		private static void TestAssetBundle(AssetBundle assetBundle)
		{
			Debug.Log("begin-load");
			var objs = assetBundle.LoadAllAssets();
			Debug.Log("load-done");
			foreach (var o in objs)
			{
				Debug.Log(o.name);
			}

			Debug.Log("readed");
		}
	}
}