using System;
using System.Threading.Tasks;
using lang.time;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromLocalBytesPipeline : ILoadAssetBundlePipeline
	{
		protected string Uri;
		protected uint Crc;

		public LoadAssetBundleFromLocalBytesPipeline Init(string uri, uint crc)
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
				var t1 = Date.Now();
				AssetBundle = AssetBundle.LoadFromMemory(bytes, Crc);
				MyLogger.De($"ldab-Load: {Date.Now() - t1}, {Uri}");
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