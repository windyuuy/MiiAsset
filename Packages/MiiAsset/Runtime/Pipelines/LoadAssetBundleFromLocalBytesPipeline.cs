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
				AssetBundle = await IOManager.LocalIOProto.ReadAllBytesAsync(Uri, bytes =>
				{
					if (bytes.Length != (int)PredictFileSize)
					{
						Result.ErrorType = PipelineErrorType.DataIncorrect;
						Result.Msg = $"File Length unmatched: {bytes.Length}!={PredictFileSize}";
						return null;
					}
					else
					{
						if (IsEncrypt)
						{
							SharedEncrypt.Encryptor.Encrypt(bytes, 0, 0, bytes.Length);
						}

						var assetBundle = AssetBundle.LoadFromMemory(bytes, Crc);
						if (assetBundle == null)
						{
							var exist = IOManager.LocalIOProto.Exists(Uri);
							MyLogger.LogError(
								$"Failed to Load AssetBundle firstTime1: {Uri}, {exist}, {bytes.Length}");
						}

						return assetBundle;
					}
				});
				if (AssetBundle == null)
				{
					AssetBundle = await IOManager.LocalIOProto.ReadAllBytesAsync(Uri, bytes2 =>
					{
						MyLogger.LogError(
							$"Failed to Load AssetBundle firstTime2: {Uri},{bytes2.Length}, [{string.Join(",", bytes2)}]");
						if (AssetBundleUtils.GetLoadedBundleByPath(Uri, out AssetBundle assetBundle))
						{
							MyLogger.LogInfo($"retry reload assetbundle to resolve: {Uri}");
							// if (assetBundle != null)
							// {
							// 	assetBundle.Unload(false);
							// }
							//
							// var assetBundle2 = AssetBundle.LoadFromMemory(bytes2, Crc);
							// return assetBundle2;
							return assetBundle;
						}
						else
						{
							return null;
						}
					});
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