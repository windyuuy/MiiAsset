using System.IO;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromFilePipeline : IPipeline
	{
		protected string Uri;
		public PipelineResult Result { get; set; }

		public LoadAssetBundleFromFilePipeline Init(string uri)
		{
			Uri = uri;
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
			if (File.Exists(Uri))
			{
				AssetBundle = AssetBundle.LoadFromFile(Uri);
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
				Result.ErrorType = PipelineErrorType.FileSystemError;
			}

			return Task.FromResult(Result);
		}

		public bool IsCached()
		{
			return IOManager.LocalIOProto.Exists(Uri);
		}
	}
}