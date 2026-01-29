#if SUPPORT_WEBGL_LOCAL_STORAGE
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lang.Encoding;
using MiiAsset.Runtime.Adapter;
using UnityEngine.Networking;
using UnityEngine.Pool;

namespace MiiAsset.Runtime.IOManagers
{
	// with localStorage
	public sealed class WebIOProtoWithLocalStorage : IOProtoBase
	{
		protected override bool EnsurePersistDirs()
		{
			return true;
		}

		public override bool Exists(string uri)
		{
			return WebGLLocalStorage.HasKey(uri);
		}

		public override bool ExistsDir(string dir)
		{
			// 按key存取, 不需要创建dir
			return true;
		}

		public override void EnsureDirectory(string dir)
		{
			// 按key存取, 不需要创建dir
		}

		public override void EnsureFileDirectory(string uri)
		{
			// 按key存取, 不需要创建dir
		}

		public override Task WriteAllTextAsync(string cacheUri, string text, Encoding encoding)
		{
			if (encoding.CodePage != Encoding.UTF8.CodePage || encoding.CodePage != EncodingExt.UTF8WithoutBom.CodePage)
			{
				throw new NotImplementedException(
					$"WriteAllTextAsync only support UTF8: {encoding.EncodingName}, {encoding.CodePage}");
			}

			WebGLLocalStorage.SetItem(cacheUri, text);
			return Task.CompletedTask;
		}

		public override Stream OpenRead(string uri)
		{
			var bytes = ReadAllBytes(uri);
			var memoryStream = new MemoryStream(bytes);
			return memoryStream;
		}

		public override Stream OpenWrite(string filePath)
		{
			var writeStream = new WebWriteFileStream(filePath);
			return writeStream;
		}

		public override Task<string> ReadAllTextAsync(string uri, Encoding encoding)
		{
			return Task.FromResult(WebGLLocalStorage.GetItem(uri));
		}

		public override void Move(string from, string uri)
		{
			WebGLLocalStorage.RenameKey(from, uri);
		}

		public override bool ExistsBundle(string bundleFileName)
		{
			var filePath = $"{CacheDir}{bundleFileName}";
			return WebGLLocalStorage.HasKey(filePath);
		}

		public override bool EnsureBundle(string bundleFileName)
		{
			return ExistsBundle(bundleFileName);
		}

		public override void Delete(string uri)
		{
			// WebGLLocalStorage.RemoveItem(uri);
			var lengthKey = $"{uri}_$pl";
			if (WebGLLocalStorage.HasKey(lengthKey))
			{
				var lengthInfo = WebGLLocalStorage.GetItem(lengthKey);
				var lengthBytes = Convert.FromBase64String(lengthInfo);
				var partsCount = BitConverter.ToInt32(lengthBytes, 4);

				WebGLLocalStorage.RemoveItem(uri);
				WebGLLocalStorage.RemoveItem(lengthKey);
				for (var partIndex = 1; partIndex < partsCount; partIndex++)
				{
					WebGLLocalStorage.RemoveItem($"{uri}_$p{partIndex}");
				}
			}
			else
			{
				WebGLLocalStorage.RemoveItem(uri);
			}
		}

		public override FilePathInfo[] ReadDir(string readDir)
		{
			if (!readDir.EndsWith("/"))
			{
				readDir += "/";
			}

			WebGLLocalStorage.RefreshKeysCacheWithPrefix(readDir);
			var fullPaths = ListPool<string>.Get();
			WebGLLocalStorage.GetAllKeys(fullPaths);
			var files = fullPaths
				.Select(fullPath =>
				{
					var fileName = fullPath.Substring(readDir.Length);
					return new FilePathInfo(null, fileName, readDir);
				})
				.ToArray();
			fullPaths.Clear();
			ListPool<string>.Release(fullPaths);
			return files;
		}

		public override Task<T> ReadAllBytesAsync<T>(string uri, Func<byte[], T> handler)
		{
			var bytes = ReadAllBytes(uri);
			return Task.FromResult(handler(bytes));
		}

		public override Task WriteAllBytesAsync(string uri, byte[] bytes)
		{
			WriteAllBytes(uri, bytes);
			return Task.CompletedTask;
		}

		public static byte[] ReadAllBytes(string uri)
		{
			var lengthKey = $"{uri}_$pl";
			if (WebGLLocalStorage.HasKey(lengthKey))
			{
				var lengthInfo = WebGLLocalStorage.GetItem(lengthKey);
				var lengthBytes = Convert.FromBase64String(lengthInfo);
				var bytesLength = BitConverter.ToInt32(lengthBytes, 0);
				var partsCount = BitConverter.ToInt32(lengthBytes, 4);
				var buffer = new byte[bytesLength];
				var textBase64 = WebGLLocalStorage.GetItem(uri);
				var offset = 0;
				Convert.TryFromBase64String(textBase64, new Span<byte>(buffer, offset, buffer.Length),
					out var bytesWritten);
				offset += bytesWritten;

				for (var partIndex = 1; partIndex < partsCount; partIndex++)
				{
					var textBase64Part = WebGLLocalStorage.GetItem($"{uri}_$p{partIndex}");
					Convert.TryFromBase64String(textBase64Part,
						new Span<byte>(buffer, offset, buffer.Length - bytesWritten), out var bytesWritten2);
					offset += bytesWritten2;
				}

				return buffer;
			}
			else
			{
				var textBase64 = WebGLLocalStorage.GetItem(uri);
				// 改成base64
				var bytes = Convert.FromBase64String(textBase64);
				return bytes;
			}
		}

		public static void WriteAllBytes(string uri, byte[] bytes)
		{
			const int partLenMax = 2 * 1024;// * 1024;
			var bytesLength = bytes.Length;
			if (bytesLength > partLenMax)
			{
				var partsCount = (bytesLength + partLenMax - 1) / partLenMax;

				for (var i = 1; i < partsCount; i++)
				{
					var textBase64Part =
						Convert.ToBase64String(new ReadOnlySpan<byte>(bytes, i * partLenMax, partLenMax));
					WebGLLocalStorage.SetItem($"{uri}_$p{i}", textBase64Part);
				}

				var textBase64 = Convert.ToBase64String(new ReadOnlySpan<byte>(bytes, 0, partLenMax));
				WebGLLocalStorage.SetItem(uri, textBase64);

				var lenInfoBytes = new byte[8];
				BitConverter.TryWriteBytes(new Span<byte>(lenInfoBytes, 0, 0), bytesLength);
				BitConverter.TryWriteBytes(new Span<byte>(lenInfoBytes, 0, 4), partsCount);
				var lenInfo = Convert.ToBase64String(lenInfoBytes);
				var lengthKey = $"{uri}_$pl";
				WebGLLocalStorage.SetItem(lengthKey, lenInfo);
			}
			else
			{
				var textBase64 = Convert.ToBase64String(bytes);
				WebGLLocalStorage.SetItem(uri, textBase64);
			}
		}

		public override Task<string> ReadCatalog(string uri)
		{
			var textBase64 = WebGLLocalStorage.GetItem(uri);
			// 改成base64
			var bytes = Convert.FromBase64String(textBase64);
			using var readBytesStream = new MemoryStream(bytes);
			using var reader = new StreamReader(
				new BrotliStream(readBytesStream, CompressionMode.Decompress));
			var text = reader.ReadToEnd();
			return Task.FromResult(text);
		}

		public override Task<EnsureStreamingBundlesResult> EnsureStreamingBundles(string bundleName)
		{
			return EnsureStreamingBundlesWithUwr(bundleName);
		}

		public override Task<bool> CleanAllFileCaches()
		{
			MyLogger.Log("clean MiiAsset begin");
			var list = ListPool<string>.Get();
			WebGLLocalStorage.RefreshKeysCacheWithPrefix(CacheDir);
			WebGLLocalStorage.GetAllKeys(list);
			WebGLLocalStorage.RefreshKeysCacheWithPrefix(ExternalDir);
			WebGLLocalStorage.GetAllKeys(list);
			foreach (var key in list)
			{
				WebGLLocalStorage.RemoveItem(key);
			}

			list.Clear();
			ListPool<string>.Release(list);
			MyLogger.Log("clean MiiAsset done");
			return Task.FromResult(true);
		}
	}
}
#endif
