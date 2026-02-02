// using System;
// using System.Runtime.InteropServices;
//
// public class WebGLIndexedDB1
// {
// 	[DllImport("__Internal")]
// 	private static extern void indexeddb_init(string dbName, string storeName);
//
// 	[DllImport("__Internal")]
// 	private static extern void indexeddb_write(string key, string value, Action<string, bool> callback);
//
// 	[DllImport("__Internal")]
// 	private static extern void indexeddb_read(string key, Action<string, string, bool> callback);
//
// 	[DllImport("__Internal")]
// 	private static extern void indexeddb_exists(string key, Action<string, bool> callback);
//
// 	[DllImport("__Internal")]
// 	private static extern void indexeddb_delete(string key, Action<string, bool> callback);
//
// 	[DllImport("__Internal")]
// 	private static extern void indexeddb_rename(string oldKey, string newKey, Action<string, string, bool> callback);
//
// 	[DllImport("__Internal")]
// 	private static extern void indexeddb_prefix_cache_keys(string prefix, Action<bool> callback);
//
// 	[DllImport("__Internal")]
// 	private static extern int indexeddb_get_cache_length();
//
// 	[DllImport("__Internal")]
// 	private static extern string indexeddb_get_key_at_index(int index);
//
// 	[DllImport("__Internal")]
// 	private static extern string indexeddb_get_value_at_index(int index);
//
// 	public static void Init(string dbName, string storeName)
// 	{
// 		indexeddb_init(dbName, storeName);
// 	}
//
// 	public static void Write(string key, string value, Action<string, bool> callback)
// 	{
// 		indexeddb_write(key, value, callback);
// 	}
//
// 	public static void Read(string key, Action<string, string, bool> callback)
// 	{
// 		indexeddb_read(key, callback);
// 	}
//
// 	public static void Exists(string key, Action<string, bool> callback)
// 	{
// 		indexeddb_exists(key, callback);
// 	}
//
// 	public static void Delete(string key, Action<string, bool> callback)
// 	{
// 		indexeddb_delete(key, callback);
// 	}
//
// 	public static void Rename(string oldKey, string newKey, Action<string, string, bool> callback)
// 	{
// 		indexeddb_rename(oldKey, newKey, callback);
// 	}
//
// 	public static void PrefixCacheKeys(string prefix, Action<bool> callback)
// 	{
// 		indexeddb_prefix_cache_keys(prefix, callback);
// 	}
//
// 	public static int GetCacheLength()
// 	{
// 		return indexeddb_get_cache_length();
// 	}
//
// 	public static string GetKeyAtIndex(int index)
// 	{
// 		return indexeddb_get_key_at_index(index);
// 	}
//
// 	public static string GetValueAtIndex(int index)
// 	{
// 		return indexeddb_get_value_at_index(index);
// 	}
// }