﻿using System;
using System.Threading.Tasks;
using MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromRemoteBytesPipeline : ILoadAssetBundlePipeline
	{
		public IDownloadPipeline DownloadPipeline;
		protected LoadAssetBundleFromLocalBytesPipeline LoadAssetBundlePipeline;

		protected string RemoteUri;
		protected string LocalUri;
		protected uint Crc;

		public LoadAssetBundleFromRemoteBytesPipeline Init(string remoteUri, string localUri, uint crc)
		{
			RemoteUri = remoteUri;
			LocalUri = localUri;
			this.Crc = crc;
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
			DownloadPipeline = new DownloadPipeline().Init(RemoteUri, LocalUri, false);
			LoadAssetBundlePipeline = new LoadAssetBundleFromLocalBytesPipeline().Init(LocalUri, Crc);
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