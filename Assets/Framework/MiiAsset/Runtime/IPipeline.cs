using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.MiiAsset.Runtime
{
	public interface IPipeline: IDisposable
	{
		public void Build();

		public Task Run();

		public bool IsCached();
		// void Abort();
	}

	public interface ILoadTextAssetPipeline : IPipeline
	{
		public string Text { get; }
	}
	public interface ILoadAssetBundlePipeline : IPipeline
	{
		public AssetBundle AssetBundle { get; }
	}
}