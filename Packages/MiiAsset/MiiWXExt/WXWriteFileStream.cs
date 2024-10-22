using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_WEBGL && SUPPORT_WECHATGAME
using WeChatWASM;

namespace MiiAsset.Runtime.IOManagers
{
	public class WXSafeOpener
	{
		protected static readonly Dictionary<string, string> FdMap = new(16);
		protected readonly WXFileSystemManager Fs = WX.GetFileSystemManager();

		public string Open(string path, string flag)
		{
			if (!FdMap.TryGetValue(path, out var fd))
			{
				while (FdMap.Count >= 12)
				{
					var item = FdMap.First();
					Fs.CloseSync(new()
					{
						fd = item.Value,
					});
					FdMap.Remove(item.Key);
				}

				fd = Fs.OpenSync(new()
				{
					filePath = path,
					flag = flag,
				});
				FdMap.Add(path, fd);
			}

			return fd;
		}
	}

	public class WXWriteFileStream : Stream
	{
		protected static readonly WXSafeOpener Opener = new();
		protected readonly WXFileSystemManager Fs;
		protected readonly string Uri;

		public WXWriteFileStream(WXFileSystemManager fs, string uri)
		{
			Fs = fs;
			Uri = uri;
		}

		public override void Flush()
		{
			Debug.LogError("NotImplementException");
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new System.NotImplementedException();
		}


		public override long Seek(long offset, SeekOrigin origin)
		{
			if (origin == SeekOrigin.Begin)
			{
				Position = offset;
			}
			else if (origin == SeekOrigin.Current)
			{
				Position += offset;
			}
			else
			{
				var len = this.Length;
				Position = len + offset;
			}

			return Position;
		}

		public override void SetLength(long value)
		{
			throw new System.NotImplementedException("WXWriteFileStream.SetLength not implement");
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			var fd = Opener.Open(Uri, "a");
			Fs.WriteSync(new WriteSyncOption
			{
				data = buffer,
				fd = fd,
				length = count,
				offset = offset,
				position = Position,
			});
		}

		public override bool CanRead => false;
		public override bool CanSeek => true;
		public override bool CanWrite => true;

		public override long Length
		{
			get { throw new System.NotImplementedException("WXWriteFileStream.get_Length not implement"); }
		}

		public override long Position { get; set; }
	}
}
#endif
