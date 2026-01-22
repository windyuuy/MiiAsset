using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOManagers;
using MiiAsset.Runtime.Status;
using MonoExtLib.AsyncExt;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MiiAsset.Runtime
{
	public class LoadOneAssetStatus
	{
		public Task<UnityEngine.Object> Task => Op.GetTask();
		public int RefCount;
		public string Address;
		public AssetBundleRequest Op;
	}

	public interface IAssetBundleStatus : IAssetLoadStatus, IDisposable
	{
		public Task<PipelineResult> Task { get; }
		public int RefCount { get; set; }
		public AssetBundle AssetBundle { get; }
		Task<PipelineResult> Load(CatalogInfo catalogInfo);
		Task<PipelineResult> Download(CatalogInfo catalogInfo);
		public long GetDownloadSize(CatalogInfo catalogInfo);
		Task UnLoad();
		Task<T> LoadAssetJust<T>(string address, AsyncOperationStatus loadStatus) where T : Object;
		Task<T> LoadAssetJustSync<T>(string address, SyncOperationStatus loadStatus) where T : Object;
		Task UnLoadAssetJust(string address);

		/// <summary>
		/// 未卸载的Bundle
		/// </summary>
		bool IsLoaded { get; }

		/// <summary>
		/// 引用计数>0
		/// </summary>
		bool IsRefered { get; }

		/// <summary>
		/// 正在使用的(未卸载或引用计数>0)
		/// </summary>
		bool IsUsing { get; }
	}

	public static class BundleStatusNotify
	{
		public static Action<AssetBundleStatus> OnBundleLoad;
		public static Action<AssetBundleStatus> OnBundleUnLoad;
		public static Action<AssetBundleStatus> OnBundleDownLoad;
	}

	public class AssetBundleStatus : IAssetBundleStatus
	{
		public string BundleName;

		public AssetBundleStatus(string bundleName)
		{
			this.BundleName = bundleName;
		}

		public long FileSize = -1;
		public bool IsKeepInMemory = false;
		public string BundleInternalName = null;
		public uint Crc;
		public Hash128 Hash128;

		public Task<PipelineResult> Task { get; set; } = null;
		public int RefCount { get; set; }

		internal ILoadAssetBundlePipeline LoadPipeline;
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

				var downloadPipeline0 = this.LoadPipeline?.GetDownloadPipeline();
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
				if (IsInternalBundle || IsDownloaded > 0)
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

		public IEnumerable<PipelineResult> Results
		{
			get
			{
				if (Task != null)
				{
					yield return Task.Result;
				}
			}
		}

		public Task<PipelineResult> Load(CatalogInfo catalogInfo)
		{
			Debug.Assert(RefCount > 0, $"Bundle is not allowed: {this.BundleName}");
			if (AssetBundle == null)
			{
				if (Task == null || (Task.IsCompleted &&
				                     (!Task.IsCompletedSuccessfully || Task.Result == null || (!Task.Result.IsOk))))
				{
					Task = null;
					Task = LoadInternal(catalogInfo, true);
				}
			}

			Debug.Assert(Task != null, "Task != null");

			return Task;
		}

		public Task<PipelineResult> Download(CatalogInfo catalogInfo)
		{
			return LoadInternal(catalogInfo, false);
		}

		public long GetDownloadSize(CatalogInfo catalogInfo)
		{
			UpdateFileSizeInfo(catalogInfo);

			return DownloadSize;
		}

		private void UpdateFileSizeInfo(CatalogInfo catalogInfo)
		{
			if (FileSize < 0)
			{
				// 判断是否内部文件
				var bundleInfo = catalogInfo.GetAssetBundleInfo(BundleName);
				FileSize = bundleInfo.size;
				Crc = bundleInfo.crc;
				Hash128 = bundleInfo.hash128;
				IsInternalBundle = catalogInfo.IsInternalBundle(BundleName);
				BundleInternalName = bundleInfo.bundleName;

				IsKeepInMemory = bundleInfo.IsKeepInMemory;
			}
		}

		public PipelineResult Result = new();

		/// <summary>
		/// 0: not downloaded, 1: downloaded, 2: loaded
		/// </summary>
		public int IsDownloaded = 0;

		public bool IsInternalBundle;

		protected async Task<PipelineResult> LoadInternal(CatalogInfo catalogInfo, bool autoLoad)
		{
			if (autoLoad)
			{
				MyLogger.Log($"AssetBundle-Loading: {this.BundleName}");
			}
			else
			{
				MyLogger.Log($"AssetBundle-Downloading: {this.BundleName}");
			}

			UpdateFileSizeInfo(catalogInfo);

			var unloadTask = UnloadTask;
			if (unloadTask != null)
			{
				await unloadTask;
			}

			if (!autoLoad && IOManager.LocalIOProto.ExistsBundle(this.BundleName))
			{
				MyLogger.Log($"AssetBundle-ExistExternal: {BundleName}");
				IsDownloaded = 1;
				_downloadProgress = new PipelineProgress().SetDownloadedProgress(Result.IsOk);
				Result.SetOk();
				return Result;
			}

			var isInternalBundleExist = false;
			if (IsInternalBundle)
			{
				// try load from internal
				var result = await IOManager.LocalIOProto.EnsureStreamingBundles(this.BundleName);
				isInternalBundleExist = result == EnsureStreamingBundlesResult.Exist;
				if (isInternalBundleExist && !autoLoad)
				{
					MyLogger.Log($"AssetBundle-ExistInternal: {BundleName}");
					IsDownloaded = 2;
					_downloadProgress = new PipelineProgress().SetDownloadedProgress(Result.IsOk);
					Result.SetOk();
					return Result;
				}
			}

			ILoadAssetBundlePipeline loadAssetBundlePipeline;
			if (this.LoadPipeline == null)
			{
				var bundleInfo = catalogInfo.GetAssetBundleInfo(BundleName);
				var loadSource = catalogInfo.BundleLoadSourceMap[BundleName];
				loadAssetBundlePipeline =
					bundleInfo.GetLoadAssetBundlePipeline(loadSource, Crc, Hash128, bundleInfo.isEncrypt,
						(ulong)FileSize);
				var downloadPipeline = loadAssetBundlePipeline.GetDownloadPipeline();
				downloadPipeline?.PresetDownloadSize(FileSize);
				this.LoadPipeline = loadAssetBundlePipeline;
				this.Disposable = loadAssetBundlePipeline.GetDisposable();
			}
			else
			{
				loadAssetBundlePipeline = this.LoadPipeline;
			}

			if (autoLoad)
			{
				// download with loading assetbundle
				var retryTimes = 0;
				while (true)
				{
					PipelineResult result;
					try
					{
						result = await loadAssetBundlePipeline.Run();
						_progress = loadAssetBundlePipeline.GetProgress();
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
						result = new PipelineResult
						{
							IsOk = false,
							Exception = exception,
							Code = -1,
							Msg = "资源加载异常",
							Uri = BundleName,
							ErrorType = PipelineErrorType.FileSystemError,
							Status = PipelineStatus.Done,
						};
					}

					var assetBundle = loadAssetBundlePipeline.AssetBundle;
					Debug.Assert(this.AssetBundle == null, $"this.AssetBundle==null, {this.BundleName}");
					this.AssetBundle = assetBundle;
				#if UNITY_EDITOR
					BundleStatusNotify.OnBundleLoad?.Invoke(this);
				#endif
					Result.Merge(result);

					if (IsInternalBundle)
					{
						if (isInternalBundleExist)
						{
							_downloadProgress = new PipelineProgress().SetDownloadedProgress(Result.IsOk);
							// MyLogger.Log($"downloadprogress1: {BundleName}, {_downloadProgress.Total}, {loadAssetBundlePipeline.GetType()?.Name}, {FileSize}");
						}
						else
						{
							_downloadProgress = new PipelineProgress((ulong)FileSize, 0).Complete(Result.IsOk);
							// MyLogger.Log($"downloadprogress2: {BundleName}, {_downloadProgress.Total}, {loadAssetBundlePipeline.GetType()?.Name}, {FileSize}");
						}

						IsDownloaded = 2;
					}
					else if (Result.IsOk)
					{
						var downloadPipeline = loadAssetBundlePipeline.GetDownloadPipeline();
						if (downloadPipeline != null)
						{
							IsDownloaded = downloadPipeline.Result.IsOk ? 1 : 0;
							_downloadProgress = downloadPipeline.GetProgress();
							// MyLogger.Log($"downloadprogress3: {BundleName}, {_downloadProgress.Total}, {loadAssetBundlePipeline.GetType()?.Name}, {FileSize}");
						}
						else
						{
							IsDownloaded = 1;
							_downloadProgress = new PipelineProgress().SetDownloadedProgress(true);
							// MyLogger.Log($"downloadprogress4: {BundleName}, {_downloadProgress.Total}, {loadAssetBundlePipeline.GetType()?.Name}, {FileSize}");
						}
					}

					if (this.AssetBundle == null)
					{
						MyLogger.LogError($"invalid AssetBundle: {BundleName}");
						var existBundle = IsLoadDuplicated();
						if (existBundle)
						{
							var errTip = $"error: cannot load bundle twice: {BundleName}";
							MyLogger.LogError(errTip);
							_ = IOManager.Widget.ShowToast(errTip, 5);
						}
						else
						{
							// need retry
							loadAssetBundlePipeline.Invalidate();
							++retryTimes;
							if (retryTimes == 1)
							{
								Debug.Log($"AutoTryReloadAsset: {BundleName}");
								continue;
							}
							else
							{
								var retry = await RemoteUriHandler.WaitChooseReloadAsset(BundleName);
								if (retry)
								{
									continue;
								}
								else
								{
									Debug.LogError($"无视资源异常, 继续流程: {BundleName}");
								}
							}
						}
					}
					else
					{
						MyLogger.Log($"AssetBundle-Loaded: {this.BundleName}");
						IOManager.LocalIOProto.EnsureBundle(this.BundleName);
					}

					break;
				}

				this.LoadPipeline = null;

				loadAssetBundlePipeline.Dispose();
			}
			else
			{
				// download only
				PipelineResult downloadResult;
				var downloadPipeline = loadAssetBundlePipeline.GetDownloadPipeline();
				var isCached = false;
				if (IsInternalBundle || downloadPipeline == null)
				{
					isCached = true;
					downloadResult = new PipelineResult
					{
						IsOk = true,
						Exception = null,
						Code = 0,
						Msg = null,
						Status = PipelineStatus.Done,
						Uri = BundleName,
					};
				}
				else
				{
					isCached = downloadPipeline.IsCached();
					downloadResult = await downloadPipeline.Run();
					_progress = loadAssetBundlePipeline.GetProgress();
				}

				IsDownloaded = 1;
				if (IsDownloaded > 0)
				{
					if (downloadPipeline != null)
					{
						_downloadProgress = downloadPipeline.GetProgress();
					}
					else
					{
						_downloadProgress = new PipelineProgress().SetDownloadedProgress(true);
					}
				}

				if (downloadResult.IsOk)
				{
					if (!isCached)
					{
						MyLogger.Log($"AssetBundle-DownLoaded: {this.BundleName}");
					}
				#if UNITY_EDITOR
					BundleStatusNotify.OnBundleDownLoad?.Invoke(this);
				#endif

					if (!IsKeepInMemory)
					{
						this.LoadPipeline = null;
						loadAssetBundlePipeline.Dispose();
					}
				}

				return downloadResult;
			}

			return Result;
		}

		private bool IsLoadDuplicated()
		{
			return AssetBundleUtils.IsLoadDuplicated(BundleInternalName);
		}

		internal Task UnloadTask;

		public async Task UnLoad()
		{
			Debug.Assert(RefCount == 0);
			if (LoadPipeline != null && Task != null)
			{
				await Task;
			}

			LoadedAssetMap.Clear();
			LoadingAssetMap.Clear();

			if (AssetBundle != null)
			{
				UnloadTask ??= this.AssetBundle.UnloadAsync(true).GetTask();
				if (Disposable != null)
				{
					Disposable.Dispose();
					Disposable = null;
				}

				AssetBundle = null;

				Task = null;

				await UnloadTask;
				UnloadTask = null;
				MyLogger.Log($"AssetBundle-unloaded: {this.BundleName}");

			#if UNITY_EDITOR
				BundleStatusNotify.OnBundleUnLoad?.Invoke(this);
			#endif
			}
			else
			{
				Task = null;

				if (UnloadTask != null)
				{
					await UnloadTask;
					UnloadTask = null;
				}
			}
		}

		protected readonly Dictionary<string, UnityEngine.Object> LoadedAssetMap = new();

		protected readonly Dictionary<string, (AssetBundleRequest op, Task<UnityEngine.Object> task)> LoadingAssetMap =
			new();

		public async Task<T> LoadAssetJust<T>(string address, AsyncOperationStatus loadStatus) where T : Object
		{
			if (AssetBundle == null && !Task.IsCompletedSuccessfully)
			{
				if (loadStatus != null)
				{
					var exception =
						new Exception($"AssetBundle not load yet: {BundleName}, cannot load by asset key: {address}");
					loadStatus.Exception = exception;
				}

				return default;
			}

			if (AssetBundle == null)
			{
				var existBundle = IsLoadDuplicated();
				var errTip = existBundle
					? $"error: AssetBundle cannot load twice: {BundleName} for {address}"
					: $"error: AssetBundle is not load correct: {BundleName} for {address}";
				MyLogger.LogError(errTip);
				if (loadStatus != null)
				{
					var exception = new NullReferenceException(errTip);
					loadStatus.Exception = exception;
				}

				if (existBundle)
				{
					_ = IOManager.Widget.ShowToast(errTip, 5);
				}

				return default;
			}

			// var t1 = Date.Now();
			// var fc1 = Time.frameCount;
			if (LoadedAssetMap.TryGetValue(address, out var assetObj))
			{
				loadStatus?.SetCompleted(true);
			}
			else
			{
				if (!LoadingAssetMap.TryGetValue(address, out var item))
				{
					var op0 = AssetBundle.LoadAssetAsync<T>(address);
					var task0 = op0.GetTask();
					item = (op0, task0);
					LoadingAssetMap.Add(address, item);
				}

				if (loadStatus != null)
				{
					loadStatus.Set(item.op);
				}

				assetObj = await item.task;
				// 如果 address unloaded, 那么 LoadingAssetMap 不存在 address, 无需在 LoadedAssetMap add address
				if (LoadingAssetMap.Remove(address))
				{
					if (!LoadedAssetMap.TryAdd(address, assetObj))
					{
						MyLogger.LogError($"duplicate asset: {address}");
					}
				}
				// var t2 = Date.Now();
				// var fc2 = Time.frameCount;
				// MyLogger.Log($"LoadAssetAsync: {t2 - t1}, {fc2 - fc1}, from: {fc1}");
			}

			if (assetObj is T asset)
			{
				return asset;
			}
			else
			{
				if (loadStatus != null)
				{
					var exception = new InvalidCastException($"invalid asset Type<{nameof(T)}> to load: {address}");
					loadStatus.Exception = exception;
				}

				return default;
			}
		}

		public Task<T> LoadAssetJustSync<T>(string address, SyncOperationStatus loadStatus) where T : Object
		{
			if (AssetBundle == null && !Task.IsCompletedSuccessfully)
			{
				if (loadStatus != null)
				{
					var exception =
						new Exception($"AssetBundle not load yet: {BundleName}, cannot load by asset key: {address}");
					loadStatus.Exception = exception;
				}

				return System.Threading.Tasks.Task.FromResult<T>(default);
			}

			if (AssetBundle == null)
			{
				var existBundle = IsLoadDuplicated();
				var errTip = existBundle
					? $"error: AssetBundle cannot load twice: {BundleName} for {address}"
					: $"error: AssetBundle is not load correct: {BundleName} for {address}";
				MyLogger.LogError(errTip);
				if (loadStatus != null)
				{
					var exception = new NullReferenceException(errTip);
					loadStatus.Exception = exception;
				}

				if (existBundle)
				{
					_ = IOManager.Widget.ShowToast(errTip, 5);
				}

				return System.Threading.Tasks.Task.FromResult<T>(default);
			}

			// var t1 = Date.Now();
			// var fc1 = Time.frameCount;
			T asset;
			if (LoadedAssetMap.TryGetValue(address, out var assetObj))
			{
				asset = assetObj as T;
			}
			else
			{
				asset = AssetBundle.LoadAsset<T>(address);
				LoadedAssetMap.Add(address, asset);
			}

			loadStatus?.Set(asset);
			// var t2 = Date.Now();
			// var fc2 = Time.frameCount;
			// MyLogger.Log($"LoadAssetAsync: {t2 - t1}, {fc2 - fc1}, from: {fc1}");
			if (asset != null)
			{
				return System.Threading.Tasks.Task.FromResult(asset);
			}
			else
			{
				if (loadStatus != null)
				{
					var exception = new InvalidCastException($"invalid asset Type<{nameof(T)}> to load: {address}");
					loadStatus.Exception = exception;
				}

				return System.Threading.Tasks.Task.FromResult<T>(default);
			}
		}

		public Task UnLoadAssetJust(string address)
		{
			LoadedAssetMap.Remove(address);
			LoadingAssetMap.Remove(address);
			return System.Threading.Tasks.Task.CompletedTask;
		}

		public bool IsLoaded => this.AssetBundle != null;

		public bool IsRefered => this.RefCount > 0;
		public bool IsUsing => IsLoaded || IsRefered;

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

		public void Dispose()
		{
			if (this.Disposable != null)
			{
				this.Disposable.Dispose();
				this.Disposable = null;
			}

			LoadedAssetMap.Clear();
			LoadingAssetMap.Clear();
			if (this.AssetBundle != null)
			{
				this.AssetBundle.UnloadAsync(true);
				this.AssetBundle = null;

			#if UNITY_EDITOR
				BundleStatusNotify.OnBundleUnLoad?.Invoke(this);
			#endif
			}

			if (this.LoadPipeline != null)
			{
				this.LoadPipeline.Dispose();
				this.LoadPipeline = null;
			}
		}
	}
}