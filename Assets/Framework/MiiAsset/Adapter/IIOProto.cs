using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Framework.MiiAsset.Runtime.IOManagers
{
	public interface IIOProtoInitOptions
	{
		public string InternalBaseUri { get; }
		public string ExternalBaseUri { get; }
		public string BundleCacheDir { get; }
		public string CatalogName { get; }
	}
	public interface IIOProto
	{
		public string CacheDir { get; }
		public string InternalDir { get; }
		public string ExternalDir { get; }
		public string CatalogName { get; }
		public bool IsInternalDirUpdating { get; }

		public Task<bool> Init(IIOProtoInitOptions options);

		public bool Exists(string uri);

		public void EnsureDirectory(string dir);

		public void EnsureFileDirectory(string uri);

		public Task WriteAllTextAsync(string cacheUri, string text, Encoding encoding);

		public Stream OpenRead(string uri);

		public Stream OpenWrite(string filePath);
		public Task<string> ReadAllTextAsync(string uri, Encoding encoding);

		public void Move(string from, string uri);

		public bool ExistsBundle(string bundleName);

		public bool EnsureBundle(string bundleName);

		public void Delete(string filePath);

		public string[] ReadDir(string readDir);

		public Task<byte[]> ReadAllBytesAsync(string uri);
		public Task WriteAllBytesAsync(string uri, byte[] bytes);

		public bool IsWebUri(string uri);

		public Task<string> ReadCatalog(string uri);
		public Task<bool> EnsureStreamingBundles(string bundleName);
		public void RegisterCertificateHandler(CertificateHandler certificateHandler);
		public void SetUwr(UnityWebRequest uwr);
	}
}