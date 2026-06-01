using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.AssetUtils;
using MonoExtLib.AsyncExt;
using UnityEngine;
using UnityEngine.Networking;

namespace MiiAsset.Runtime.IOManagers
{
	public abstract class IOProtoBase : IIOProto
	{
		public virtual string CacheDir { get; protected set; }
		public virtual string InternalDir { get; protected set; }
		public virtual string ExternalDir { get; protected set; }
		public virtual string CatalogName { get; protected set; }

		public virtual bool IsInternalDirUpdating => false;
		public virtual bool IsInternalAssetsValid => true;
		public virtual string StreamingRemoteAssetPath { get; set; }

		/// <summary>
		/// 秒
		/// </summary>
		public virtual int Timeout { get; protected set; }

		public virtual Task<bool> Init(IIOProtoInitOptions options)
		{
			var persistentDataPath = Application.persistentDataPath;
			var streamingAssetsCachePath = Application.streamingAssetsPath;
			var streamingAssetsPath = Application.streamingAssetsPath;

			return this.SetupDirs(options, persistentDataPath, streamingAssetsCachePath, streamingAssetsPath);
		}

		protected virtual Task<bool> SetupDirs(IIOProtoInitOptions options, string persistentDataPath,
			string streamingAssetsCachePath, string streamingAssetsPath)
		{
		#if UNITY_EDITOR
			this.InternalDir = AssetHelper.GetInternalBuildPath();
		#else
			this.InternalDir = $"{streamingAssetsCachePath}/{options.InternalBaseUri}";
		#endif

			this.CacheDir = $"{persistentDataPath}/{options.BundleCacheDir}";
			this.ExternalDir = $"{persistentDataPath}/{options.ExternalBaseUri}";
			StreamingRemoteAssetPath = $"{streamingAssetsPath}/{options.InternalBaseUri}";

			MyLogger.Log(
				$"iopaths: internal={this.InternalDir}, cache={this.CacheDir}, external={this.ExternalDir}, streaming={StreamingRemoteAssetPath}");

			this.CatalogName = options.CatalogName;
			this.Timeout = options.Timeout;

			var ret = EnsurePersistDirs();

			return Task.FromResult(ret);
		}

		protected virtual bool EnsurePersistDirs()
		{
			try
			{
				EnsureDirectory(this.CacheDir);
				EnsureDirectory(this.ExternalDir);
				return true;
			}
			catch (Exception exception)
			{
				MyLogger.LogException(exception, "e29");
				return false;
			}
		}

		public abstract bool Exists(string uri);

		public virtual Task<bool> ExistsAsync(string uri)
		{
			return Task.FromResult(Exists(uri));
		}

		public abstract bool ExistsDir(string dir);
		public abstract void EnsureDirectory(string dir);

		public virtual void EnsureFileDirectory(string uri)
		{
			var dir = Path.GetDirectoryName(uri);
			EnsureDirectory(dir);
		}

		public abstract Task WriteAllTextAsync(string cacheUri, string text, Encoding encoding);
		public abstract Stream OpenRead(string uri);
		public abstract Stream OpenWrite(string filePath);
		public abstract Task<string> ReadAllTextAsync(string uri, Encoding encoding);
		public abstract void Move(string from, string uri);
		public abstract bool ExistsBundle(string bundleFileName);
		public abstract bool EnsureBundle(string bundleFileName);
		public abstract void Delete(string filePath);

		public virtual Task DeleteAsync(string filePath)
		{
			Delete(filePath);
			return Task.CompletedTask;
		}

		public abstract FilePathInfo[] ReadDir(string readDir);
		public abstract Task<T> ReadAllBytesAsync<T>(string uri, Func<byte[], T> handler);
		public abstract Task WriteAllBytesAsync(string uri, byte[] bytes);

		public virtual bool IsWebUri(string uri)
		{
			return uri.Contains("://");
		}

		protected CertificateHandler CertificateHandler;

		public virtual void RegisterCertificateHandler(CertificateHandler certificateHandler)
		{
			CertificateHandler = certificateHandler;
		}

		public virtual void SetUwr(UnityWebRequest uwr)
		{
			SetUwrStatic(uwr, CertificateHandler, Timeout);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetUwrStatic(UnityWebRequest uwr, CertificateHandler certificateHandler, int timeout)
		{
			MyLogger.Log($"request-begin: {uwr.url}, {timeout}");

			if (certificateHandler != null)
			{
				uwr.certificateHandler = certificateHandler;
				uwr.disposeCertificateHandlerOnDispose = false;
				uwr.timeout = timeout;
			}

			// MyLogger.Log("Access-Control-Allow-Origin: *");
			uwr.SetRequestHeader("Access-Control-Allow-Origin", "*");
			// uwr.SetRequestHeader("Access-Control-Allow-Origin", "http://127.0.0.1:8080");
		}

		public abstract Task<string> ReadCatalog(string uri);
		public abstract Task<EnsureStreamingBundlesResult> EnsureStreamingBundles(string bundleFileName);

		protected virtual async Task<EnsureStreamingBundlesResult> EnsureStreamingBundlesWithUwr(string bundleFileName)
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

		public abstract Task<bool> CleanAllFileCaches();
	}
}