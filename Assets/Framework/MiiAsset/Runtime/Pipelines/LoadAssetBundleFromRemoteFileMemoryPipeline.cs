using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromRemoteFileMemoryPipeline: ILoadAssetBundlePipeline
	{
		public IDownloadPipeline DownloadPipeline;
		protected ILoadAssetBundlePipeline LoadAssetBundlePipeline;

		protected string RemoteUri;
		protected string LocalUri;

		public LoadAssetBundleFromRemoteFileMemoryPipeline Init(string remoteUri, string localUri)
		{
			RemoteUri = remoteUri;
			LocalUri = localUri;
			this.Build();
			return this;
		}

		public void Dispose()
		{
			
		}

		public PipelineResult Result { get; }
		public void Build()
		{
			throw new NotImplementedException();
		}

		public Task<PipelineResult> Run()
		{
			throw new NotImplementedException();
		}

		public bool IsCached()
		{
			throw new NotImplementedException();
		}

		public PipelineProgress GetProgress()
		{
			throw new NotImplementedException();
		}

		public AssetBundle AssetBundle { get; }
		public IDisposable GetDisposable()
		{
			return null;
		}

		public IDownloadPipeline GetDownloadPipeline()
		{
			throw new NotImplementedException();
		}
	}
}