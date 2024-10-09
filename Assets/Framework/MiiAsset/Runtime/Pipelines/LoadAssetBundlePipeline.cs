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
			LoadStream = new LoadAssetBundleStream().Init(Uri);
			ReadStream = new ReadFileStream().Init(Uri);
			LoadStream.BindWriteStream(ReadStream);
		}

		public AssetBundle AssetBundle;

		public Task<PipelineResult> Run()
		{
			if (AssetBundle == null)
			{
				ReadStream.Run();
				AssetBundle = AssetBundle.LoadFromStream(LoadStream);
				if (AssetBundle != null)
				{
					Result.IsOk = true;
					// Debug.Log($"bundle-loaded: {Uri}");
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