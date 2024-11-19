using System;
using System.IO;
using System.Threading.Tasks;
using MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromRemoteMemoryPipeline : ILoadAssetBundlePipeline
	{
		protected string RemoteUri;
		protected WebDownloadToMemoryPipeline DownloadToMemoryPipeline;
		protected uint Crc;

		public LoadAssetBundleFromRemoteMemoryPipeline Init(string remoteUri, uint crc)
		{
			RemoteUri = remoteUri;
			this.Crc = crc;
			this.Build();
			return this;
		}

		public void Dispose()
		{
			DownloadToMemoryPipeline.Dispose();
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
			DownloadToMemoryPipeline = new WebDownloadToMemoryPipeline().Init(RemoteUri);
		}

		public async Task<PipelineResult> Run()
		{
			Result = await DownloadToMemoryPipeline.Run();
			if (Result.IsOk)
			{
				var bytes = DownloadToMemoryPipeline.Bytes;
				this.AssetBundle = AssetBundle.LoadFromMemory(bytes, Crc);

				if (this.AssetBundle == null)
				{
					Result = new()
					{
						IsOk = false,
						Code = 0,
						Msg = "invalid bundle data",
						ErrorType = PipelineErrorType.DataIncorrect,
					};
				}
				// else
				// {
				// 	MyLogger.Log($"bundle-loaded: {RemoteUri}");
				// }
			}

			return Result;
		}

		public bool IsCached()
		{
			return false;
		}

		public PipelineProgress GetProgress()
		{
			return DownloadToMemoryPipeline.GetProgress().Combine(new PipelineProgress().SetDownloadedProgress(Result.IsOk));
		}

		public AssetBundle AssetBundle { get; set; }
		public IDisposable GetDisposable()
		{
			return null;
		}

		public IDownloadPipeline GetDownloadPipeline()
		{
			return DownloadToMemoryPipeline;
		}
	}
}