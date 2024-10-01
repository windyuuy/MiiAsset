using System;
using System.IO;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;

namespace Framework.MiiAsset.Runtime.IOStreams
{
	public class ReadFileStream : IRandomReadStream
	{
		public string Uri;

		public ReadFileStream Init(string uri)
		{
			this.Uri = uri;
			this.FileStream = IOManager.LocalIOProto.OpenRead(uri);
			return this;
		}

		public bool Exist()
		{
			return IOManager.LocalIOProto.Exists(this.Uri);
		}

		protected FileStream FileStream;

		public int Read(byte[] data, int offset, int len)
		{
			return FileStream.Read(data, offset, len);
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