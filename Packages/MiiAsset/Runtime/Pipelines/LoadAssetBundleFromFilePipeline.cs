using System.IO;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromFilePipeline : IPipeline
	{
		protected string Uri;
		protected uint Crc;
		public PipelineResult Result { get; set; }

		public LoadAssetBundleFromFilePipeline Init(string uri, uint crc)
		{
			Uri = uri;
			Crc = crc;
			Result = new();
			this.Build();
			return this;
		}

		public AssetBundle AssetBundle;

		public void Dispose()
		{
			AssetBundle.Unload(true);
			AssetBundle = null;
		}

		public void Build()
		{
		}

		public Task<PipelineResult> Run()
		{
			if (IOManager.LocalIOProto.Exists(Uri))
			{
				AssetBundle = AssetBundle.LoadFromFile(Uri, Crc);
				// AssetBundle = AssetBundle.LoadFromFile(Uri, Crc);
				if (AssetBundle == null)
				{
					if (AssetBundleUtils.GetLoadedBundleByPath(Uri, out AssetBundle assetBundle))
					{
						MyLogger.Log($"retry reload assetbundle to resolve: {Uri}");
						if (assetBundle != null)
						{
							assetBundle.Unload(false);
						}

						AssetBundle = AssetBundle.LoadFromFile(Uri, Crc);
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
				}
			}
			else
			{
				Result.ErrorType = PipelineErrorType.FileSystemError;
			}

			return Task.FromResult(Result);
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