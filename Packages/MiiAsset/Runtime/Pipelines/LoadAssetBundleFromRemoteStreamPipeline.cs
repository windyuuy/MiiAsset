using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromRemoteStreamPipeline : ILoadAssetBundlePipeline
	{
		public IDownloadPipeline DownloadPipeline;
		protected LoadAssetBundlePipelineFromLocalStream LoadAssetBundlePipeline;

		protected string RemoteUri;
		protected string LocalUri;
		protected uint Crc;
		protected bool IsEncrypt;
		protected ulong PredictFileSize;

		public LoadAssetBundleFromRemoteStreamPipeline Init(string remoteUri, string localUri, uint crc, bool isEncrypt,
			ulong predictFileSize)
		{
			RemoteUri = remoteUri;
			LocalUri = localUri;
			IsEncrypt = isEncrypt;
			Crc = crc;
			PredictFileSize = predictFileSize;
			this.Build();
			return this;
		}

		public void Dispose()
		{
			if (DownloadPipeline != null)
			{
				DownloadPipeline.Dispose();
				DownloadPipeline = null;
			}

			if (LoadAssetBundlePipeline != null)
			{
				LoadAssetBundlePipeline.Dispose();
				LoadAssetBundlePipeline = null;
			}
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
			DownloadPipeline = new DownloadPipeline().Init(RemoteUri, LocalUri, false, PredictFileSize);
			LoadAssetBundlePipeline = new LoadAssetBundlePipelineFromLocalStream().Init(LocalUri, Crc, IsEncrypt);
		}

		public AssetBundle AssetBundle => LoadAssetBundlePipeline.AssetBundle;

		public IDisposable GetDisposable()
		{
			return LoadAssetBundlePipeline.GetDisposable();
		}

		public IDownloadPipeline GetDownloadPipeline()
		{
			return DownloadPipeline;
		}

		public async Task<PipelineResult> Run()
		{
			Result = await DownloadPipeline.Run();
			if (Result.IsOk)
			{
				Result = await LoadAssetBundlePipeline.Run();
			}

			return Result;
		}

		public bool IsCached()
		{
			return LoadAssetBundlePipeline.IsCached();
		}

		public PipelineProgress GetProgress()
		{
			return DownloadPipeline.CombineProgress(LoadAssetBundlePipeline);
		}
	}
}