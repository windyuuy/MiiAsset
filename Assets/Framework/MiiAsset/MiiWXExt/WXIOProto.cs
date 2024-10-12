using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.AssetUtils;
using Lang.Encoding;
using UnityEngine;
using UnityEngine.Networking;
using WeChatWASM;

namespace Framework.MiiAsset.Runtime.IOManagers
{
	public static class WXExt
	{
		public static string GetExceptionDesc(this FileError resp, string desc)
		{
			return $"file-error: errCode: {resp.errCode}, errMsg: {resp.errMsg}, {desc}";
		}

		public static string GetExceptionDesc(this WXTextResponse resp, string desc)
		{
			return $"file-error: errCode: {resp.errCode}, errMsg: {resp.errMsg}, {desc}";
		}
	}

	public class WXIOProto : IIOProto
	{
		public string CacheDir { get; set; }
		public string InternalDir { get; set; }
		public string ExternalDir { get; set; }
		public string CatalogName { get; set; }
		public bool IsInternalDirUpdating => true;
		public static string StreamingCacheAssetPath = $"{WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE/StreamingAssets/";
		public static string StreamingRemoteAssetPath;

		protected WXFileSystemManager FileSystemManager;

		public async Task<bool> Init(IIOProtoInitOptions options)
		{
#if UNITY_EDITOR
			this.InternalDir = AssetHelper.GetInternalBuildPath();
#else
			this.InternalDir = $"{StreamingCacheAssetPath}{options.InternalBaseUri}";
#endif
			var persistentDataPath = WX.env.USER_DATA_PATH;
			this.CacheDir = $"{persistentDataPath}/{options.BundleCacheDir}";
			this.ExternalDir = $"{persistentDataPath}/{options.ExternalBaseUri}";
			StreamingRemoteAssetPath = $"{Application.streamingAssetsPath}/{options.InternalBaseUri}";
			this.CatalogName = options.CatalogName;

			Debug.Log($"iopaths: {this.InternalDir}, {this.CacheDir}, {this.ExternalDir}, {StreamingRemoteAssetPath}");

			FileSystemManager = WX.GetFileSystemManager();
			var results = await Task.WhenAll(
				EnsureStreamingAssets("catalog.hash"),
				EnsureStreamingAssets(CatalogName)
			);

			var isOk = results.All(r => r);
			return isOk;
		}

		public bool Exists(string uri)
		{
			return FileSystemManager.AccessSync(uri) == "access:ok";
		}

		public void EnsureDirectory(string dir)
		{
			if (!Exists(dir))
			{
				FileSystemManager.MkdirSync(dir, true);
			}
		}

		public void EnsureFileDirectory(string uri)
		{
			var pos = uri.LastIndexOf('/');
			var dir = uri.Substring(0, pos);
			EnsureDirectory(dir);
		}

		public Task WriteAllTextAsync(string cacheUri, string text, Encoding encoding)
		{
			Debug.Assert(Equals(encoding, Encoding.UTF8) || Equals(encoding, EncodingExt.UTF8WithoutBom));
			var ts = new TaskCompletionSource<bool>();
			FileSystemManager.WriteFile(new WriteFileStringParam
			{
				success = (resp) => { ts.SetResult(true); },
				fail = (resp) => { ts.SetResult(false); },
				filePath = cacheUri,
				data = text,
				encoding = "utf-8",
			});
			return ts.Task;
		}

		public Stream OpenRead(string uri)
		{
			var content = FileSystemManager.ReadFileSync(uri);
			var memoryStream = new MemoryStream(content);
			return memoryStream;
		}

		public Stream OpenWrite(string filePath)
		{
			var writeFileStream = new WXWriteFileStream(FileSystemManager, filePath);
			return writeFileStream;
		}

		public Task<string> ReadAllTextAsync(string uri, Encoding encoding)
		{
			Debug.Assert(Equals(encoding, Encoding.UTF8) || Equals(encoding, EncodingExt.UTF8WithoutBom));

			var ts = new TaskCompletionSource<string>();
			FileSystemManager.ReadFile(new ReadFileParam
			{
				success = (resp) =>
				{
					var data = resp.stringData;
					ts.SetResult(data);
				},
				fail = (resp) =>
				{
					var exception = new IOException(resp.GetExceptionDesc("read-file-failed"));
					ts.SetException(exception);
				},
				filePath = uri,
				encoding = "utf-8",
			});
			return ts.Task;
		}

		public void Move(string from, string uri)
		{
			FileSystemManager.RenameSync(from, uri);
		}

		protected Dictionary<string, bool> BundleExistMap;

		protected void InitBundleExistMap()
		{
			if (BundleExistMap == null)
			{
				BundleExistMap = new();
				var files1 = FileSystemManager.ReaddirSync(CacheDir);
				var files2 = FileSystemManager.ReaddirSync(InternalDir);
				foreach (var file in files1.Concat(files2))
				{
					var fileName = Path.GetFileName(file);
					BundleExistMap.Add(fileName, true);
				}
			}
		}

		public bool ExistsBundle(string bundleName)
		{
			InitBundleExistMap();
			return BundleExistMap.ContainsKey(bundleName);
		}

		public bool EnsureBundle(string bundleName)
		{
			if (BundleExistMap.TryGetValue(bundleName, out var exist))
			{
				return exist;
			}
			else
			{
				var exists = Exists(CacheDir + bundleName) || Exists(InternalDir + bundleName);
				if (exists)
				{
					BundleExistMap.Add(bundleName, true);
				}

				return exist;
			}
		}

		public void Delete(string filePath)
		{
			FileSystemManager.UnlinkSync(filePath);
		}

		public string[] ReadDir(string readDir)
		{
			var files = FileSystemManager.ReaddirSync(readDir);
			return files;
		}

		public Task<byte[]> ReadAllBytesAsync(string uri)
		{
			var ts = new TaskCompletionSource<byte[]>();
			FileSystemManager.ReadFile(new()
			{
				success = (resp) => { ts.SetResult(resp.binData); },
				fail = (resp) =>
				{
					var exception = new IOException(resp.GetExceptionDesc("read-file-failed"));
					ts.SetException(exception);
				},
				filePath = uri,
			});
			return ts.Task;
		}

		public Task WriteAllBytesAsync(string uri, byte[] bytes)
		{
			var ts = new TaskCompletionSource<bool>();
			FileSystemManager.WriteFile(new WriteFileParam
			{
				success = (resp) => { ts.SetResult(true); },
				fail = (resp) =>
				{
					var exception = new IOException(resp.GetExceptionDesc("write-file-failed"));
					ts.SetException(exception);
				},
				filePath = uri,
			});
			return ts.Task;
		}

		public bool IsWebUri(string uri)
		{
			return !uri.StartsWith(WX.env.USER_DATA_PATH) && uri.Contains("://");
		}

		public Task<string> ReadCatalog(string uri)
		{
			var entry = "catalog.json";
			var ts = new TaskCompletionSource<string>();
			FileSystemManager.ReadZipEntry(new ReadZipEntryOptionString()
			{
				success = (resp) => { ts.SetResult(resp.entries[entry].data); },
				fail = (resp) => { ts.SetException(new IOException(resp.GetExceptionDesc("read catalog failed"))); },
				entries = "all",
				filePath = uri,
				encoding = "utf-8",
			});
			return ts.Task;
		}

		private async Task<bool> EnsureStreamingAssets(string fileName)
		{
			var uri2 = $"{StreamingRemoteAssetPath}{fileName}";
			Debug.Log($"EnsureStreamingAssets: {uri2}");
			var uwr = UnityWebRequest.Get(uri2);
			var op = uwr.SendWebRequest();
			await op.GetTask();
			var isOk = uwr.result == UnityWebRequest.Result.Success;
			if (!isOk)
			{
				Debug.Log($"EnsureStreamingAssets-failed: {uri2}, {uwr.responseCode}, {uwr.error}");
			}

			var maxTimes = 100;
			// 有可能还是旧的，但是不是新的没关系, 在就行
			await AsyncUtils.WaitUntil(() =>
			{
				if (--maxTimes <= 0)
				{
					return true;
				}

				return Exists(InternalDir + fileName);
			});

			return isOk;
		}

		public async Task<EnsureStreamingBundlesResult> EnsureStreamingBundles(string bundleName)
		{
			var existsBundle = ExistsBundle(bundleName);
			if (!existsBundle)
			{
				var uri2 = $"{StreamingRemoteAssetPath}{bundleName}";
				Debug.Log($"EnsureStreamingBundles: {uri2}");
				var uwr = UnityWebRequest.Get(uri2);
				var op = uwr.SendWebRequest();
				await op.GetTask();
				var isOk = uwr.result == UnityWebRequest.Result.Success;
				if (!isOk)
				{
					Debug.Log($"EnsureStreamingBundles-failed: {uri2}, {uwr.responseCode}, {uwr.error}");
				}
				else
				{
					var maxTimes = 100;
					// 有可能还是旧的，但是不是新的没关系, 在就行
					await AsyncUtils.WaitUntil(() =>
					{
						if (--maxTimes <= 0)
						{
							return true;
						}

						return EnsureBundle(bundleName);
						;
					});
				}

				return isOk ? EnsureStreamingBundlesResult.Downloaded : EnsureStreamingBundlesResult.Failed;
			}
			else
			{
				return EnsureStreamingBundlesResult.Exist;
			}
		}

		protected CertificateHandler CertificateHandler;

		public void RegisterCertificateHandler(CertificateHandler certificateHandler)
		{
			CertificateHandler = certificateHandler;
		}

		public void SetUwr(UnityWebRequest uwr)
		{
			Debug.Log($"request-begin: {uwr.url}");

			if (CertificateHandler != null)
			{
				uwr.certificateHandler = CertificateHandler;
				uwr.disposeCertificateHandlerOnDispose = false;
			}
		}
	}
}