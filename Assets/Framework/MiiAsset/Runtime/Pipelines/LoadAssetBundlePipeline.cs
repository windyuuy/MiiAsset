using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundlePipeline: IPipeline
	{
		protected AssetBundleStream LoadStream;
		protected IRandomReadStream ReadStream;

		protected string Uri;

		public LoadAssetBundlePipeline Init(string uri)
		{
			Uri = uri;
			this.Build();
			return this;
		}

		public void Dispose()
		{
			LoadStream.UnBindWriteStream(ReadStream);
			LoadStream.Dispose();
			ReadStream.Dispose();
			LoadStream = null;
			ReadStream = null;
		}

		public void Build()
		{
			LoadStream = new AssetBundleStream();
			ReadStream = new ReadFileStream().Init(Uri);
			LoadStream.BindWriteStream(ReadStream);
		}

		public AssetBundle AssetBundle;
		public Task Run()
		{
			AssetBundle = AssetBundle.LoadFromStream(LoadStream);
			return Task.FromResult(AssetBundle);
		}

		public bool IsCached()
		{
			return ReadStream.Exist();
		}
	}
}