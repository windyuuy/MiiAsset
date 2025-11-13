using System;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.Encrypt;
using MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromLocalBytesPipeline : ILoadAssetBundlePipeline
	{
		protected string Uri;
		protected uint Crc;
		protected bool IsEncrypt;

		public LoadAssetBundleFromLocalBytesPipeline Init(string uri, uint crc, bool isEncrypt)
		{
			Uri = uri;
			Crc = crc;
			IsEncrypt = isEncrypt;
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
				if (IsEncrypt)
				{
					SharedEncrypt.Encryptor.Encrypt(bytes, 0, 0, bytes.Length);
				}

				AssetBundle = AssetBundle.LoadFromMemory(bytes, Crc);
				// AssetBundle = AssetBundle.LoadFromMemory(bytes, Crc);
				if (AssetBundle == null)
				{
					if (AssetBundleUtils.GetLoadedBundleByPath(Uri, out AssetBundle assetBundle))
					{
						MyLogger.LogInfo($"retry reload assetbundle to resolve: {Uri}");
						if (assetBundle != null)
						{
							assetBundle.Unload(false);
						}

						AssetBundle = AssetBundle.LoadFromMemory(bytes, Crc);
					}
				}

				if (AssetBundle != null)
				{
					AssetBundleUtils.AddLiveBundle(AssetBundle);
					Result.IsOk = true;
				}
				else
				{
					Result.ErrorType = PipelineErrorType.DataIncorrect;
					Result.Msg = "Failed to load AssetBundle";
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