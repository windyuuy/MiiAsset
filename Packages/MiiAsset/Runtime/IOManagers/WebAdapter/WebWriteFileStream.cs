#if SUPPORT_WEBGL_LOCAL_STORAGE
using System.IO;
using MiiAsset.Runtime.Adapter;

namespace MiiAsset.Runtime.IOManagers
{
	public class WebWriteFileStream : Stream
	{
		protected readonly string Uri;

		public WebWriteFileStream(string uri)
		{
			Uri = uri;
			Buffer = new MemoryStream();
		}

		protected MemoryStream Buffer;

		public override void Close()
		{
			Flush();
			Buffer.Close();
		}

		protected override void Dispose(bool disposing)
		{
			if (Buffer != null)
			{
				Buffer.Dispose();
				Buffer = null;
			}
		}

		public override void Flush()
		{
			WebIOProtoWithLocalStorage.WriteAllBytes(Uri, Buffer.GetBuffer());
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new System.NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return Buffer.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			Buffer.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Buffer.Write(buffer, offset, count);
		}

		public override bool CanRead => Buffer.CanRead;
		public override bool CanSeek => Buffer.CanSeek;
		public override bool CanWrite => Buffer.CanWrite;
		public override long Length => Buffer.Length;

		public override long Position
		{
			get => Buffer.Position;
			set => Buffer.Position = value;
		}
	}
}
#endif
