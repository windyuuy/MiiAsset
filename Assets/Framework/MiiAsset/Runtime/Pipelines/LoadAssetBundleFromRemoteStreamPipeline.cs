using System;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromRemoteStreamPipeline : ILoadAssetBundlePipeline
	{
		public IDownloadPipeline DownloadPipeline;
		protected LoadAssetBundlePipeline LoadAssetBundlePipeline;

		protected string RemoteUri;
		protected string LocalUri;

		public LoadAssetBundleFromRemoteStreamPipeline Init(string remoteUri, string localUri)
		{
			RemoteUri = remoteUri;
			LocalUri = localUri;
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
			LoadAssetBundlePipeline = new LoadAssetBundlePipeline().Init(LocalUri);
		}

		public AssetBundle AssetBundle => LoadAssetBundlePipeline.AssetBundle;
		public IDisposable GetDisposable()
		{
			return LoadAssetBundlePipeline.GetDisposable();
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