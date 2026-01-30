using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDK;
using MiiAsset.Runtime.AssetUtils;
using UnityEngine;
using UnityEngine.Networking;
using MiiAsset.Runtime.Adapter;
using MonoExtLib.AsyncExt;

#if UNITY_WEBGL && SUPPORT_WDK
using Lang.Encoding;
using MiiAsset.Runtime.Utils;

namespace MiiAsset.Runtime.IOManagers
{
	public static class WDKExt
	{
		public static string GetExceptionDesc(this ReadFileResult resp, string desc)
		{
			return $"file-error: errCode: {resp.errCode}, errMsg: {resp.errMsg}, {desc}";
		}

		public static string GetExceptionDesc(this BaseResponse resp, string desc)
		{
			return $"file-error: errCode: {resp.ErrCode}, errMsg: {resp.ErrMsg}, {desc}";
		}
	}

	public sealed class WDKIOProto : IIOProto
	{
		public string CacheDir { get; set; }
		public string InternalDir { get; set; }
		public string ExternalDir { get; set; }
		public string CatalogName { get; set; }

		/// <summary>
		/// 秒
		/// </summary>
		public int Timeout { get; set; }

		public bool IsInternalDirUpdating => true;
		public static string StreamingCacheAssetPath;
		public static string StreamingRemoteAssetPath;

		protected IFileSystemManager FileSystemManager;

		public async Task<bool> Init(IIOProtoInitOptions options)
		{
			if (!SDKManager.Instance.IsInited())
			{
				MyLogger.Log($"MiiAsset.InitSDK");
				var code = await SDKManager.Instance.Init();

				MyLogger.Log($"MiiAsset.InitSDK return code: {code}");
				var ret = await InitInternal(options);
				return ret;
			}
			else
			{
				return await InitInternal(options);
			}
		}

		protected Task<bool> InitInternal(IIOProtoInitOptions options)
		{
			StreamingCacheAssetPath = $"{UserAPI.Instance.GameInfo.UserDataPath}/__GAME_FILE_CACHE/StreamingAssets/";

		#if UNITY_EDITOR
			this.InternalDir = AssetHelper.GetInternalBuildPath();
		#else
			this.InternalDir = $"{StreamingCacheAssetPath}{options.InternalBaseUri}";
		#endif
			var persistentDataPath = UserAPI.Instance.GameInfo.UserDataPath;
			this.CacheDir = $"{persistentDataPath}/{options.BundleCacheDir}";
			this.ExternalDir = $"{persistentDataPath}/{options.ExternalBaseUri}";
			StreamingRemoteAssetPath = $"{Application.streamingAssetsPath}/{options.InternalBaseUri}";
			this.CatalogName = options.CatalogName;
			this.Timeout = options.Timeout;

			MyLogger.Log(
				$"iopaths: {this.InternalDir}, {this.CacheDir}, {this.ExternalDir}, {StreamingRemoteAssetPath}");

			FileSystemManager = UserAPI.Instance.FileSystem.GetFileSystemManager();

			// var catalogHashName = this.CatalogName
			// 	.Replace(".json", ".hash")
			// 	.Replace(".zip", ".hash");
			// var results = await Task.WhenAll(
			// 	EnsureStreamingAssets(catalogHashName),
			// 	EnsureStreamingAssets(CatalogName)
			// );
			//
			// var isOk = results.All(r => r);
			// return isOk;

			var ret = EnsurePersistDirs();

			return Task.FromResult(ret);
		}

		private bool EnsurePersistDirs()
		{
			try
			{
				EnsureDirectory(this.CacheDir);
				EnsureDirectory(this.ExternalDir);
				return true;
			}
			catch (Exception exception)
			{
				MyLogger.LogException(exception, "e13");
				return false;
			}
		}

		public bool Exists(string uri)
		{
			return FileSystemManager.AccessSync(uri).Exist;
		}

		public bool ExistsDir(string dir)
		{
			return Exists(dir);
		}

		public void EnsureDirectory(string dir)
		{
			if (!ExistsDir(dir))
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
			FileSystemManager.WriteFileText(new()
			{
				success = (resp) => { JsDelay.DelayExec(() => ts.SetResult(true)); },
				fail = (resp) => { JsDelay.DelayExec(() => ts.SetResult(false)); },
				filePath = cacheUri,
				data = text,
				encoding = "utf-8",
			});
			return ts.Task;
		}

		public Stream OpenRead(string uri)
		{
			var content = FileSystemManager.ReadFileBytesSync(uri);
			var memoryStream = new MemoryStream(content);
			return memoryStream;
		}

		public Stream OpenWrite(string filePath)
		{
			var writeFileStream = new WDKWriteFileStream(FileSystemManager, filePath);
			return writeFileStream;
		}

		public Task<string> ReadAllTextAsync(string uri, Encoding encoding)
		{
			Debug.Assert(Equals(encoding, Encoding.UTF8) || Equals(encoding, EncodingExt.UTF8WithoutBom));

			var ts = new TaskCompletionSource<string>();
			try
			{
				FileSystemManager.ReadFileAllText(new()
				{
					success = (resp) =>
					{
						var data = resp.stringData;
						JsDelay.DelayExec(() => ts.SetResult(data));
					},
					fail = (resp) =>
					{
						var exception = new IOException(resp.GetExceptionDesc($"read-file-failed: {uri}"));
						MyLogger.LogException(exception, "e14");
						JsDelay.DelayExec(() => ts.SetException(exception));
					},
					filePath = uri,
					encoding = "utf-8",
				});
			}
			catch (Exception exception)
			{
				ts.SetException(exception);
			}

			return ts.Task;
		}

		public void Move(string from, string uri)
		{
			FileSystemManager.RenameSync(from, uri);
		}

		protected Dictionary<string, bool> BundleExistMap;
		protected bool IsInitedBundleExistMap = false;

		protected void InitBundleExistMap()
		{
			if (!IsInitedBundleExistMap)
			{
				IsInitedBundleExistMap = true;
				BundleExistMap = new();
				var files1 = FileSystemManager.ReaddirSync(CacheDir);
				var files2 = ExistsDir(InternalDir)
					? FileSystemManager.ReaddirSync(InternalDir)
					: Array.Empty<string>();
				foreach (var file in files1.Concat(files2))
				{
					var fileName = Path.GetFileName(file);
					BundleExistMap.Add(fileName, true);
				}
			}
		}

		public bool ExistsBundle(string bundleFileName)
		{
			InitBundleExistMap();
			return BundleExistMap.ContainsKey(bundleFileName);
		}

		public bool EnsureBundle(string bundleFileName)
		{
			InitBundleExistMap();
			// MyLogger.Log($"EnsureBundle: {bundleName}, {BundleExistMap.Count}");
			if (BundleExistMap.TryGetValue(bundleFileName, out var exist))
			{
				return exist;
			}
			else
			{
				var exists = Exists(CacheDir + bundleFileName) || Exists(InternalDir + bundleFileName);
				if (exists)
				{
					BundleExistMap.Add(bundleFileName, true);
				}

				return false;
			}
		}

		public void Delete(string filePath)
		{
			if (Exists(filePath))
			{
				FileSystemManager.UnlinkSync(filePath);
			}
			else
			{
				MyLogger.LogError($"file not exist: {filePath}");
			}
		}

		public FilePathInfo[] ReadDir(string readDir)
		{
			if (!readDir.EndsWith("/"))
			{
				readDir += "/";
			}

			var files = FileSystemManager
				.ReaddirSync(readDir)
				.Select(fileName => new FilePathInfo(null, fileName, readDir))
				.ToArray();
			return files;
		}

		public Task<T> ReadAllBytesAsync<T>(string uri, Func<byte[], T> handler)
		{
			var ts = new TaskCompletionSource<T>();
			FileSystemManager.ReadFileBytes(new()
			{
				success = (resp) =>
				{
					var result = handler(resp.binData);
					JsDelay.DelayExec(() => ts.SetResult(result));
				},
				fail = (resp) =>
				{
					var exception = new IOException(resp.GetExceptionDesc("read-file-failed"));
					MyLogger.LogException(exception, "e15");
					JsDelay.DelayExec(() => ts.SetException(exception));
				},
				filePath = uri,
			});
			return ts.Task;
		}

		public Task WriteAllBytesAsync(string uri, byte[] bytes)
		{
			var ts = new TaskCompletionSource<bool>();
			FileSystemManager.WriteFileBytes(new()
			{
				success = (resp) => { JsDelay.DelayExec(() => ts.SetResult(true)); },
				fail = (resp) =>
				{
					var exception = new IOException(resp.GetExceptionDesc("write-file-failed"));
					MyLogger.LogException(exception, "e16");
					JsDelay.DelayExec(() => ts.SetException(exception));
				},
				data = bytes,
				filePath = uri,
			});
			return ts.Task;
		}

		public bool IsWebUri(string uri)
		{
			return (!uri.StartsWith(UserAPI.Instance.GameInfo.UserDataPath)) && uri.Contains("://");
		}

		public Task<string> ReadCatalog(string uri)
		{
			var text = FileSystemManager.ReadCompressedFileTextSync(new WDK.ReadCompressedFileSyncOption
			{
				filePath = uri,
			});
			return Task.FromResult(text);
		}

		private async Task<bool> EnsureStreamingAssets(string fileName)
		{
			var uri2 = $"{StreamingRemoteAssetPath}{fileName}";
			MyLogger.Log($"EnsureStreamingAssets: {uri2}");
			var uwr = UnityWebRequest.Get(uri2);
			var op = uwr.SendWebRequest();
			await op.GetTask();
			var isOk = uwr.IsUwrOk();
			if (!isOk)
			{
				MyLogger.Log($"EnsureStreamingAssets-failed: {uri2}, {(int)uwr.responseCode}, {uwr.error}");
			}

			uwr.Dispose();
			uwr = null;

			var maxTimes = 100;
			// 有可能还是旧的，但是不是新的没关系, 在就行
			await UniAsyncUtils.WaitUntil(() =>
			{
				if (--maxTimes <= 0)
				{
					return true;
				}

				return Exists(InternalDir + fileName);
			});

			return isOk;
		}

		public async Task<EnsureStreamingBundlesResult> EnsureStreamingBundles(string bundleFileName)
		{
			var existsBundle = ExistsBundle(bundleFileName);
			if (!existsBundle)
			{
				var uri2 = $"{StreamingRemoteAssetPath}{bundleFileName}";
				MyLogger.Log($"EnsureStreamingBundles: {uri2}");
				var uwr = UnityWebRequest.Get(uri2);
				var op = uwr.SendWebRequest();
				await op.GetTask();
				var isOk = uwr.IsUwrOk();
				var uwrResponseCode = uwr.responseCode;
				var uwrError = uwr.error;
				uwr.Dispose();
				uwr = null;
				if (!isOk)
				{
					MyLogger.Log($"EnsureStreamingBundles-failed: {uri2}, {(int)uwrResponseCode}, {uwrError}");
				}
				else
				{
					var maxTimes = 100;
					// 有可能还是旧的，但是不是新的没关系, 在就行
					await UniAsyncUtils.WaitUntil(() =>
					{
						if (--maxTimes <= 0)
						{
							return true;
						}

						return EnsureBundle(bundleFileName);
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
			IOProtoBase.SetUwrStatic(uwr, CertificateHandler, Timeout);
		}

		public Task<bool> CleanAllFileCaches()
		{
			var ts = new TaskCompletionSource<bool>();
			try
			{
				MyLogger.Log("clean MiiAsset begin");
				MyLogger.Log("read ExternalDir");
				var externalFiles = Array.Empty<string>();
				try
				{
					externalFiles = FileSystemManager.ReaddirSync(this.ExternalDir);
				}
				catch (Exception exception3)
				{
					MyLogger.LogException(exception3, "e17");
					MyLogger.LogError($"ReaddirSync failed: {this.ExternalDir}");
				}

				MyLogger.Log("read CacheDir");
				var cacheFiles = Array.Empty<string>();
				try
				{
					cacheFiles = FileSystemManager.ReaddirSync(this.CacheDir);
				}
				catch (Exception exception4)
				{
					MyLogger.LogException(exception4, "e18");
					MyLogger.LogError($"ReaddirSync failed: {this.CacheDir}");
				}

				void CleanFiles(string dir, string[] files)
				{
					foreach (var file in files)
					{
						try
						{
							var path = $"{dir}{file}";
							MyLogger.Log($"delete file: {path}");
							FileSystemManager.UnlinkSync(path);
						}
						catch (Exception exception2)
						{
							MyLogger.LogException(exception2, "e20");
							MyLogger.LogError($"delete file failed: {file}");
						}
					}
				}

				MyLogger.Log("clean ExternalDir");
				CleanFiles(this.ExternalDir, externalFiles);
				MyLogger.Log("clean CacheDir");
				CleanFiles(this.CacheDir, cacheFiles);
				MyLogger.Log("clean MiiAsset done");
			}
			catch (Exception exception1)
			{
				MyLogger.LogError("read dirs failed");
				MyLogger.LogException(exception1, "e20");
			}

			try
			{
				UserAPI.Instance.FileSystem.GetFileSystemManager()
					.CleanAllFileCache((ret) => { ts.TrySetResult(ret); });
			}
			catch (Exception exception)
			{
				MyLogger.LogException(exception, "e20");
				ts.TrySetResult(false);
			}

			return ts.Task;
		}
	}
}
#endif