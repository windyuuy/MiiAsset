using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace MiiAsset.Runtime.IOStreams
{
	public class LoadAssetBundleStream : Stream, IRandomWritePumpStream
	{
		protected override void Dispose(bool disposing)
		{
			if (ReadStream != null)
			{
				ReadStream.Dispose();
				ReadStream = null;
			}

			base.Dispose(disposing);
		}

		public override void Flush()
		{
			Debug.LogError("NotImplementException");
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return ReadStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			if (origin == SeekOrigin.Begin)
			{
				return ReadStream.SetPosition(offset);
			}
			else if (origin == SeekOrigin.Current)
			{
				return ReadStream.SetPosition(offset + ReadStream.GetPosition());
			}
			else if (origin == SeekOrigin.End)
			{
				return ReadStream.SetPosition(ReadStream.GetLength() - ReadStream.GetPosition());
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;
		public override long Length => ReadStream.GetLength();

		public override long Position
		{
			get => ReadStream.GetPosition();
			set => ReadStream.SetPosition(value);
		}

		public IRandomReadStream ReadStream { get; set; }

		public LoadAssetBundleStream Init(string uri)
		{
			this.Uri = uri;
			return this;
		}

		protected string Uri;
	}
}