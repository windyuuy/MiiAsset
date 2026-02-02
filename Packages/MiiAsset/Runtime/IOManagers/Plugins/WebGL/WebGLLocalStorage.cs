// #if SUPPORT_WEBGL_LOCAL_STORAGE
// 	using System;
// 	using System.Collections.Generic;
// 	using System.Runtime.InteropServices;
// 	using MiiAsset.Runtime.Adapter;
// 	using UnityEngine;
//
// 	public static class WebGLLocalStorage
// 	{
// 	#if UNITY_WEBGL && !UNITY_EDITOR
// 	[DllImport("__Internal")]
// 	private static extern void LocalStorage_SetItem(string key, string value);
//
// 	[DllImport("__Internal")]
// 	private static extern IntPtr LocalStorage_GetItem(string key);
//
// 	[DllImport("__Internal")]
// 	private static extern void LocalStorage_RemoveItem(string key);
//
// 	[DllImport("__Internal")]
// 	private static extern void LocalStorage_Clear();
//
// 	[DllImport("__Internal")]
// 	private static extern bool LocalStorage_HasKey(string key);
//
// 	[DllImport("__Internal")]
// 	private static extern void LocalStorage_RefreshKeysCache();
//
// 	[DllImport("__Internal")]
// 	private static extern void LocalStorage_RefreshKeysCacheWithPrefix(string prefix);
//
// 	[DllImport("__Internal")]
// 	private static extern int LocalStorage_GetKeysCacheLength();
//
// 	[DllImport("__Internal")]
// 	private static extern IntPtr LocalStorage_GetKeyByIndex(int index);
//
// 	[DllImport("__Internal")]
// 	private static extern IntPtr LocalStorage_GetValueByIndex(int index);
//
// 	[DllImport("__Internal")]
// 	private static extern bool LocalStorage_RenameKey(string oldKey, string newKey, bool force);
// 	#endif
//
// 		// 缓存本地键值对（仅用于非 WebGL 平台模拟）
// 		private static Dictionary<string, string> localDataCache = new Dictionary<string, string>();
//
// 		/// <summary>
// 		/// 设置键值对
// 		/// </summary>
// 		public static void SetItem(string key, string value)
// 		{
// 		#if UNITY_WEBGL && !UNITY_EDITOR
// 		LocalStorage_SetItem(key, value);
// 		#else
// 			localDataCache[key] = value;
// 		#endif
// 		}
//
// 		/// <summary>
// 		/// 获取键对应的值
// 		/// </summary>
// 		public static string GetItem(string key)
// 		{
// 		#if UNITY_WEBGL && !UNITY_EDITOR
// 		IntPtr ptr = LocalStorage_GetItem(key);
// 		if (ptr != IntPtr.Zero)
// 		{
// 			return Marshal.PtrToStringAnsi(ptr);
// 		}
// 		return null;
// 		#else
// 			if (localDataCache.TryGetValue(key, out var item))
// 			{
// 				return item;
// 			}
// 			else
// 			{
// 				return null;
// 			}
// 		#endif
// 		}
//
// 		/// <summary>
// 		/// 删除指定键
// 		/// </summary>
// 		public static void RemoveItem(string key)
// 		{
// 		#if UNITY_WEBGL && !UNITY_EDITOR
// 		LocalStorage_RemoveItem(key);
// 		#else
// 			localDataCache.Remove(key);
// 		#endif
// 		}
//
// 		/// <summary>
// 		/// 清空所有存储
// 		/// </summary>
// 		public static void Clear()
// 		{
// 		#if UNITY_WEBGL && !UNITY_EDITOR
// 		LocalStorage_Clear();
// 		#else
// 			localDataCache.Clear();
// 		#endif
// 		}
//
// 		/// <summary>
// 		/// 检查键是否存在
// 		/// </summary>
// 		public static bool HasKey(string key)
// 		{
// 		#if UNITY_WEBGL && !UNITY_EDITOR
// 		return LocalStorage_HasKey(key);
// 		#else
// 			return localDataCache.ContainsKey(key);
// 		#endif
// 		}
//
// 		/// <summary>
// 		/// 刷新键缓存（重新加载所有键值对到缓存中）
// 		/// </summary>
// 		public static void RefreshKeysCache()
// 		{
// 		#if UNITY_WEBGL && !UNITY_EDITOR
// 		LocalStorage_RefreshKeysCache();
// 		#endif
// 		}
//
// 		/// <summary>
// 		/// 按前缀刷新键缓存（只加载指定前缀的键值对到缓存中）
// 		/// </summary>
// 		public static void RefreshKeysCacheWithPrefix(string prefix)
// 		{
// 		#if UNITY_WEBGL && !UNITY_EDITOR
// 		LocalStorage_RefreshKeysCacheWithPrefix(prefix);
// 		#endif
// 		}
//
// 		/// <summary>
// 		/// 获取键缓存的长度
// 		/// </summary>
// 		public static int GetKeysCacheLength()
// 		{
// 		#if UNITY_WEBGL && !UNITY_EDITOR
// 		return LocalStorage_GetKeysCacheLength();
// 		#else
// 			return localDataCache.Count;
// 		#endif
// 		}
//
// 		/// <summary>
// 		/// 根据索引获取键
// 		/// </summary>
// 		public static string GetKeyByIndex(int index)
// 		{
// 		#if UNITY_WEBGL && !UNITY_EDITOR
// 		IntPtr ptr = LocalStorage_GetKeyByIndex(index);
// 		if (ptr != IntPtr.Zero)
// 		{
// 			return Marshal.PtrToStringAnsi(ptr);
// 		}
// 		return null;
// 		#else
// 			if (index >= 0 && index < localDataCache.Count)
// 			{
// 				int i = 0;
// 				foreach (var kvp in localDataCache)
// 				{
// 					if (i == index)
// 					{
// 						return kvp.Key;
// 					}
//
// 					i++;
// 				}
// 			}
//
// 			return null;
// 		#endif
// 		}
//
// 		/// <summary>
// 		/// 根据索引获取值
// 		/// </summary>
// 		public static string GetValueByIndex(int index)
// 		{
// 		#if UNITY_WEBGL && !UNITY_EDITOR
// 		IntPtr ptr = LocalStorage_GetValueByIndex(index);
// 		if (ptr != IntPtr.Zero)
// 		{
// 			return Marshal.PtrToStringAnsi(ptr);
// 		}
// 		return null;
// 		#else
// 			if (index >= 0 && index < localDataCache.Count)
// 			{
// 				int i = 0;
// 				foreach (var kvp in localDataCache)
// 				{
// 					if (i == index)
// 					{
// 						return kvp.Value;
// 					}
//
// 					i++;
// 				}
// 			}
//
// 			return null;
// 		#endif
// 		}
//
// 		/// <summary>
// 		/// 获取所有键的列表
// 		/// </summary>
// 		public static void GetAllKeys(List<string> keys)
// 		{
// 			keys.Clear();
// 			int length = GetKeysCacheLength();
// 			for (int i = 0; i < length; i++)
// 			{
// 				keys.Add(GetKeyByIndex(i));
// 			}
// 		}
//
// 		/// <summary>
// 		/// 获取所有值的列表
// 		/// </summary>
// 		public static void GetAllValues(List<string> values)
// 		{
// 			values.Clear();
// 			int length = GetKeysCacheLength();
// 			for (int i = 0; i < length; i++)
// 			{
// 				values.Add(GetValueByIndex(i));
// 			}
// 		}
//
// 		/// <summary>
// 		/// 重命名键
// 		/// </summary>
// 		/// <param name="oldKey">旧键名</param>
// 		/// <param name="newKey">新键名</param>
// 		/// <param name="force">是否强制重命名（覆盖现有key）</param>
// 		/// <returns>如果成功则返回true，否则返回false</returns>
// 		public static bool RenameKey(string oldKey, string newKey, bool force = false)
// 		{
// 		#if UNITY_WEBGL && !UNITY_EDITOR
// 		bool result = LocalStorage_RenameKey(oldKey, newKey, force);
// 		if (!result && !HasKey(newKey))
// 		{
// 			MyLogger.LogError($"Failed to rename key '{oldKey}' to '{newKey}'. New key does not exist.");
// 		}
// 		return result;
// 		#else
// 			if (!localDataCache.ContainsKey(oldKey))
// 			{
// 				MyLogger.LogError($"Failed to rename key '{oldKey}' to '{newKey}'. Old key does not exist.");
// 				return false;
// 			}
//
// 			if (localDataCache.ContainsKey(newKey) && !force)
// 			{
// 				return false;
// 			}
//
// 			string value = localDataCache[oldKey];
// 			localDataCache.Remove(oldKey);
// 			localDataCache[newKey] = value;
// 			return true;
// 		#endif
// 		}
// 	}
// #endif