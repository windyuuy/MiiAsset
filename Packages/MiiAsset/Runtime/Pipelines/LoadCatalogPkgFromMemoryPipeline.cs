using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadCatalogPkgFromMemoryPipeline : ILoadTextAssetPipeline
	{
		protected byte[] Bytes;
		protected string RemoteCatalogUri;

		public LoadCatalogPkgFromMemoryPipeline Init(string remoteCatalogUri, byte[] bytes)
		{
			this.Bytes = bytes;
			this.RemoteCatalogUri = remoteCatalogUri;
			Result = new();
			this.Build();
			return this;
		}

		public void Dispose()
		{
			Reset();
		}

		private void Reset()
		{
			Text = null;
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
		}

		public async Task<PipelineResult> Run()
		{
			try
			{
				MyLogger.LogInfo($"load catalog file: {Bytes.Length}, {RemoteCatalogUri}");
				try
				{
					try
					{
						var stream = new MemoryStream(Bytes);
						using var streamReader = new StreamReader(new BrotliStream(stream, CompressionMode.Decompress));
					#if UNITY_WEBGL
						// ReSharper disable once MethodHasAsyncOverload
						var text = streamReader.ReadToEnd();
					#else
						var text = await streamReader.ReadToEndAsync();
					#endif
						Text = text;
						MyLogger.LogInfo($"load catalog file done: {RemoteCatalogUri}");
					}
					catch (Exception exception)
					{
						MyLogger.LogException(exception, "e38");
					}

					if (string.IsNullOrWhiteSpace(Text))
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
					MyLogger.LogException(exception, "e39");
				}
			}
			catch (Exception ex)
			{
				MyLogger.LogException(ex, "e40");
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

		public void Invalidate()
		{
			Reset();
			Build();
		}

		public string Text { get; set; }
	}
}