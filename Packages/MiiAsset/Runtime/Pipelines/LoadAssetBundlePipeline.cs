using System;
using System.IO;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOStreams;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundlePipeline : ILoadAssetBundlePipeline
	{
		protected LoadAssetBundleStream LoadStream;
		protected IRandomReadStream ReadStream;

		protected string Uri;
		protected uint Crc;

		public LoadAssetBundlePipeline Init(string uri, uint crc)
		{
			Uri = uri;
			Crc = crc;
			Result = new();
			this.Build();
			return this;
		}

		public void Dispose()
		{
			// if (LoadStream != null && ReadStream != null)
			// {
			// 	LoadStream.UnBindWriteStream(ReadStream);
			// }
			//
			// if (LoadStream != null)
			// {
			// 	LoadStream.Dispose();
			// 	LoadStream = null;
			// }
			//
			// if (ReadStream != null)
			// {
			// 	ReadStream.Dispose();
			// 	ReadStream = null;
			// }

			LoadStream = null;
			ReadStream = null;
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
			LoadStream = new LoadAssetBundleStream().Init(Uri);
			ReadStream = new ReadFileStream().Init(Uri);
			LoadStream.BindWriteStream(ReadStream);
		}

		public AssetBundle AssetBundle { get; set; }

		public IDisposable GetDisposable()
		{
			return LoadStream;
		}

		public IDownloadPipeline GetDownloadPipeline()
		{
			return null;
		}

		public Task<PipelineResult> Run()
		{
			if (AssetBundle == null)
			{
				ReadStream.Run();
				AssetBundle = AssetBundle.LoadFromStream(LoadStream, Crc);
				// if (Random.Range(0, 4) > 1)
				// {
				// 	AssetBundle = AssetBundle.LoadFromStream(LoadStream, Crc);
				// }
				if (AssetBundle == null)
				{
					MyLogger.LogError($"assetbundle is null: {Uri}");
					if (AssetBundleUtils.GetLoadedBundleByPath(Uri, out AssetBundle assetBundle))
					{
						MyLogger.Log($"retry reload assetbundle to resolve: {Uri}");
						if (assetBundle != null)
						{
							assetBundle.Unload(false);
						}

						LoadStream.Seek(0, SeekOrigin.Begin);
						AssetBundle = AssetBundle.LoadFromStream(LoadStream, Crc);
					}
				}

				if (AssetBundle != null)
				{
					AssetBundleUtils.AddLiveBundle(AssetBundle);
					Result.IsOk = true;
					// MyLogger.Log($"bundle-loaded: {Uri}");
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

			return Task.FromResult(Result);
		}

		public bool IsCached()
		{
			return ReadStream.Exist();
		}

		public PipelineProgress GetProgress()
		{
			return new PipelineProgress().Set01Progress(Result.IsOk);
		}
	}
}