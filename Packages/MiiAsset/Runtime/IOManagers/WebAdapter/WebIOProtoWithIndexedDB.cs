using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebGLIndexedDB;

namespace MiiAsset.Runtime.IOManagers
{
	public sealed class WebIOProtoWithIndexedDB : IOProtoBase
	{
		public override bool Exists(string uri)
		{
			throw new NotImplementedException();
		}

		public override bool ExistsDir(string dir)
		{
			return true;
		}

		public override void EnsureDirectory(string dir)
		{
		}

		public override Task WriteAllTextAsync(string cacheUri, string text, Encoding encoding)
		{
			var ts = new TaskCompletionSource<bool>();
			IndexedDB.WriteKey(cacheUri, text, (success, err) =>
			{
				if (success)
				{
					ts.SetResult(success);
				}
				else
				{
					ts.SetException(new Exception(err));
				}
			});
			return ts.Task;
		}

		public override Stream OpenRead(string uri)
		{
			throw new NotImplementedException();
		}

		public override Stream OpenWrite(string filePath)
		{
			throw new NotImplementedException();
		}

		public override Task<string> ReadAllTextAsync(string uri, Encoding encoding)
		{
			throw new NotImplementedException();
		}

		public override void Move(string from, string uri)
		{
			throw new NotImplementedException();
		}

		public override bool ExistsBundle(string bundleFileName)
		{
			throw new NotImplementedException();
		}

		public override bool EnsureBundle(string bundleFileName)
		{
			throw new NotImplementedException();
		}

		public override void Delete(string filePath)
		{
			throw new NotImplementedException();
		}

		public override Task DeleteAsync(string filePath)
		{
			var ts = new TaskCompletionSource<bool>();
			IndexedDB.DeleteKey(filePath, (success, err) =>
			{
				if (success)
				{
					ts.SetResult(success);
				}
				else
				{
					ts.SetException(new Exception(err));
				}
			});
			return ts.Task;
		}

		public override FilePathInfo[] ReadDir(string readDir)
		{
			throw new NotImplementedException();
		}

		public override Task<T> ReadAllBytesAsync<T>(string uri, Func<byte[], T> handler)
		{
			var ts = new TaskCompletionSource<T>();
			IndexedDB.ReadKeyBinary(uri, (success, data, err) =>
			{
				if (success)
				{
					T result = handler(data);
					ts.SetResult(result);
				}
				else
				{
					ts.SetException(new Exception(err));
				}
			});
			return ts.Task;
		}

		public override Task WriteAllBytesAsync(string uri, byte[] bytes)
		{
			throw new NotImplementedException();
		}

		public override Task<string> ReadCatalog(string uri)
		{
			throw new NotImplementedException();
		}

		public override Task<EnsureStreamingBundlesResult> EnsureStreamingBundles(string bundleFileName)
		{
			throw new NotImplementedException();
		}

		public override Task<bool> CleanAllFileCaches()
		{
			throw new NotImplementedException();
		}
	}
}