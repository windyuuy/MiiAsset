using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Pipelines
{
	public class LoadCatalogPkgFromMemoryPipeline : ILoadTextAssetPipeline
	{
		protected byte[] Bytes;

		public LoadCatalogPkgFromMemoryPipeline Init(byte[] bytes)
		{
			this.Bytes = bytes;
			Result = new();
			this.Build();
			return this;
		}

		public void Dispose()
		{
			throw new System.NotImplementedException();
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
		}

		public async Task<PipelineResult> Run()
		{
			try
			{
				Debug.Log($"load catalog.zip: {Bytes.Length}");
				try
				{
					var stream = new MemoryStream(Bytes);
					using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
					var entry = zipArchive.GetEntry("catalog.json");
					Debug.Assert(entry != null, "entry!=null");
					using var streamReader = new StreamReader(entry.Open());
					var text = await streamReader.ReadToEndAsync();
					Text = text;
					Debug.Log("load catalog.zip done");
					
					if (string.IsNullOrWhiteSpace(text))
					{
						Result.ErrorType = PipelineErrorType.DataIncorrect;
					}
					else
					{
						Result.IsOk = true;
					}
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				Result.ErrorType = PipelineErrorType.DataIncorrect;
				Result.Exception = ex;
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