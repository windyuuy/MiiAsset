using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using UnityEngine;

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
			if (File.Exists(CatalogUri))
			{
				try
				{
					var stream = IOManager.LocalIOProto.OpenRead(CatalogUri);
					using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
					var entry = zipArchive.GetEntry("catalog.json");
					Debug.Assert(entry != null, "entry!=null");
					using var streamReader = new StreamReader(entry.Open());
					var text = await streamReader.ReadToEndAsync();
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
				Result.Msg = $"file not exist: {CatalogUri}";
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