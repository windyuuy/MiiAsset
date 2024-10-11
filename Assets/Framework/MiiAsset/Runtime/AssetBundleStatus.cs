using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using Framework.MiiAsset.Runtime.Pipelines;
using Framework.MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework.MiiAsset.Runtime
{
	public class LoadOneAssetStatus
	{
		public Task<object> Task => Op.GetTask();
		public int RefCount;
		public string Address;
		public AssetBundleRequest Op;
	}

	public interface IAssetBundleStatus : IAssetLoadStatus
	{
		public Task<PipelineResult> Task { get; }
		public int RefCount { get; set; }
		public AssetBundle AssetBundle { get; }
		Task<PipelineResult> Load(CatalogInfo catalogInfo);
		Task<PipelineResult> Download(CatalogInfo catalogInfo);
		public long GetDownloadSize(CatalogInfo catalogInfo);
		Task UnLoad();
		Task<T> LoadAssetJust<T>(string address, AsyncOperationStatus loadStatus);
		Task UnLoadAssetJust(string address);
	}

	public class AssetBundleStatus : IAssetBundleStatus
	{
		public string BundleName;

		public AssetBundleStatus(string bundleName)
		{
			this.BundleName = bundleName;
		}

		public long FileSize = -1;

		public Task<PipelineResult> Task { get; set; } = null;
		public int RefCount { get; set; }

		internal IPipeline LoadPipeline;
		internal IDisposable Disposable;

		public AssetBundle AssetBundle { get; set; }


		private PipelineProgress _progress;

		public PipelineProgress Progress
		{
			get
			{
				if (Result.IsDone)
				{
					return _progress;
				}

				if (LoadPipeline != null)
				{
					return new PipelineProgress().Combine(LoadPipeline.GetProgress());
				}
				else
				{
					return new PipelineProgress().Set01Progress(false);
				}
			}
		}


		private PipelineProgress _downloadProgress;

		public PipelineProgress DownloadProgress
		{
			get
			{
				if (IsDownloaded > 0)
				{
					return _downloadProgress;
				}

				IDownloadPipeline downloadPipeline0;
				if (this.LoadPipeline is DownloadPipeline downloadPipeline)
				{
					downloadPipeline0 = downloadPipeline;
				}
				else if (this.LoadPipeline is LoadAssetBundleFromRemoteStreamPipeline loadAssetBundleFromRemoteStreamPipeline)
				{
					downloadPipeline0 = loadAssetBundleFromRemoteStreamPipeline.DownloadPipeline;
				}
				else
				{
					downloadPipeline0 = null;
				}

				if (downloadPipeline0 != null)
				{
					return downloadPipeline0.GetProgress();
				}

				return new PipelineProgress((ulong)FileSize, 0);
			}
		}

		public bool IsFileExist = false;

		public long DownloadSize
		{
			get
			{
				if (IsDownloaded > 0)
				{
					IsFileExist = true;
				}
				else if (!IsFileExist)
				{
					IsFileExist = IOManager.LocalIOProto.ExistsBundle(BundleName);
				}

				if (!IsFileExist)
				{
					return FileSize;
				}

				return 0;
			}
		}

		public Task<PipelineResult> Load(CatalogInfo catalogInfo)
		{
			Debug.Assert(RefCount > 0, $"Bundle is not allowed: {this.BundleName}");
			if (AssetBundle == null)
			{
				if (Task == null || (Task.IsCompleted && !Task.IsCompletedSuccessfully))
				{
					Task = LoadInternal(catalogInfo, true);
				}
			}

			return Task;
		}

		public Task<PipelineResult> Download(CatalogInfo catalogInfo)
		{
			return LoadInternal(catalogInfo, false);
		}

		public long GetDownloadSize(CatalogInfo catalogInfo)
		{
			if (FileSize < 0)
			{
				FileSize = catalogInfo.GetFileSize(BundleName);
			}

			return DownloadSize;
		}

		public PipelineResult Result = new();

		/// <summary>
		/// 0: not downloaded, 1: downloaded, 2: loaded
		/// </summary>
		public int IsDownloaded = 0;

		protected async Task<PipelineResult> LoadInternal(CatalogInfo catalogInfo, bool autoLoad)
		{
			FileSize = catalogInfo.GetFileSize(this.BundleName);
			var unloadTask = UnloadTask;
			if (unloadTask != null)
			{
				await unloadTask;
			}

			var bundleInfo = catalogInfo.GetAssetBundleInfo(BundleName);
			var loadSource = catalogInfo.BundleLoadSourceMap[BundleName];
			if (autoLoad)
			{
				// download with loading assetbundle
				using var loadAssetBundlePipeline = bundleInfo.GetLoadAssetBundlePipeline(loadSource);
				if (this.LoadPipeline is DownloadPipeline downloadPipeline
				    && loadAssetBundlePipeline is LoadAssetBundleFromRemoteStreamPipeline loadAssetBundleFromRemoteStreamPipeline)
				{
					loadAssetBundleFromRemoteStreamPipeline.DownloadPipeline = downloadPipeline;
				}

				this.LoadPipeline = loadAssetBundlePipeline;
				this.Disposable = loadAssetBundlePipeline.GetDisposable();

				var result = await loadAssetBundlePipeline.Run();
				_progress = loadAssetBundlePipeline.GetProgress();
				var assetBundle = loadAssetBundlePipeline.AssetBundle;
				Debug.Assert(this.AssetBundle == null, $"this.AssetBundle==null, {this.BundleName}");
				this.AssetBundle = assetBundle;
				Result.Merge(result);

				if (loadAssetBundlePipeline is LoadAssetBundleFromRemoteStreamPipeline loadAssetBundleFromRemoteStreamPipeline2)
				{
					IsDownloaded = loadAssetBundleFromRemoteStreamPipeline2.DownloadPipeline.Result.IsOk ? 1 : 0;
					_downloadProgress = loadAssetBundleFromRemoteStreamPipeline2.DownloadPipeline.GetProgress();
				}
				else if (Result.IsOk)
				{
					_downloadProgress = new PipelineProgress().SetDownloadedProgress(true);
					IsDownloaded = 2;
				}

				this.LoadPipeline = null;

				if (this.AssetBundle == null)
				{
					Debug.LogError($"invalid AssetBundle: {BundleName}");
				}
				else
				{
					Debug.Log($"AssetBundle-loaded: {this.BundleName}");
				}
			}
			else
			{
				// download only
				PipelineResult downloadResult;
				if (this.LoadPipeline is LoadAssetBundleFromRemoteStreamPipeline loadAssetBundleFromRemoteStreamPipeline)
				{
					var loadAssetBundlePipeline = loadAssetBundleFromRemoteStreamPipeline.DownloadPipeline;
					downloadResult = await loadAssetBundlePipeline.Run();
				}
				else if (this.LoadPipeline is DownloadPipeline downloadPipeline)
				{
					downloadResult = await downloadPipeline.Run();
				}
				else if (this.LoadPipeline is LoadAssetBundleFromRemoteMemoryStreamPipeline memoryStreamPipeline)
				{
					downloadResult = new PipelineResult
					{
						IsOk = true,
						Exception = null,
						Code = 0,
						Msg = null,
						Status = PipelineStatus.Done,
					};
				}
				else
				{
					Debug.Assert(this.LoadPipeline == null, "this.LoadPipeline ==null");
					var cacheUri = loadSource.GetCacheUri(bundleInfo.fileName);
					if (cacheUri != null)
					{
						var remoteUri = loadSource.GetSourceUri(bundleInfo.fileName);
						using var loadAssetBundlePipeline = new DownloadPipeline().Init(remoteUri, cacheUri, false);
						this.LoadPipeline = loadAssetBundlePipeline;

						downloadResult = await loadAssetBundlePipeline.Run();
						if (this.LoadPipeline == loadAssetBundlePipeline)
						{
							_progress = loadAssetBundlePipeline.GetProgress();
							this.LoadPipeline = null;
						}
					}
					else
					{
						// cache not support, load from internal
						downloadResult = new PipelineResult
						{
							IsOk = true,
							Exception = null,
							Code = 0,
							Msg = null,
							Status = PipelineStatus.Done,
						};
					}
				}

				IsDownloaded = 1;
				if (IsDownloaded > 0)
				{
					_downloadProgress = new PipelineProgress((ulong)FileSize, (ulong)FileSize);
				}

				return downloadResult;
			}

			return Result;
		}

		internal Task UnloadTask;

		public async Task UnLoad()
		{
			Debug.Assert(RefCount == 0);
			if (LoadPipeline is ILoadAssetBundlePipeline)
			{
				await Task;
			}

			if (AssetBundle != null)
			{
				UnloadTask = this.AssetBundle.UnloadAsync(true).GetTask();
				if (Disposable != null)
				{
					Disposable.Dispose();
					Disposable = null;
				}

				Task = null;

				await UnloadTask;
				UnloadTask = null;
				Debug.Log($"AssetBundle-unloaded: {this.BundleName}");
			}
		}

		public async Task<T> LoadAssetJust<T>(string address, AsyncOperationStatus loadStatus)
		{
			if (AssetBundle == null && !Task.IsCompletedSuccessfully)
			{
				throw new Exception($"AssetBundle not load yet: {BundleName}, cannot load by asset key: {address}");
			}

			var op = AssetBundle.LoadAssetAsync<T>(address);
			loadStatus?.Set(op);
			var task = op.GetTask();
			await task;
			if (op.asset is T asset)
			{
				return asset;
			}
			else
			{
				throw new InvalidCastException($"invalid asset Type<{nameof(T)}> to load: {address}");
			}
		}

		public Task UnLoadAssetJust(string address)
		{
			return System.Threading.Tasks.Task.CompletedTask;
		}

		protected Dictionary<string, LoadOneAssetStatus> AssetStatusMap = new();
		private IAssetBundleStatus _assetBundleStatusImplementation;

		public LoadOneAssetStatus GetOrCreateAssetStatus(string address)
		{
			if (!AssetStatusMap.TryGetValue(address, out var status))
			{
				status = new()
				{
					Address = address,
				};
				AssetStatusMap.Add(address, status);
			}

			return status;
		}

		public async Task<T> LoadAssetByRefer<T>(string address)
		{
			var status = GetOrCreateAssetStatus(address);
			AssetBundleRequest op;
			if (status.RefCount > 0)
			{
				op = status.Op;
			}
			else
			{
				op = AssetBundle.LoadAssetAsync<T>(address);
				status.Op = op;
			}

			var task = op.GetTask();
			++status.RefCount;
			await task;
			if (op.asset is T asset)
			{
				return asset;
			}
			else
			{
				throw new InvalidCastException($"invalid asset Type<{nameof(T)}> to load: {address}");
			}
		}

		public Task UnLoadAssetByRefer(string address)
		{
			var status = GetOrCreateAssetStatus(address);
			if (status.RefCount > 0)
			{
				--status.RefCount;
				return status.Task;
			}

			return System.Threading.Tasks.Task.CompletedTask;
		}
	}
}