using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.AssetUtils;
using UnityEngine;
using UnityEngine.Networking;

namespace MiiAsset.Runtime.IOManagers
{
	public sealed class LocalIOProto : IOProtoBase
	{
		public override bool Exists(string uri)
		{
			return File.Exists(uri);
		}

		public override bool ExistsDir(string dir)
		{
			return Directory.Exists(dir);
		}

		public override void EnsureDirectory(string dir)
		{
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
		}

		public override Task WriteAllTextAsync(string cacheUri, string text, Encoding encoding)
		{
			return File.WriteAllTextAsync(cacheUri, text, encoding);
		}

		public override Stream OpenRead(string uri)
		{
			return File.OpenRead(uri);
		}

		public override async Task<string> ReadCatalog(string uri)
		{
		#if UNITY_WEBGL
			// WebGL不支持异步API
			// ReSharper disable once MethodHasAsyncOverload
			var bytes = File.ReadAllBytes(uri);
			using var readBytesStream = new MemoryStream(bytes);
			using var reader = new StreamReader(
				new BrotliStream(readBytesStream, CompressionMode.Decompress));
			// ReSharper disable once MethodHasAsyncOverload
			var text = reader.ReadToEnd();
		#else
			await using var readBytesStream = File.OpenRead(uri);
			using var reader = new StreamReader(
				new BrotliStream(readBytesStream, CompressionMode.Decompress));
			var text = reader.ReadToEnd();
		#endif
			return text;
		}

		public override Task<EnsureStreamingBundlesResult> EnsureStreamingBundles(string bundleFileName)
		{
			return Task.FromResult(EnsureStreamingBundlesResult.Exist);
		}

		public override Stream OpenWrite(string filePath)
		{
			return File.OpenWrite(filePath);
		}

		public override Task<string> ReadAllTextAsync(string uri, Encoding encoding)
		{
		#if UNITY_WEBGL
			return Task.FromResult(File.ReadAllText(uri, encoding));
		#else
			return File.ReadAllTextAsync(uri, encoding);
		#endif
		}

		public override void Move(string from, string uri)
		{
			if (File.Exists(uri))
			{
				File.Delete(uri);
			}

			File.Move(from, uri);
		}

		public override bool ExistsBundle(string bundleFileName)
		{
		#if UNITY_ANDROID
			return File.Exists(CacheDir + bundleFileName);
		#else
			return File.Exists(CacheDir + bundleFileName) || File.Exists(InternalDir + bundleFileName);
		#endif
		}

		public override bool EnsureBundle(string bundleFileName)
		{
			return ExistsBundle(bundleFileName);
		}

		public override void Delete(string filePath)
		{
			File.Delete(filePath);
		}

		public override FilePathInfo[] ReadDir(string readDir)
		{
			var filePathInfos = Directory.GetFiles(readDir)
				.Select(filePath => new FilePathInfo(filePath, null, readDir))
				.ToArray();
			return filePathInfos;
		}

		public override Task<T> ReadAllBytesAsync<T>(string uri, Func<byte[], T> handler)
		{
			return Task.FromResult(handler(File.ReadAllBytes(uri)));
		}

		public override Task WriteAllBytesAsync(string uri, byte[] bytes)
		{
			return File.WriteAllBytesAsync(uri, bytes);
		}

		public override Task<bool> CleanAllFileCaches()
		{
			var succeed = true;
			var externalDir = this.ExternalDir;
			var files = Directory.GetFiles(externalDir);
			foreach (var file in files)
			{
				try
				{
					File.Delete(file);
				}
				catch (Exception e)
				{
					succeed = false;
					MyLogger.LogException(e, "e30");
				}
			}

			return Task.FromResult(succeed);
		}
	}
}