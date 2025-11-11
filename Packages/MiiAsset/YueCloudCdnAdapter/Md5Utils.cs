using System;
using System.Security.Cryptography;
using System.Text;

namespace MonoUtils.Encrypt
{
	public static class Md5Utils
	{
		private static MD5 _md5 = MD5.Create();
		private static readonly StringBuilder _stringBuilder = new StringBuilder(1024);
		private static readonly byte[] _bytes4096 = new byte[4096];
		private static readonly byte[] _hashBytes = new byte[1024];

		public static bool TryComputeMd5Hash4096(string message, out string hash)
		{
			var bytes4096 = _bytes4096;
			var hashBytes = _hashBytes;

			var len = Encoding.ASCII.GetBytes(message, 0, message.Length, bytes4096, 0);
			if (_md5.TryComputeHash(new ReadOnlySpan<byte>(bytes4096,0,len), new Span<byte>(hashBytes), out var hashLength))
			{
				_stringBuilder.Clear();
				for (int i = 0; i < hashLength; i++)
				{
					_stringBuilder.Append(hashBytes[i].ToString("x2"));
				}

				hash = _stringBuilder.ToString();
				return true;
			}
			else
			{
				hash = null;
				return false;
			}
		}

		public static string ComputeMd5Hash(string message)
		{
			_stringBuilder.Clear();

			var bytes = Encoding.ASCII.GetBytes(message);
			byte[] hash = _md5.ComputeHash(bytes);
			for (int i = 0; i < hash.Length; i++)
			{
				_stringBuilder.Append(hash[i].ToString("X2"));
			}

			return _stringBuilder.ToString();
		}
	}
}