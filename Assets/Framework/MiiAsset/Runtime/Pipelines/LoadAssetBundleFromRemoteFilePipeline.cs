using System.Threading.Tasks;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromRemoteFilePipeline : IPipeline
	{
		protected DownloadPipeline DownloadPipeline;
		protected LoadAssetBundleFromFilePipeline LoadAssetBundlePipeline;

		protected string RemoteUri;
		protected string LocalUri;
		public PipelineResult Result { get; set; }

		public LoadAssetBundleFromRemoteFilePipeline Init(string remoteUri, string localUri)
		{
			RemoteUri = remoteUri;
			LocalUri = localUri;
			this.Build();
			return this;
		}

		public void Dispose()
		{
			DownloadPipeline.Dispose();
			LoadAssetBundlePipeline.Dispose();
			DownloadPipeline = null;
			LoadAssetBundlePipeline = null;
		}

		public void Build()
		{
			DownloadPipeline = new DownloadPipeline().Init(RemoteUri, LocalUri, false);
			LoadAssetBundlePipeline = new LoadAssetBundleFromFilePipeline().Init(LocalUri);
		}

		public AssetBundle AssetBundle => LoadAssetBundlePipeline.AssetBundle;

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