#if SUPPORT_WEBGL_LOCAL_STORAGE
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace WebGLIndexedDB
{
	public class IndexedDB
	{
		// 写入key值(回调形式)
		[DllImport("__Internal")]
		private static extern void IndexedDB_WriteKey(IntPtr key, IntPtr value, IntPtr callback);

		// 读取key值(回调形式)
		[DllImport("__Internal")]
		private static extern void IndexedDB_ReadKey(IntPtr key, IntPtr callback);

		// 写入key值(回调形式)(二进制数据)
		[DllImport("__Internal")]
		private static extern void IndexedDB_WriteKeyBinary(IntPtr key, IntPtr data, int length, IntPtr callback);

		// 读取key值(回调形式)(二进制数据)
		[DllImport("__Internal")]
		private static extern void IndexedDB_ReadKeyBinary(IntPtr key, IntPtr callback);

		// 是否存在key(回调形式)
		[DllImport("__Internal")]
		private static extern void IndexedDB_ExistsKey(IntPtr key, IntPtr callback);

		// 删除key(回调形式)
		[DllImport("__Internal")]
		private static extern void IndexedDB_DeleteKey(IntPtr key, IntPtr callback);

		// 重命名key(回调形式)
		[DllImport("__Internal")]
		private static extern void IndexedDB_RenameKey(IntPtr oldKey, IntPtr newKey, IntPtr callback);

		// 按key前缀重新生成key列表缓存(回调形式)
		[DllImport("__Internal")]
		private static extern void IndexedDB_RegenerateKeyListCache(IntPtr prefix, IntPtr callback);

		// 获取key列表缓存长度(同步形式)
		[DllImport("__Internal")]
		private static extern int IndexedDB_GetKeyListCacheLength();

		// 按索引从缓存获取列表项key(同步形式)
		[DllImport("__Internal")]
		private static extern IntPtr IndexedDB_GetKeyFromCacheByIndex(int index);

		// 按索引从缓存获取列表项值(同步形式)
		[DllImport("__Internal")]
		private static extern IntPtr IndexedDB_GetValueFromCacheByIndex(int index);

		// 释放从JavaScript返回的字符串指针
		[DllImport("__Internal")]
		private static extern void free(IntPtr ptr);

		// 写入key值(回调形式)
		public static void WriteKey(string key, string value, Action<bool, string> callback)
		{
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				callback(false, null);
				return;
			}

			IntPtr keyPtr = Marshal.StringToHGlobalAnsi(key);
			IntPtr valuePtr = Marshal.StringToHGlobalAnsi(value);
			IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(new Action<int, IntPtr>((result, errPtr) =>
			{
				Marshal.FreeHGlobal(keyPtr);
				Marshal.FreeHGlobal(valuePtr);
				string error = null;
				if (errPtr != IntPtr.Zero)
				{
					error = Marshal.PtrToStringAnsi(errPtr);
					free(errPtr);
				}

				callback(result == 1, error);
			}));

			IndexedDB_WriteKey(keyPtr, valuePtr, callbackPtr);
		}

		// 读取key值(回调形式)
		public static void ReadKey(string key, Action<bool, string, string> callback)
		{
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				callback(false, null, null);
				return;
			}

			IntPtr keyPtr = Marshal.StringToHGlobalAnsi(key);
			IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(
				new Action<int, IntPtr, IntPtr>((result, valuePtr, errPtr) =>
				{
					Marshal.FreeHGlobal(keyPtr);
					string error = null;
					if (errPtr != IntPtr.Zero)
					{
						error = Marshal.PtrToStringAnsi(errPtr);
						free(errPtr);
					}

					if (result == 1 && valuePtr != IntPtr.Zero)
					{
						string value = Marshal.PtrToStringAnsi(valuePtr);
						free(valuePtr);
						callback(true, value, error);
					}
					else
					{
						callback(false, null, error);
					}
				}));

			IndexedDB_ReadKey(keyPtr, callbackPtr);
		}

		// 写入key值(回调形式)(二进制数据)
		public static void WriteKeyBinary(string key, byte[] data, Action<bool, string> callback)
		{
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				callback(false, null);
				return;
			}

			IntPtr keyPtr = Marshal.StringToHGlobalAnsi(key);
			IntPtr dataPtr = Marshal.AllocHGlobal(data.Length);
			Marshal.Copy(data, 0, dataPtr, data.Length);
			IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(new Action<int, IntPtr>((result, errPtr) =>
			{
				Marshal.FreeHGlobal(keyPtr);
				Marshal.FreeHGlobal(dataPtr);
				string error = null;
				if (errPtr != IntPtr.Zero)
				{
					error = Marshal.PtrToStringAnsi(errPtr);
					free(errPtr);
				}

				callback(result == 1, error);
			}));

			IndexedDB_WriteKeyBinary(keyPtr, dataPtr, data.Length, callbackPtr);
		}

		// 读取key值(回调形式)(二进制数据)
		public static void ReadKeyBinary(string key, Action<bool, byte[], string> callback)
		{
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				callback(false, null, null);
				return;
			}

			IntPtr keyPtr = Marshal.StringToHGlobalAnsi(key);
			IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(
				new Action<int, IntPtr, int, IntPtr>((result, dataPtr, length, errPtr) =>
				{
					Marshal.FreeHGlobal(keyPtr);
					string error = null;
					if (errPtr != IntPtr.Zero)
					{
						error = Marshal.PtrToStringAnsi(errPtr);
						free(errPtr);
					}

					if (result == 1 && dataPtr != IntPtr.Zero && length > 0)
					{
						byte[] data = new byte[length];
						Marshal.Copy(dataPtr, data, 0, length);
						free(dataPtr);
						callback(true, data, error);
					}
					else
					{
						callback(false, null, error);
					}
				}));

			IndexedDB_ReadKeyBinary(keyPtr, callbackPtr);
		}

		// 是否存在key(回调形式)
		public static void ExistsKey(string key, Action<bool, string> callback)
		{
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				callback(false, null);
				return;
			}

			IntPtr keyPtr = Marshal.StringToHGlobalAnsi(key);
			IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(new Action<int, IntPtr>((result, errPtr) =>
			{
				Marshal.FreeHGlobal(keyPtr);
				string error = null;
				if (errPtr != IntPtr.Zero)
				{
					error = Marshal.PtrToStringAnsi(errPtr);
					free(errPtr);
				}

				callback(result == 1, error);
			}));

			IndexedDB_ExistsKey(keyPtr, callbackPtr);
		}

		// 删除key(回调形式)
		public static void DeleteKey(string key, Action<bool, string> callback)
		{
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				callback(false, null);
				return;
			}

			IntPtr keyPtr = Marshal.StringToHGlobalAnsi(key);
			IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(new Action<int, IntPtr>((result, errPtr) =>
			{
				Marshal.FreeHGlobal(keyPtr);
				string error = null;
				if (errPtr != IntPtr.Zero)
				{
					error = Marshal.PtrToStringAnsi(errPtr);
					free(errPtr);
				}

				callback(result == 1, error);
			}));

			IndexedDB_DeleteKey(keyPtr, callbackPtr);
		}

		// 重命名key(回调形式)
		public static void RenameKey(string oldKey, string newKey, Action<bool, string> callback)
		{
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				callback(false, null);
				return;
			}

			IntPtr oldKeyPtr = Marshal.StringToHGlobalAnsi(oldKey);
			IntPtr newKeyPtr = Marshal.StringToHGlobalAnsi(newKey);
			IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(new Action<int, IntPtr>((result, errPtr) =>
			{
				Marshal.FreeHGlobal(oldKeyPtr);
				Marshal.FreeHGlobal(newKeyPtr);
				string error = null;
				if (errPtr != IntPtr.Zero)
				{
					error = Marshal.PtrToStringAnsi(errPtr);
					free(errPtr);
				}

				callback(result == 1, error);
			}));

			IndexedDB_RenameKey(oldKeyPtr, newKeyPtr, callbackPtr);
		}

		// 按key前缀重新生成key列表缓存(回调形式)
		public static void RegenerateKeyListCache(string prefix, Action<bool, string> callback)
		{
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				callback(false, null);
				return;
			}

			IntPtr prefixPtr = Marshal.StringToHGlobalAnsi(prefix);
			IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(new Action<int, IntPtr>((result, errPtr) =>
			{
				Marshal.FreeHGlobal(prefixPtr);
				string error = null;
				if (errPtr != IntPtr.Zero)
				{
					error = Marshal.PtrToStringAnsi(errPtr);
					free(errPtr);
				}

				callback(result == 1, error);
			}));

			IndexedDB_RegenerateKeyListCache(prefixPtr, callbackPtr);
		}

		// 获取key列表缓存长度(同步形式)
		public static int GetKeyListCacheLength()
		{
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				return 0;
			}

			return IndexedDB_GetKeyListCacheLength();
		}

		// 按索引从缓存获取列表项key(同步形式)
		public static string GetKeyFromCacheByIndex(int index)
		{
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				return null;
			}

			IntPtr keyPtr = IndexedDB_GetKeyFromCacheByIndex(index);
			if (keyPtr == IntPtr.Zero)
			{
				return null;
			}

			string key = Marshal.PtrToStringAnsi(keyPtr);
			free(keyPtr);
			return key;
		}

		// 按索引从缓存获取列表项值(同步形式)
		public static string GetValueFromCacheByIndex(int index)
		{
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				return null;
			}

			IntPtr valuePtr = IndexedDB_GetValueFromCacheByIndex(index);
			if (valuePtr == IntPtr.Zero)
			{
				return null;
			}

			string value = Marshal.PtrToStringAnsi(valuePtr);
			free(valuePtr);
			return value;
		}
	}
}
#endif
