using System;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MonoExtLib.AsyncExt;
using UnityEngine;
using UnityEngine.Networking;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleInternalPipeline : ILoadAssetBundlePipeline, IDownloadPipeline
	{
		protected string RemoteUri;
		protected UnityWebRequest Uwr;
		protected DownloadHandlerAssetBundle DownloadHandler;
		protected AssetBundle CachedAssetBundle;
		protected uint Crc;
		protected Hash128 Hash128;

		public LoadAssetBundleInternalPipeline Init(string remoteUri, uint crc, Hash128 hash128)
		{
			if (remoteUri.Contains("://"))
			{
				RemoteUri = remoteUri;
			}
			else
			{
				RemoteUri = "file://" + remoteUri;
			}

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
			Uwr = UnityWebRequestAssetBundle.GetAssetBundle(RemoteUri);
			DownloadHandler = (DownloadHandlerAssetBundle)Uwr.downloadHandler;
			DownloadHandler.autoLoadAssetBundle = false;
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
				var sendWebRequest = Uwr.SendWebRequest();
				Result.Status = PipelineStatus.Running;
				await sendWebRequest.GetTask();
				Result.SetWithUwr(Uwr);
			}
			catch (Exception exception)
			{
				Result.ErrorType = PipelineErrorType.NetError;
				MyLogger.LogException(exception, "e36");
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

		public void Invalidate()
		{
			Reset();
			Build();
		}

		public void PresetDownloadSize(long fileSize)
		{
			PresetSize = fileSize;
		}

		public AssetBundle AssetBundle
		{
			get
			{
				if (CachedAssetBundle != null)
				{
					return CachedAssetBundle;
				}

				if (DownloadHandler != null)
				{
					CachedAssetBundle = DownloadHandler.assetBundle;
				}

				return CachedAssetBundle;
			}
		}

		public IDisposable GetDisposable()
		{
			return Uwr;
		}

		protected void SetAutoLoad(bool autoLoadAssetBundle)
		{
			DownloadHandler.autoLoadAssetBundle = autoLoadAssetBundle;
		}

		// protected LoadAssetBundleInternalPipeline DownloadPipeline;

		public IDownloadPipeline GetDownloadPipeline()
		{
			// if (DownloadPipeline == null)
			// {
			// DownloadPipeline = new LoadAssetBundleInternalPipeline().Init(RemoteUri, Crc, Hash128);
			// DownloadPipeline.SetAutoLoad(false);
			// }

			// return DownloadPipeline;
			return this;
		}
	}
}