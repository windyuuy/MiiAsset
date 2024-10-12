using System;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleBytesPipeline : ILoadAssetBundlePipeline
	{
		protected string Uri;
		protected uint Crc;

		public LoadAssetBundleBytesPipeline Init(string uri, uint crc)
		{
			Uri = uri;
			Crc = crc;
			Result = new();
			this.Build();
			return this;
		}

		public void Dispose()
		{
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
		}

		public AssetBundle AssetBundle { get; set; }
		public IDisposable GetDisposable()
		{
			return null;
		}

		public IDownloadPipeline GetDownloadPipeline()
		{
			return null;
		}

		public async Task<PipelineResult> Run()
		{
			if (AssetBundle == null)
			{
				var bytes = await IOManager.LocalIOProto.ReadAllBytesAsync(Uri);
				AssetBundle = AssetBundle.LoadFromMemory(bytes, Crc);
				if (AssetBundle != null)
				{
					Result.IsOk = true;
				}
				else
				{
					Result.ErrorType = PipelineErrorType.DataIncorrect;
				}
			}
			else
			{
				Result.IsOk = true;
			}

			Result.Status = PipelineStatus.Done;

			return Result;
		}

		public bool IsCached()
		{
			return IOManager.LocalIOProto.Exists(Uri);
		}

		public PipelineProgress GetProgress()
		{
			return new PipelineProgress().Set01Progress(Result.IsOk);
		}
	}
}