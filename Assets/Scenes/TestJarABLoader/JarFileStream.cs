using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace GameLib.Networking.Ext
{
	public class JarFileStream : Stream
	{
		protected AndroidJavaObject NativeJarFileStream;

		public string Name = "unknown";

		protected Dictionary<int, int> AttachedIds = new();

		protected int MainThreadId;

		public JarFileStream()
		{
			MainThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		protected void AttachThread()
		{
			var id = Thread.CurrentThread.ManagedThreadId;
			if (id == MainThreadId)
			{
				return;
			}

			if (!AttachedIds.ContainsKey(id))
			{
				Debug.Log($"toattach:{this.GetHashCode()}-{id}");
				var ret = AndroidJNI.AttachCurrentThread();
				Debug.Assert(ret == 0, $"AndroidJNI.AttachCurrentThread-failed: {ret}");
				AttachedIds.Add(id, 0);
			}

			AttachedIds[id]++;
		}

		protected void DetachThread()
		{
			var id = Thread.CurrentThread.ManagedThreadId;
			if (id == MainThreadId)
			{
				return;
			}

			if (AttachedIds.ContainsKey(id))
			{
				AttachedIds[id]--;

				if (AttachedIds[id] == 0)
				{
					var ret = AndroidJNI.DetachCurrentThread();
					Debug.Assert(ret == 0, $"AndroidJNI.AttachCurrentThread-failed: {ret}");
					AttachedIds.Remove(id);
				}
			}
		}

		protected T AttachAndRun<T>(Func<T> action)
		{
			try
			{
				AttachThread();
				var ret = action();
				return ret;
			}
			finally
			{
				DetachThread();
			}
		}

		protected void AttachAndRun(Action action)
		{
			try
			{
				AttachThread();
				action();
			}
			finally
			{
				DetachThread();
			}
		}

		protected long _lengthCached = -1;

		public override long Length
		{
			get
			{
				try
				{
					if (_lengthCached == -1)
					{
						var ret = AttachAndRun(() =>
						{
							var len = NativeJarFileStream.CallNativeR<long>("GetLength");
							if (len < 0)
							{
								len = 0;
								throw new Exception("GetLength-Failed");
							}

							return len;
						});
						_lengthCached = ret;
					}

					return _lengthCached;
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					return 0;
				}
			}
		}

		public override long Position
		{
			get
			{
				return AttachAndRun(() =>
				{
					var pos = NativeJarFileStream.CallNativeR<long>("GetPosition");
					return pos;
				});
			}
			set { throw new NotImplementedException("Position"); }
		}

		public virtual bool Open(string path)
		{
			if (NativeJarFileStream == null)
			{
				// NativeJarFileStream = NativeHelper.CreateNativeR("com.unity3d.myutils.JarFileStream");
				var jCls = NativeHelper.GetNativeR("com.unity3d.myutils.JarFileStream");
				NativeJarFileStream = NativeHelper.CallNativeR<AndroidJavaObject>(jCls, "Create");
			}

			var ret = NativeJarFileStream.CallNativeR<int>("Open", path);

			// if (NativeJarFileStream == null)
			// {
			//     NativeJarFileStream = new AndroidJavaObject("com.unity3d.myutils.JarFileStream");
			// }
			// var ret = NativeJarFileStream.Call<int>("Open", path);
			return ret == 0;
		}

		protected long SeekNative(long offset, SeekOrigin origin)
		{
			var pos = NativeJarFileStream.CallNativeR<long>("Seek", offset, (int)origin);
			if (origin == SeekOrigin.Begin)
			{
				Debug.Assert(offset == pos, "offset==pos");
			}
			else if (origin == SeekOrigin.End)
			{
				Debug.Assert(offset == _lengthCached - offset, "offset==_lengthCached-offset");
			}
			else
			{
				Debug.Assert(false, "origin not tested");
			}

			return pos;
		}

		protected int ReadNative(byte[] array, int offset, int count)
		{
			var bs = NativeJarFileStream.CallNativeR<byte[]>("Read", offset, count);
			if (bs == null)
			{
				throw new IOException("cannot read from NativeJarFileStream");
			}

			(bs[0], bs[1], bs[2], bs[3]) = (bs[3], bs[2], bs[1], bs[0]);
			var len = BitConverter.ToInt32(bs, 0);
			Array.Copy(bs, 4, array, offset, len);
			return len;
		}

		public override void Close()
		{
			this.AttachAndRun(() =>
			{
				var ret = NativeJarFileStream.CallNativeR<int>("Close");
				if (ret == -1)
				{
					Debug.LogError("Close-failed");
				}
			});

			base.Close();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return AttachAndRun(() =>
			{
				if (offset > this.Length)
				{
					Debug.LogError($"outofbound: {this.Name}");
					return offset;
				}

				if (LockOffset != 0)
				{
					var pos1 = this.SeekNative(offset, origin);
					this.SeekNative(offset + LockOffset, origin);
					return pos1;
				}
				else
				{
					return this.SeekNative(offset + LockOffset, origin);
				}
			});
		}

		public override int Read(byte[] array, int offset, int count)
		{
			return AttachAndRun(() =>
			{
				var pos = this.Position;

				if (pos == this.Length)
				{
					if (count > 0)
					{
						Debug.LogError($"outofbound1: {this.Name}");
					}

					return 0;
				}
				else if (pos > this.Length)
				{
					Debug.LogError($"outofbound2: {this.Name}");
				}

				var len = this.ReadNative(array, offset, count);
				if (LockRead && _encrypter != null)
				{
					try
					{
						_encrypter.Encrypt(array, pos - LockOffset, offset, len);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
						throw;
					}
				}

			#if ENABLE_ENCRYPT_CHECK
			CheckRead(array, offset, count, len, pos);
			#endif

				return len;
			});
		}

		protected override void Dispose(bool disposing)
		{
		#if ENABLE_ENCRYPT_CHECK
			DisposeCheck();
		#endif
			base.Dispose(disposing);

			AttachAndRun(() => { NativeJarFileStream.CallNativeR("Dispose"); });
		}

		#region override

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region encrypt

		protected int LockOffset;

		protected bool LockRead;
		private AssetBundleEncryptor _encrypter;

		public void InitEncrypter(int offset, string key, string fileName)
		{
			LockRead = true;
			if (_encrypter == null && !string.IsNullOrEmpty(key))
			{
			#if ENABLE_ENCRYPT_CHECK
				MakeChecker(fileName);
			#endif
				// base.Seek(0, SeekOrigin.Begin);
				// _encrypter = new AssetBundleEncryptor();
				_encrypter = AssetBundleEncryptor.SharedEncryptor;
				_encrypter.Init(key);
			}

			if (offset > 0) LockOffset = offset;
		}

		public void ProtectReadForever(int offset, string key, string checkerFileName)
		{
			InitEncrypter(offset, key, checkerFileName);
		}

		#endregion
	}
}