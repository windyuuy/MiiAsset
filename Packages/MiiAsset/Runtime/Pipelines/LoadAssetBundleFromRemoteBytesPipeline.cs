using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromRemoteBytesPipeline : ILoadAssetBundlePipeline
	{
		public DownloadPipeline DownloadPipeline;
		protected LoadAssetBundleFromLocalBytesPipeline LoadAssetBundlePipeline;

		protected string RemoteUri;
		protected string LocalUri;
		protected uint Crc;
		protected bool IsEncrypt;
		protected ulong PredictFileSize;

		public LoadAssetBundleFromRemoteBytesPipeline Init(string remoteUri, string localUri, uint crc, bool isEncrypt,
			ulong predictFileSize)
		{
			RemoteUri = remoteUri;
			LocalUri = localUri;
			Crc = crc;
			IsEncrypt = isEncrypt;
			PredictFileSize = predictFileSize;
			Build();
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
			LoadAssetBundlePipeline = new LoadAssetBundleFromLocalBytesPipeline().Init(LocalUri, Crc, IsEncrypt, PredictFileSize);
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

		public void Invalidate()
		{
			if (LoadAssetBundlePipeline != null)
			{
				LoadAssetBundlePipeline.Invalidate();
			}

			if (DownloadPipeline != null)
			{
				DownloadPipeline.Invalidate();
			}
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