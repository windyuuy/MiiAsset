using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using UnityEngine;
using WeChatWASM;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadCatalogPkgPipeline : ILoadTextAssetPipeline
	{
		protected string CatalogUri;

		public LoadCatalogPkgPipeline Init(string catalogUri)
		{
			this.CatalogUri = catalogUri;
			Result = new();
			this.Build();
			return this;
		}

		public void Dispose()
		{
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
		}

		public async Task<PipelineResult> Run()
		{
			if (IOManager.LocalIOProto.Exists(CatalogUri))
			{
				try
				{
					var text = await IOManager.LocalIOProto.ReadCatalog(CatalogUri);
					Text = text;

					if (string.IsNullOrWhiteSpace(text))
					{
						Result.ErrorType = PipelineErrorType.DataIncorrect;
					}
					else
					{
						Result.IsOk = true;
					}
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					Result.ErrorType = PipelineErrorType.DataIncorrect;
					Result.Exception = ex;
				}
			}
			else
			{
				Result.ErrorType = PipelineErrorType.FileSystemError;
				Result.Msg = $"file not exist2: [{CatalogUri}]";
			}

			return Result;
		}

		public bool IsCached()
		{
			return false;
		}

		public PipelineProgress GetProgress()
		{
			return new PipelineProgress().Set01Progress(Result.IsOk);
		}

		public string Text { get; set; }
	}
}