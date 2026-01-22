using System;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.Encrypt;
using MiiAsset.Runtime.IOManagers;
using UnityEngine;
using UnityEngine.Profiling;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromLocalBytesPipeline : ILoadAssetBundlePipeline
	{
		protected string Uri;
		protected uint Crc;
		protected bool IsEncrypt;
		protected ulong PredictFileSize;

		public LoadAssetBundleFromLocalBytesPipeline Init(string uri, uint crc, bool isEncrypt, ulong predictFileSize)
		{
			Uri = uri;
			Crc = crc;
			IsEncrypt = isEncrypt;
			PredictFileSize = predictFileSize;
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

		public void Invalidate()
		{
			if (AssetBundle != null)
			{
				AssetBundle.Unload(true);
				AssetBundle = null;
			}
		}

		public async Task<PipelineResult> Run()
		{
			if (AssetBundle == null)
			{
				var bytes = await IOManager.LocalIOProto.ReadAllBytesAsync(Uri);
				if (bytes.Length != (int)PredictFileSize)
				{
					Result.ErrorType = PipelineErrorType.DataIncorrect;
					Result.Msg = $"File Length unmatched: {bytes.Length}!={PredictFileSize}";
				}
				else
				{
					if (IsEncrypt)
					{
						SharedEncrypt.Encryptor.Encrypt(bytes, 0, 0, bytes.Length);
					}

					AssetBundle = AssetBundle.LoadFromMemory(bytes, Crc);
					if (AssetBundle == null)
					{
						var bytes2 = await IOManager.LocalIOProto.ReadAllBytesAsync(Uri);
						var exist = IOManager.LocalIOProto.Exists(Uri);
						Debug.LogError(
							$"Failed to Load AssetBundle firstTime: {Uri}, {exist}, {bytes.Length},{bytes2.Length}, [{string.Join(",", bytes2)}]");
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