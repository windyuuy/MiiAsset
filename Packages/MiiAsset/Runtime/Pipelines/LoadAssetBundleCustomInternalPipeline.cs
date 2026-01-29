#if SUPPORT_WEBGL_LOCAL_STORAGE
using System;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.Encrypt;
using MiiAsset.Runtime.IOManagers;
using MonoExtLib.AsyncExt;
using UnityEngine;
using UnityEngine.Networking;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleCustomInternalPipeline : ILoadAssetBundlePipeline, IDownloadPipeline
	{
		protected string RemoteUri;
		protected string CacheUri;
		protected UnityWebRequest Uwr;
		protected DownloadHandler DownloadHandler;
		protected AssetBundle CachedAssetBundle;
		protected uint Crc;
		protected Hash128 Hash128;
		protected bool IsEncrypt;
		protected int PredictFileSize;

		public LoadAssetBundleCustomInternalPipeline Init(string remoteUri, string cacheUri, uint crc, Hash128 hash128,
			bool isEncrypt, ulong predictFileSize)
		{
			if (remoteUri.Contains("://"))
			{
				RemoteUri = remoteUri;
			}
			else
			{
				RemoteUri = "file://" + remoteUri;
			}

			CacheUri = cacheUri;
			IsEncrypt = isEncrypt;
			PredictFileSize = (int)predictFileSize;

			this.Crc = crc;
			this.Hash128 = hash128;
			this.Build();
			return this;
		}

		public void Dispose()
		{
			Reset();
		}

		private void Reset()
		{
			if (DownloadHandler != null)
			{
				DownloadHandler.Dispose();
				DownloadHandler = null;
			}

			if (Uwr != null)
			{
				Uwr.Dispose();
				Uwr = null;
			}

			LoadTask = null;
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
			Uwr = UnityWebRequest.Get(RemoteUri);
			IOManager.LocalIOProto.SetUwr(Uwr);
			DownloadHandler = Uwr.downloadHandler;
			Result = new PipelineResult
			{
				IsOk = false,
				Exception = null,
				Code = -1,
				Msg = "",
				ErrorType = PipelineErrorType.NetError,
				Status = PipelineStatus.Init,
				Uri = RemoteUri,
			};
		}

		protected Task<PipelineResult> LoadTask = null;

		public Task<PipelineResult> Run()
		{
			LoadTask ??= _run();
			return LoadTask;
		}

		private async Task<PipelineResult> _run()
		{
			// DownloadPipeline = this;

			try
			{
				if (!string.IsNullOrEmpty(CacheUri) && IOManager.LocalIOProto.Exists(CacheUri))
				{
					CachedAssetBundle = await IOManager.LocalIOProto.ReadAllBytesAsync(CacheUri, bytes =>
					{
						if (bytes.Length != (int)PredictFileSize)
						{
							Result.ErrorType = PipelineErrorType.DataIncorrect;
							Result.Msg = $"File Length unmatched: {bytes.Length}!={PredictFileSize}";
							return null;
						}
						else
						{
							if (IsEncrypt)
							{
								SharedEncrypt.Encryptor.Encrypt(bytes);
							}

							var assetBundle = AssetBundle.LoadFromMemory(bytes, Crc);
							if (assetBundle == null)
							{
								var exist = IOManager.LocalIOProto.Exists(CacheUri);
								MyLogger.LogError(
									$"Failed to Load AssetBundle firstTime1: {CacheUri}, {exist}, {bytes.Length}");
							}

							return assetBundle;
						}
					});
				}
				else
				{
					var sendWebRequest = Uwr.SendWebRequest();
					Result.Status = PipelineStatus.Running;
					await sendWebRequest.GetTask();
					Result.SetWithUwr(Uwr);
					if (Result.IsOk)
					{
						var bytes = DownloadHandler.data;
						if (bytes.Length == PredictFileSize)
						{
							if (!string.IsNullOrEmpty(CacheUri))
							{
								IOManager.LocalIOProto.WriteAllBytesAsync(CacheUri, bytes);
							}

							if (IsEncrypt)
							{
								SharedEncrypt.Encryptor.Encrypt(bytes);
							}

							CachedAssetBundle = UnityEngine.AssetBundle.LoadFromMemory(bytes, Crc);
						}
						else
						{
							Result.IsOk = false;
							Result.ErrorType = PipelineErrorType.DataIncorrect;
							var errMsg = $"File Length unmatched: {bytes.Length}!={PredictFileSize}";
							MyLogger.LogError(errMsg);
							Result.Msg = errMsg;
						}
					}
				}
			}
			catch (Exception exception)
			{
				Result.ErrorType = PipelineErrorType.NetError;
				MyLogger.LogException(exception, "e50");
			}

			return Result;
		}

		public bool IsCached()
		{
		#if UNITY_WEBGL && !UNITY_EDITOR
			// TODO: 使用hash和uri间接存储
			return false;
		#else
			var exist = Caching.IsVersionCached(RemoteUri, Hash128);
			return exist;
		#endif
		}

		protected PipelineProgress Progress = new PipelineProgress();
		protected long PresetSize = -1;

		public PipelineProgress GetProgress()
		{
			if (Result.Status == PipelineStatus.Init)
			{
				if (PresetSize >= 0)
				{
					Progress = new((ulong)PresetSize, 0);
				}
				else
				{
					Progress.Set01Progress(false);
				}
			}
			else if (Result.Status == PipelineStatus.Done)
			{
				Progress.Complete();
			}
			else
			{
				var uwrDownloadedBytes = Uwr.downloadedBytes;
				ulong waitDoneAddition = 100;
				Progress = new()
				{
					Total = Math.Max(uwrDownloadedBytes, (ulong)(uwrDownloadedBytes / Uwr.downloadProgress)) +
					        waitDoneAddition,
					Count = uwrDownloadedBytes,
				};
			}

			return Progress;
		}

		private void DeleteCachedFile()
		{
			if (!string.IsNullOrEmpty(CacheUri))
			{
				var exist = IOManager.LocalIOProto.Exists(CacheUri);
				if (exist)
				{
					IOManager.LocalIOProto.Delete(CacheUri);
				}
			}
		}

		public void Invalidate()
		{
			Reset();
			DeleteCachedFile();
			Build();
		}

		public void PresetDownloadSize(long fileSize)
		{
			PresetSize = fileSize;
		}

		public AssetBundle AssetBundle
		{
			get { return CachedAssetBundle; }
		}

		public IDisposable GetDisposable()
		{
			return Uwr;
		}

		public IDownloadPipeline GetDownloadPipeline()
		{
			return this;
		}
	}
}
#endif
