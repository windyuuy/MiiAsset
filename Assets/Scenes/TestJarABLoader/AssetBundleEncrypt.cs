using System.IO;
using System.Text;
using UnityEngine;

namespace GameLib.Networking.Ext
{
	public class AssetBundleEncryptor
	{
		public static readonly AssetBundleEncryptor SharedEncryptor = new();

		public void Init(string key)
		{
		}

		public void Encrypt(byte[] array, long position, int offset, int count)
		{
		}

		public static string GetSharedKey(bool isEncrypt)
		{
			return "";
		}
	}
}