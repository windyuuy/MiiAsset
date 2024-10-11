using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.IOManagers
{
	public interface IIOProto
	{
		public bool Exists(string uri);
	}

	public class LocalIOProto : IIOProto
	{
		public readonly string CacheDir = $"{Application.persistentDataPath}/hotres/";
		public string InternalDir;

		public bool Exists(string uri)
		{
			return File.Exists(uri);
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

		public FileStream OpenRead(string uri)
		{
			return File.OpenRead(uri);
		}

		public Task<string> ReadAllTextAsync(string uri, Encoding utf8)
		{
			return File.ReadAllTextAsync(uri, utf8);
		}

		public void Move(string toTempPath, string uri)
		{
			if (File.Exists(uri))
			{
				File.Delete(uri);
			}

			File.Move(toTempPath, uri);
		}

		public bool ExistsBundle(string bundleName)
		{
			return File.Exists(CacheDir + bundleName) || File.Exists(InternalDir + bundleName);
		}

		public void Delete(string filePath)
		{
			File.Delete(filePath);
		}

		public string[] ReadDir(string readDir)
		{
			return Directory.GetFiles(readDir);
		}

		public Task<byte[]> ReadAllBytesAsync(string uri)
		{
			return Task.FromResult(File.ReadAllBytes(uri));
		}
	}

	public class IOManager
	{
		public static LocalIOProto LocalIOProto = new();
	}
}