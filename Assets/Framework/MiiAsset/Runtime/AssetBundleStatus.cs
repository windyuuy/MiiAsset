using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using Framework.MiiAsset.Runtime.Status;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework.MiiAsset.Runtime
{
    public class LoadOneAssetStatus
    {
        public Task<object> Task => Op.GetTask();
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
        Task<T> LoadAssetJust<T>(string address, AsyncOperationStatus loadStatus);
        Task UnLoadAssetJust(string address);
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
        public uint Crc;

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
            get { yield return Task.Result; }
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
                IsInternalBundle = catalogInfo.IsInternalBundle(BundleName);
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
            UpdateFileSizeInfo(catalogInfo);

            var unloadTask = UnloadTask;
            if (unloadTask != null)
            {
                await unloadTask;
            }

            var isInternalBundleExist = false;
            if (IsInternalBundle)
            {
                var result = await IOManager.LocalIOProto.EnsureStreamingBundles(this.BundleName);
                isInternalBundleExist = result == EnsureStreamingBundlesResult.Exist;
            }

            ILoadAssetBundlePipeline loadAssetBundlePipeline;
            if (this.LoadPipeline == null)
            {
                var bundleInfo = catalogInfo.GetAssetBundleInfo(BundleName);
                var loadSource = catalogInfo.BundleLoadSourceMap[BundleName];
                loadAssetBundlePipeline = bundleInfo.GetLoadAssetBundlePipeline(loadSource, Crc);
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

                var result = await loadAssetBundlePipeline.Run();
                _progress = loadAssetBundlePipeline.GetProgress();
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
                        // Debug.Log($"downloadprogress1: {BundleName}, {_downloadProgress.Total}, {loadAssetBundlePipeline.GetType()?.Name}, {FileSize}");
                    }
                    else
                    {
                        _downloadProgress = new PipelineProgress((ulong)FileSize, 0).Complete(Result.IsOk);
                        // Debug.Log($"downloadprogress2: {BundleName}, {_downloadProgress.Total}, {loadAssetBundlePipeline.GetType()?.Name}, {FileSize}");
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
                        // Debug.Log($"downloadprogress3: {BundleName}, {_downloadProgress.Total}, {loadAssetBundlePipeline.GetType()?.Name}, {FileSize}");
                    }
                    else
                    {
                        IsDownloaded = 1;
                        _downloadProgress = new PipelineProgress().SetDownloadedProgress(true);
                        // Debug.Log($"downloadprogress4: {BundleName}, {_downloadProgress.Total}, {loadAssetBundlePipeline.GetType()?.Name}, {FileSize}");
                    }
                }

                this.LoadPipeline = null;

                if (this.AssetBundle == null)
                {
                    Debug.LogError($"invalid AssetBundle: {BundleName}");
                }
                else
                {
                    Debug.Log($"AssetBundle-Loaded: {this.BundleName}");
                    IOManager.LocalIOProto.EnsureBundle(this.BundleName);
                }

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
                        Debug.Log($"AssetBundle-DownLoaded: {this.BundleName}");
                    }
#if UNITY_EDITOR
                    BundleStatusNotify.OnBundleDownLoad?.Invoke(this);
#endif
                }

                return downloadResult;
            }

            return Result;
        }

        internal Task UnloadTask;

        public async Task UnLoad()
        {
            Debug.Assert(RefCount == 0);
            if (LoadPipeline != null && Task != null)
            {
                await Task;
            }

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
                Debug.Log($"AssetBundle-unloaded: {this.BundleName}");

#if UNITY_EDITOR
                BundleStatusNotify.OnBundleUnLoad?.Invoke(this);
#endif
            }
        }

        public async Task<T> LoadAssetJust<T>(string address, AsyncOperationStatus loadStatus)
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
                if (loadStatus != null)
                {
                    var exception = new InvalidCastException($"invalid asset Type<{nameof(T)}> to load: {address}");
                    loadStatus.Exception = exception;
                }

                return default;
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

        public void Dispose()
        {
            if (this.Disposable != null)
            {
                this.Disposable.Dispose();
                this.Disposable = null;
            }

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