using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
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
				// MyLogger.Log($"OpenSync: {path}, {fd}");
				FdMap.Add(path, fd);
			}

			// MyLogger.Log($"OpenSync2: {path}, {fd}");
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
			MyLogger.LogError("NotImplementException-Flush");
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			MyLogger.LogError("NotImplementException-Read");
			var readLen = Math.Min(buffer.Length - offset, count);
			var bytes = Fs.ReadFileSync(this.Uri, this.Position, readLen);
			var readLen1 = bytes.Length;
			Buffer.BlockCopy(bytes, 0, buffer, offset, readLen1);
			this.Position += readLen1;
			this._length = Math.Max(this._length, this.Position);
			return bytes.Length;
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
			MyLogger.LogError("WXWriteFileStream.SetLength not implement");
			this._length = value;
			this.Position = Math.Min(this._length, this.Position);
		}

		const int WriteSeg = 1024*1024*4;//11525472;
		static readonly byte[] TempBuffer = new byte[WriteSeg];

		public override void Write(byte[] buffer, int offset, int count)
		{
			var fd = Opener.Open(Uri, "a");
			// MyLogger.Log($"Write-Begine: {Path.GetFileName(Uri)}, {offset}, {count}, {buffer.Length}, {Position}, {fd}");

			if (buffer.Length > WriteSeg)
			{
				for (var i = 0; i < count; i += WriteSeg)
				{
					var writeLen = Math.Min(WriteSeg, count - i);
					Buffer.BlockCopy(buffer, offset + i, TempBuffer, 0, writeLen);
					Fs.WriteSync(new WriteSyncOption
					{
						fd = fd,
						position = Position,
						data = TempBuffer,
						offset = 0,
						length = writeLen,
					});
					// WaitLock.AddWait();
					// Fs.Write(new WriteOption
					// {
					// 	success = (resp) =>
					// 	{
					// 		MyLogger.Log($"Write-End: {Path.GetFileName(this.Uri)}, {Position}, {offset}, {count}");
					// 		this.WaitLock.OkOnce();
					// 	},
					// 	fail = (resp) =>
					// 	{
					// 		var reason =
					// 			$"WriteFile-Fail: {Path.GetFileName(Uri)}, code: {resp?.errCode}, errMsg: {resp?.errMsg}, offset: {offset}, count: {count}, len: {buffer.Length}, pos: {Position}, fd: {fd}";
					// 		MyLogger.LogError(reason);
					// 		this.WaitLock.FailOnce(reason);
					// 	},
					// 	fd = fd,
					// 	position = Position,
					// 	data = TempBuffer,
					// 	offset = 0,
					// 	length = writeLen,
					// });
					Position += writeLen;
				}
			}
			else
			{
				Fs.WriteSync(new WriteSyncOption
				{
					fd = fd,
					position = Position,
					data = buffer,
					offset = offset,
					length = count,
				});
				Position += count;
			}

			// MyLogger.Log($"Write-Wait: {Path.GetFileName(this.Uri)}, {Position}, {offset}, {count}");

			_length = Math.Max(Position, _length);
		}

		protected WaitLock WaitLock = new WaitLock(false);

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			// return this.WaitLock.Wait();
			return Task.CompletedTask;
		}

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => true;

		private long _length = 0;
		public override long Length => _length;

		public override long Position { get; set; }
	}
}
#endif