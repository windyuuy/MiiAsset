using System.IO;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadAssetBundleFromFilePipeline: IPipeline
	{
		protected string Uri;

		public LoadAssetBundleFromFilePipeline Init(string uri)
		{
			Uri = uri;
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

		public Task Run()
		{
			AssetBundle = AssetBundle.LoadFromFile(Uri);
			return Task.FromResult(AssetBundle);
		}

		public bool IsCached()
		{
			return IOManager.LocalIOProto.Exists(Uri);
		}
	}
}