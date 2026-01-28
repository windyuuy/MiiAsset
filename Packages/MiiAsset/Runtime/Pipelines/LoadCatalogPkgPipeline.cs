using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class LoadCatalogPkgPipeline : ILoadTextAssetPipeline
	{
		protected string CatalogUri;
		protected string ExternalHash;

		public LoadCatalogPkgPipeline Init(string catalogUri, string externalHash)
		{
			this.CatalogUri = catalogUri;
			this.ExternalHash = externalHash;
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

		private static bool CheckBytesContentHash(byte[] bytes, string hash128Str0)
		{
			var hash = SHA256.Create();
			byte[] hashByte = hash.ComputeHash(bytes);
			var hash128Str = BitConverter.ToString(hashByte).Replace("-", "").ToLower();
			var isMatched = hash128Str == hash128Str0;
			return isMatched;
		}

		public async Task<PipelineResult> Run()
		{
			if (IOManager.LocalIOProto.Exists(CatalogUri))
			{
				try
				{
					// 校验hash
					bool isMatched;
					if (ExternalHash != null)
					{
						isMatched = await IOManager.LocalIOProto.ReadAllBytesAsync(CatalogUri, bytes =>
						{
							var isMatched1 = CheckBytesContentHash(bytes, ExternalHash);
							return isMatched1;
						});
						if (!isMatched)
						{
							MyLogger.LogError($"File hash not matched, auto Invalidate: {CatalogUri}, {ExternalHash}");
							this.Invalidate();
						}
					}
					else
					{
						isMatched = true;
					}

					if (isMatched)
					{
						var text = await IOManager.LocalIOProto.ReadCatalog(CatalogUri);
						Text = text;

						if (string.IsNullOrWhiteSpace(text))
						{
							Result.ErrorType = PipelineErrorType.DataIncorrect;
							Result.Msg = $"file empty: {CatalogUri}, [{text}]";
						}
						else
						{
							Result.IsOk = true;
						}
					}
					else
					{
						Result.ErrorType = PipelineErrorType.DataIncorrect;
						Result.Msg = $"file hash not matched: {CatalogUri}, {ExternalHash}";
					}
				}
				catch (Exception ex)
				{
					MyLogger.LogException(ex, "e3");
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

		public void Invalidate()
		{
			Reset();
			DeleteCachedFile();
			Build();
		}

		private void DeleteCachedFile()
		{
			var exist = IOManager.LocalIOProto.Exists(CatalogUri);
			if (exist)
			{
				IOManager.LocalIOProto.Delete(CatalogUri);
			}
		}

		public string Text { get; set; }
	}
}