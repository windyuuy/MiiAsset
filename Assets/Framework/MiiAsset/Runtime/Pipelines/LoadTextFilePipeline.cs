using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using Framework.MiiAsset.Runtime.IOStreams;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadTextFilePipeline : ILoadTextAssetPipeline
	{
		protected string Uri;

		public LoadTextFilePipeline Init(string uri)
		{
			Uri = uri;
			this.Result = new();
			this.Build();
			return this;
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
		}

		public string Text { get; set; }

		public async Task<PipelineResult> Run()
		{
			if (Text == null)
			{
				try
				{
					Text = await IOManager.LocalIOProto.ReadAllTextAsync(Uri, Encoding.UTF8);
					Result.IsOk = true;
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					Result.ErrorType = PipelineErrorType.FileSystemError;
					Result.Exception = ex;
				}
			}

			return Result;
		}

		public bool IsCached()
		{
			return IOManager.LocalIOProto.Exists(Uri);
		}

		public void Dispose()
		{
		}
	}
}