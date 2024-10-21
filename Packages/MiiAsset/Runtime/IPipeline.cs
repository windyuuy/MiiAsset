using System;
using System.Threading.Tasks;
using MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace MiiAsset.Runtime
{
	public interface IPipeline : IDisposable
	{
		public PipelineResult Result { get; }
		public void Build();

		public Task<PipelineResult> Run();

		public bool IsCached();
		// void Abort();

		public PipelineProgress GetProgress();
	}

	public interface IDownloadPipeline : IPipeline
	{
		void PresetDownloadSize(long fileSize);
	}

	public static class PipelineExt
	{
		public static PipelineProgress CombineProgress(this IPipeline pipeline1, IPipeline pipeline2)
		{
			var progress1 = pipeline1.GetProgress();
			var progress2 = pipeline2.GetProgress();
			return progress1.Combine(progress2);
		}

		public static PipelineProgress CombineProgress(this IPipeline pipeline1, params IPipeline[] pipelines)
		{
			var progress1 = pipeline1.GetProgress();
			var pipelineProgress = new PipelineProgress()
			{
				Total = progress1.Total,
				Count = progress1.Count,
			};
			foreach (var pipeline in pipelines)
			{
				var progress2 = pipeline.GetProgress();
				pipelineProgress = pipelineProgress.Combine(progress2);
			}

			return pipelineProgress;
		}
	}

	public interface ILoadTextAssetPipeline : IPipeline
	{
		public string Text { get; }
	}

	public interface ILoadAssetBundlePipeline : IPipeline
	{
		public AssetBundle AssetBundle { get; }
		public IDisposable GetDisposable();
		IDownloadPipeline GetDownloadPipeline();
	}
}