using System.IO;
using System.Text;
using UnityEngine;

namespace MiiAsset.Runtime.Encrypt
{
	public class AssetBundleEncryptor
	{
		public static bool IsEncryptEnabled = false;

		public static string GetSharedKey(bool isEncrypt)
		{
			return (IsEncryptEnabled || isEncrypt) ? _sharedKey : null;
		}

		public static bool IsEncrypt(bool isEncrypt)
		{
			return !string.IsNullOrEmpty(AssetBundleEncryptor.GetSharedKey(isEncrypt));
		}

		private static readonly string _sharedKey = "wjfowihi-wlfknsdlkf=owihefo4nfoh";

		public static readonly AssetBundleEncryptor SharedEncryptor = new();

		// public static string SharedKey = null;
		protected byte[] KeyBytes;
		protected int KeyLength;

		private bool _isInited = false;

		public void Init(string key)
		{
			if (_isInited)
			{
				return;
			}

			_isInited = true;

			var keyLen = key.Length;
			KeyBytes = new byte[keyLen];
			Encoding.UTF8.GetBytes(key, 0, keyLen, KeyBytes, 0);
			Debug.Assert(keyLen == KeyBytes.Length);
			KeyLength = KeyBytes.Length;
		}

		public void EncryptFile(string path)
		{
			EncryptFile(path, path, 4096 * 8);
		}

		public void EncryptFile(string pathIn, string pathOut, int chunkSize)
		{
			using var fsWrite = new FileStream(pathOut, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
			using var fsRead = new FileStream(pathIn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var buff = new byte[chunkSize];
			while (fsRead.CanSeek)
			{
				Debug.Assert(fsRead.Position == fsWrite.Position);
				var pos = fsRead.Position;
				var len = fsRead.Read(buff, 0, chunkSize);
				if (len == 0)
				{
					break;
				}

				this.Encrypt(buff, pos, 0, len);

				fsWrite.Write(buff, 0, len);
			}

			fsWrite.Flush();
			fsRead.Close();
			fsWrite.Close();
		}

		public void Encrypt(byte[] array)
		{
			Encrypt(array, 0, 0, array.Length);
		}

		public void Encrypt(byte[] array, long position, int offset, int count)
		{
			// for (var i = 0; i < array.Length && i<count; i++)
			// {
			// 	array[i+offset] ^= 0xFC;
			// }
			const long segStage1 = 256;
			const long segStage2 = 4096 * 8 * 16;
			long iSeg = 0;
			var iPos = position;
			for (; iPos < segStage1 && iSeg < count; iPos++, iSeg++)
			{
				array[iSeg + offset] ^= 0xDF;
			}

			if (segStage1 <= iPos && iPos < segStage2 && iSeg < count)
			{
				var segStep = 4;
				var iPos2 = segStage2 + (iPos - segStage2 + segStep - 1) & (~0x3);
				iSeg += (iPos2 - iPos);
				iPos = iPos2;
				for (; iPos < segStage2 && iSeg < count; iPos += segStep, iSeg += segStep)
				{
					array[iSeg + offset] ^= KeyBytes[iPos % KeyLength];
				}
			}

			if (segStage2 <= iPos && iSeg < count)
			{
				var segStep = 256;
				var iPos2 = segStage2 + (iPos - segStage2 + segStep - 1) & (~0xFF);
				iSeg += (iPos2 - iPos);
				iPos = iPos2;
				for (; iSeg < count; iPos += segStep, iSeg += segStep)
				{
					array[iSeg + offset] ^= KeyBytes[iPos % KeyLength];
				}
			}
		}
	}
}