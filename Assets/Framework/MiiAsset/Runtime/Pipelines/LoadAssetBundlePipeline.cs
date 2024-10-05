using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundlePipeline : IPipeline
	{
		protected LoadAssetBundleStream LoadStream;
		protected IRandomReadStream ReadStream;

		protected string Uri;

		public LoadAssetBundlePipeline Init(string uri)
		{
			Uri = uri;
			Result = new();
			this.Build();
			return this;
		}

		public void Dispose()
		{
			if (LoadStream != null && ReadStream != null)
			{
				LoadStream.UnBindWriteStream(ReadStream);
			}

			if (LoadStream != null)
			{
				LoadStream.Dispose();
				LoadStream = null;
			}

			if (ReadStream != null)
			{
				ReadStream.Dispose();
				ReadStream = null;
			}
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
			LoadStream = new LoadAssetBundleStream();
			ReadStream = new ReadFileStream().Init(Uri);
			LoadStream.BindWriteStream(ReadStream);
		}

		public AssetBundle AssetBundle;

		public Task<PipelineResult> Run()
		{
			AssetBundle = AssetBundle.LoadFromStream(LoadStream);
			if (AssetBundle != null)
			{
				Result.IsOk = true;
			}
			else
			{
				Result.ErrorType = PipelineErrorType.DataIncorrect;
			}

			return Task.FromResult(Result);
		}

		public bool IsCached()
		{
			return ReadStream.Exist();
		}
	}
}