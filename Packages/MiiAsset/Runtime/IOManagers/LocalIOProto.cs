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
	public class LocalIOProto : IIOProto
	{
		public string CacheDir { get; set; }
		public string InternalDir { get; set; }
		public string ExternalDir { get; set; }
		public string CatalogName { get; set; }
		public int Timeout { get; set; }
		public bool IsInternalDirUpdating => false;

		public Task<bool> Init(IIOProtoInitOptions options)
		{
#if UNITY_EDITOR
			this.InternalDir = AssetHelper.GetInternalBuildPath();
#else
			this.InternalDir = Application.dataPath + "/" + options.InternalBaseUri;
#endif
			var persistentDataPath = Application.persistentDataPath;
			this.CacheDir = $"{persistentDataPath}/{options.BundleCacheDir}";
			this.ExternalDir = persistentDataPath + "/" + options.ExternalBaseUri;
			this.CatalogName = options.CatalogName;
			this.Timeout = options.Timeout;

			var ret = EnsurePersistDirs();

			return Task.FromResult(ret);
		}

		private bool EnsurePersistDirs()
		{
			try
			{
				EnsureDirectory(this.CacheDir);
				EnsureDirectory(this.ExternalDir);
				return true;
			}
			catch (Exception exception)
			{
				MyLogger.LogException(exception);
				return false;
			}
		}

		public bool Exists(string uri)
		{
			return File.Exists(uri);
		}

		public bool ExistsDir(string dir)
		{
			return Directory.Exists(dir);
		}

		public void EnsureDirectory(string dir)
		{
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
		}

		public void EnsureFileDirectory(string uri)
		{
			var dir = Path.GetDirectoryName(uri);
			EnsureDirectory(dir);
		}

		public Task WriteAllTextAsync(string cacheUri, string text, Encoding encoding)
		{
			return File.WriteAllTextAsync(cacheUri, text, encoding);
		}

		public Stream OpenRead(string uri)
		{
			return File.OpenRead(uri);
		}

		public async Task<string> ReadCatalog(string uri)
		{
			await using var stream = File.OpenRead(uri);
			using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
			var entry = zipArchive.GetEntry("catalog.json");
			Debug.Assert(entry != null, "entry!=null");
			using var streamReader = new StreamReader(entry.Open());
			var text = await streamReader.ReadToEndAsync();
			return text;
		}

		public Task<EnsureStreamingBundlesResult> EnsureStreamingBundles(string bundleName)
		{
			return Task.FromResult(EnsureStreamingBundlesResult.Exist);
		}

		public Stream OpenWrite(string filePath)
		{
			return File.OpenWrite(filePath);
		}

		public Task<string> ReadAllTextAsync(string uri, Encoding encoding)
		{
			return File.ReadAllTextAsync(uri, encoding);
		}

		public void Move(string from, string uri)
		{
			if (File.Exists(uri))
			{
				File.Delete(uri);
			}

			File.Move(from, uri);
		}

		public bool ExistsBundle(string bundleName)
		{
#if UNITY_ANDROID
			return File.Exists(CacheDir + bundleName);
#else
			return File.Exists(CacheDir + bundleName) || File.Exists(InternalDir + bundleName);
#endif
		}

		public bool EnsureBundle(string bundleName)
		{
			return true;
		}

		public void Delete(string filePath)
		{
			File.Delete(filePath);
		}

		public FilePathInfo[] ReadDir(string readDir)
		{
			var filePathInfos = Directory.GetFiles(readDir)
				.Select(filePath => new FilePathInfo(filePath, null, readDir))
				.ToArray();
			return filePathInfos;
		}

		public Task<byte[]> ReadAllBytesAsync(string uri)
		{
			return Task.FromResult(File.ReadAllBytes(uri));
		}

		public Task WriteAllBytesAsync(string uri, byte[] bytes)
		{
			return File.WriteAllBytesAsync(uri, bytes);
		}

		public void WriteAllBytes(string uri, byte[] bytes)
		{
			File.WriteAllBytes(uri, bytes);
		}

		public bool IsWebUri(string uri)
		{
			return uri.Contains("://");
		}

		protected CertificateHandler CertificateHandler;

		public void RegisterCertificateHandler(CertificateHandler certificateHandler)
		{
			CertificateHandler = certificateHandler;
		}

		public void SetUwr(UnityWebRequest uwr)
		{
			if (CertificateHandler != null)
			{
				uwr.certificateHandler = CertificateHandler;
				uwr.disposeCertificateHandlerOnDispose = false;
				uwr.timeout = this.Timeout;
			}
		}
	}
}