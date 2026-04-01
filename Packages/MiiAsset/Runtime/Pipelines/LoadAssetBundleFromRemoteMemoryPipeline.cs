using System;
using System.IO;
using System.Threading.Tasks;
using MiiAsset.Runtime.Encrypt;
using MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromRemoteMemoryPipeline : ILoadAssetBundlePipeline
	{
		protected string RemoteUri;
		protected WebDownloadToMemoryPipeline DownloadToMemoryPipeline;
		protected uint Crc;
		protected bool IsEncrypt;

		public LoadAssetBundleFromRemoteMemoryPipeline Init(string remoteUri, uint crc, bool isEncrypt)
		{
			RemoteUri = remoteUri;
			this.Crc = crc;
			IsEncrypt = isEncrypt;
			this.Build();
			return this;
		}

		public void Dispose()
		{
			if (DownloadToMemoryPipeline != null)
			{
				DownloadToMemoryPipeline.Dispose();
				DownloadToMemoryPipeline = null;
			}
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
				if (IsEncrypt)
				{
					SharedEncrypt.Encryptor.Encrypt(bytes, 0, 0, bytes.Length);
				}
				this.AssetBundle = AssetBundle.LoadFromMemory(bytes, Crc);

				if (this.AssetBundle == null)
				{
					Result = new()
					{
						IsOk = false,
						Code = 0,
						Msg = $"invalid bundle data: {RemoteUri}",
						ErrorType = PipelineErrorType.DataIncorrect,
						Uri = RemoteUri,
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
			var downloadPipelineProgress = DownloadToMemoryPipeline.GetProgress();
			if (Result != null)
			{
				return downloadPipelineProgress
					.Combine(new PipelineProgress().SetDownloadedProgress(Result?.IsOk??false));
			}
			else
			{
				return downloadPipelineProgress;
			}
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

		public void Invalidate()
		{
			if (AssetBundle != null)
			{
				AssetBundle.Unload(true);
				AssetBundle = null;
			}

			if (DownloadToMemoryPipeline != null)
			{
				DownloadToMemoryPipeline.Invalidate();
			}
		}
	}
}