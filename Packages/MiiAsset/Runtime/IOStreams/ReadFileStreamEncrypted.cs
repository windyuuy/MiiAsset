using System;
using System.IO;
using System.Threading.Tasks;
using MiiAsset.Runtime.Encrypt;
using MiiAsset.Runtime.IOManagers;

namespace MiiAsset.Runtime.IOStreams
{
	public class ReadFileStreamEncrypted : IRandomReadStream
	{
		public string Uri;

		public ReadFileStreamEncrypted Init(string uri)
		{
			this.Uri = uri;
			return this;
		}

		public bool Exist()
		{
			return IOManager.LocalIOProto.Exists(this.Uri);
		}

		protected Stream FileStream;

		public int Read(byte[] data, int offset, int len)
		{
			var position = FileStream.Position;
			var readLen = FileStream.Read(data, offset, len);
			SharedEncrypt.Encryptor.Encrypt(data, position, offset, readLen);
			return readLen;
		}

		public void Run()
		{
			this.FileStream = IOManager.LocalIOProto.OpenRead(Uri);
		}

		public void Dispose()
		{
			if (FileStream != null)
			{
				FileStream.Dispose();
				FileStream = null;
			}
		}

		public long GetLength()
		{
			return FileStream.Length;
		}

		public long GetPosition()
		{
			return FileStream.Position;
		}

		public long SetPosition(long pos)
		{
			return FileStream.Position = pos;
		}
	}
}