using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Framework.MiiAsset.Runtime.IOManagers
{
	public interface IIOProto
	{
		public bool Exists(string uri);
	}

	public class LocalIOProto : IIOProto
	{
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

		public Task WriteAllTextAsync(string cacheUri, string text, Encoding utf8)
		{
			return File.WriteAllTextAsync(cacheUri, text, utf8);
		}

		public FileStream OpenRead(string uri)
		{
			return File.OpenRead(uri);
		}

		public Task<string> ReadAllTextAsync(string uri, Encoding utf8)
		{
			return File.ReadAllTextAsync(uri, utf8);
		}
	}

	public class IOManager
	{
		public static LocalIOProto LocalIOProto = new();
	}
}