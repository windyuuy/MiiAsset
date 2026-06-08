using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lang.Encoding;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.IOManagers;
using UnityEngine;

namespace MiiAsset.Runtime.Pipelines
{
	public class UpdateCatalogPipeline : IPipeline
	{
		public string CatalogName;
		public string InternalBaseUri;
		public string ExternalBaseUri;
		public string RemoteBaseUri;
		public string CatalogExt;

		public UpdateCatalogPipeline Init(string catalogName, string catalogExt, string internalBaseUri,
			string externalBaseUri,
			string remoteBaseUri)
		{
			CatalogName = catalogName;
			CatalogExt = catalogExt;
			InternalBaseUri = internalBaseUri;
			ExternalBaseUri = externalBaseUri;
			RemoteBaseUri = remoteBaseUri;
			IsHashLoaded = false;
			this.Build();
			return this;
		}

		protected bool IsHashLoaded;
		protected IPipeline LoadInternalCatalogPipeline;
		protected IPipeline LoadExternalCatalogPipeline;

		public CatalogConfig InternalCatalog;
		public CatalogConfig ExternalCatalog;
		public string SourceUri;

		protected Task<PipelineResult> LoadCatalogTask;

		private static string Convert2RemoteUri(string remoteBaseUri, string catalogName)
		{
			return RemoteUriHandler.ConvertRemoteUri(remoteBaseUri, catalogName);
		}

		private async Task<PipelineResult> LoadCatalogTaskInternal()
		{
			var catalogName = CatalogName;
			var internalCatalogUri = InternalBaseUri + catalogName;
			var remoteBaseUri = RemoteBaseUri;
			var supportRemoteCatalog = !string.IsNullOrEmpty(remoteBaseUri);
			var remoteCatalogUri = supportRemoteCatalog ? Convert2RemoteUri(remoteBaseUri, catalogName) : null;
			var externalCatalogUri = ExternalBaseUri + catalogName;
			var externalCatalogHashUri = ToHashFileName(externalCatalogUri);
			var isInternalAsWebUri = IOManager.LocalIOProto.IsWebUri(internalCatalogUri);
			// 暂时仅判断catalog文件是否存在来判定, 已经足够
			var isInternalCatalogExist = isInternalAsWebUri || IOManager.LocalIOProto.Exists(internalCatalogUri);
			MyLogger.LogInfo($"isInternalCatalogExist: {isInternalCatalogExist}, {isInternalAsWebUri}");
			var internalCatalogHashUri = ToHashFileName(internalCatalogUri);
			using ILoadTextAssetPipeline loadInternalHashPipeline = isInternalCatalogExist
				? (isInternalAsWebUri
					? new LoadRemoteTextFilePipeline().Init(internalCatalogHashUri, null) // jar://hash
					: new LoadTextFilePipeline().Init(internalCatalogHashUri)) // 包内文件hash
				: null;

			var remoteCatalogHashUri =
				Convert2RemoteUri(remoteBaseUri, ToHashFileName(catalogName)); //ToHashFileName(remoteCatalogUri);
			using var loadRemoteHashPipeline = supportRemoteCatalog
				? new LoadRemoteTextFilePipeline().Init(remoteCatalogHashUri, null) // https://hash
				: null;

			using ILoadTextAssetPipeline loadInternalCatalogPipeline = isInternalCatalogExist
				? (isInternalAsWebUri
					? new LoadRemoteCatalogPkgFromMemoryPipeline().Init(internalCatalogUri) // jar://catalog
					: new LoadCatalogPkgPipeline().Init(internalCatalogUri, null)) // 包内文件catalog
				: null;

			// 存在外部catalog文件缓存
			var existExternalCatalog = IOManager.LocalIOProto.Exists(externalCatalogHashUri) &&
			                           IOManager.LocalIOProto.Exists(externalCatalogUri);
			using LoadTextFilePipeline loadExternalHashPipeline =
				existExternalCatalog ? new LoadTextFilePipeline().Init(externalCatalogHashUri) : null;

			MyLogger.LogInfo(
				$"LoadCatalogOptions: internalCatalogUri:{internalCatalogUri}, remoteCatalogUri:{remoteCatalogUri}, externalCatalogUri:{externalCatalogUri}," +
				$" isInternalCatalogExist:{isInternalCatalogExist}, existExternalCatalog:{existExternalCatalog}, isInternalAsWebUri:{isInternalAsWebUri}");

			/// 加载三重hash值
			IEnumerable<Task<PipelineResult>> CollectValidPipelinesResult()
			{
				if (loadRemoteHashPipeline != null)
				{
					yield return loadRemoteHashPipeline.Run();
				}

				if (loadExternalHashPipeline != null)
				{
					yield return loadExternalHashPipeline.Run();
				}

				if (loadInternalHashPipeline != null)
				{
					yield return loadInternalHashPipeline.Run();
				}
			}

			Task loadHashPipelinesTask = Task.WhenAll(CollectValidPipelinesResult());

			LoadInternalCatalogPipeline = loadInternalCatalogPipeline;
			var loadInternalCatalogTask = loadInternalCatalogPipeline?.Run();

			await loadHashPipelinesTask;
			MyLogger.Log($"loadHashPipelinesTask-done");

			IsHashLoaded = true;

			if (loadInternalHashPipeline != null && loadInternalHashPipeline.Result.IsOk == false)
			{
				Result = loadInternalHashPipeline.Result;
				MyLogger.LogError($"LoadInternalHashPipeline-failed: {internalCatalogHashUri}");
				return Result;
			}

			if (loadRemoteHashPipeline != null && loadRemoteHashPipeline.Result.IsOk == false)
			{
				Result = loadRemoteHashPipeline.Result;
				MyLogger.LogError($"LoadRemoteHashPipeline-failed: {remoteCatalogHashUri}");
				return Result;
			}

			if (loadExternalHashPipeline != null && loadExternalHashPipeline.Result.IsOk == false)
			{
				Result = loadExternalHashPipeline.Result;
				MyLogger.LogError($"LoadExternalHashPipeline-failed: {externalCatalogHashUri}");
				return Result;
			}

			var internalHash = loadInternalHashPipeline?.Text;
			var remoteHash = loadRemoteHashPipeline?.Text;
			var externalHash = loadExternalHashPipeline?.Text;
			MyLogger.Log(
				$"UpdateCatalog: internalHash:{internalHash}, remoteHash:{remoteHash}, externalHash:{externalHash}");
			var needUpdateCatalog = false;

			ILoadTextAssetPipeline DetermineLoadExternalHashPipeline()
			{
				ILoadTextAssetPipeline loadExternalCatalogPipeline;
				if (remoteHash != null && ((loadExternalHashPipeline == null && internalHash != remoteHash) ||
				                           (loadExternalHashPipeline != null &&
				                            externalHash != remoteHash)))
				{
					// load from remote
					needUpdateCatalog = true;
					TryParseHashInfo(remoteHash, out var predictFileSize, out var remoteHashStr);

					// ReSharper disable once ConditionIsAlwaysTrueOrFalse
					loadExternalCatalogPipeline = supportRemoteCatalog
						? new LoadRemoteCatalogPkgPipeline().Init(remoteCatalogUri, externalCatalogUri, remoteHashStr,
							true,
							predictFileSize)
						: null;
				}
				else if (loadExternalHashPipeline != null
				         && externalHash == remoteHash && externalHash != internalHash)
				{
					// load from cache
					TryParseHashInfo(remoteHash, out var predictFileSize, out var externalHashStr);
					loadExternalCatalogPipeline =
						new LoadCatalogPkgPipeline().Init(externalCatalogUri, externalHashStr);
				}
				else
				{
					// load from internal
					loadExternalCatalogPipeline = null;
				}

				return loadExternalCatalogPipeline;
			}

			using var loadExternalCatalogPipeline = DetermineLoadExternalHashPipeline();

			LoadExternalCatalogPipeline = loadExternalCatalogPipeline;

			MyLogger.Log($"DetermineLoadExternalHashPipeline: {loadExternalCatalogPipeline?.GetType()?.FullName}");

			if (loadExternalCatalogPipeline != null)
			{
				if (loadInternalCatalogTask != null)
				{
					// await Task.WhenAll(loadInternalCatalogTask, loadExternalCatalogPipeline.Run());
					var loadExternalCatalogTask = loadExternalCatalogPipeline.Run();
					await loadInternalCatalogTask;
					await loadExternalCatalogTask;
				}
				else
				{
					await loadExternalCatalogPipeline.Run();
				}
			}
			else if (loadInternalCatalogTask != null)
			{
				await loadInternalCatalogTask;
			}
			else
			{
				var message = "no valid catalog to load";
				MyLogger.LogError(message);
				Result = new PipelineResult
				{
					IsOk = false,
					Msg = message,
					ErrorType = PipelineErrorType.NetError,
					Status = PipelineStatus.Done,
				};
				return Result;
			}

			MyLogger.Log($"LoadCatalogPipelines-done");

			if (loadInternalCatalogPipeline != null && loadInternalCatalogPipeline.Result.IsOk == false)
			{
				Result = loadInternalCatalogPipeline.Result;
				MyLogger.LogError($"LoadInternalCatalogPipeline-failed: {internalCatalogUri}");
				return Result;
			}

			if (loadExternalCatalogPipeline != null && loadExternalCatalogPipeline.Result.IsOk == false)
			{
				Result = loadExternalCatalogPipeline.Result;
				MyLogger.LogError($"LoadExternalCatalogPipeline-failed: {externalCatalogUri}");
				return Result;
			}

			CatalogConfig internalCatalog;
			const string invalidJsonFormat = "invalid catalog json format: ";
			if (loadInternalCatalogPipeline != null)
			{
				try
				{
					internalCatalog = JsonUtility.FromJson<CatalogConfig>(loadInternalCatalogPipeline.Text);
				}
				catch (Exception exception)
				{
					var errMsg = $"{invalidJsonFormat}{internalCatalogUri}";
					MyLogger.LogError(errMsg);
					MyLogger.LogException(exception, "e44");
					Result = new()
					{
						IsOk = false,
						Exception = exception,
						Msg = errMsg,
						ErrorType = PipelineErrorType.DataIncorrect,
						Uri = internalCatalogUri,
					};
					return Result;
				}
			}
			else
			{
				internalCatalog = null;
			}

			CatalogConfig externalCatalog;
			var externalCatalogText = loadExternalCatalogPipeline?.Text;
			if (loadExternalCatalogPipeline != null)
			{
				try
				{
					externalCatalog = JsonUtility.FromJson<CatalogConfig>(externalCatalogText);
				}
				catch (Exception exception)
				{
					var errMsg = $"{invalidJsonFormat}{externalCatalogUri}";
					MyLogger.LogError(errMsg);
					MyLogger.LogException(exception, "e45");
					Result = new()
					{
						IsOk = false,
						Exception = exception,
						Msg = errMsg,
						ErrorType = PipelineErrorType.DataIncorrect,
						Uri = $"{remoteCatalogUri}->{externalCatalogUri}",
					};
					return Result;
				}
			}
			else
			{
				externalCatalog = null;
			}

			MyLogger.Log($"LoadCatalogJsons-done");

			// update hash file
			if (needUpdateCatalog)
			{
				_ = IOManager.LocalIOProto.WriteAllTextAsync(externalCatalogHashUri, remoteHash,
					EncodingExt.UTF8WithoutBom);
			}

			MyLogger.Log($"WriteCatalogHash-done");

			this.HandleCatalog(internalCatalog, externalCatalog, remoteBaseUri);

			MyLogger.Log($"HandleCatalog-done");
			// loadExternalHashPipeline?.Dispose();
			// loadExternalCatalogPipeline?.Dispose();

			Result = new()
			{
				IsOk = true,
			};
			return Result;
		}

		private static readonly Regex ParseHashRegex = new Regex(@"(\w+),(\d+)");

		private static bool TryParseHashInfo(string remoteHash, out ulong predictFileSize, out string hashStr)
		{
			if (remoteHash == null)
			{
				predictFileSize = 0;
				hashStr = null;
				return false;
			}

			var m = ParseHashRegex.Match(remoteHash);
			if (m.Success)
			{
				if (ulong.TryParse(m.Groups[2].Value, out predictFileSize))
				{
					hashStr = m.Groups[1].Value;
					return true;
				}
				else
				{
					predictFileSize = 0;
					hashStr = null;
				}
			}
			else
			{
				predictFileSize = 0;
				hashStr = null;
			}

			return false;
		}

		public Task<PipelineResult> RunUpdateCatalog(string remoteBaseUri, string catalogName)
		{
			if (LoadCatalogTask == null || (LoadCatalogTask.IsCompleted && !Result.IsOk))
			{
				this.RemoteBaseUri = remoteBaseUri;
				this.CatalogName = catalogName;

				LoadCatalogTask = LoadCatalogTaskInternal();
			}

			return LoadCatalogTask;
		}

		private string ToHashFileName(string internalCatalogUri)
		{
			var index = internalCatalogUri.LastIndexOf(CatalogExt, StringComparison.Ordinal);
			string hashFileName;
			if (index >= 0)
			{
				hashFileName = internalCatalogUri[0..(index - 1)] + ".hash" +
				               internalCatalogUri[(index + CatalogExt.Length)..];
			}
			else
			{
				hashFileName = internalCatalogUri + ".hash";
			}

			MyLogger.LogInfo($"ToHashFileName: {internalCatalogUri}->{hashFileName}");
			return hashFileName;
		}

		private void HandleCatalog(CatalogConfig internalCatalog, CatalogConfig externalCatalog, string sourceUri)
		{
			if (!MyLogger.Assert(internalCatalog != null || externalCatalog != null,
				    "internalCatalog!=null||externalCatalog!=null"))
			{
				MyLogger.LogInfo(
					$"internalCatalog != null:{internalCatalog != null}, externalCatalog != null: {externalCatalog != null}");
			}

			this.InternalCatalog = internalCatalog;
			this.ExternalCatalog = externalCatalog;
			this.SourceUri = sourceUri;
		}

		public void Dispose()
		{
			if (LoadInternalCatalogPipeline != null)
			{
				LoadInternalCatalogPipeline.Dispose();
				LoadInternalCatalogPipeline = null;
			}

			if (LoadExternalCatalogPipeline != null)
			{
				LoadExternalCatalogPipeline.Dispose();
				LoadExternalCatalogPipeline = null;
			}

			LoadCatalogTask = null;
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
		}

		public async Task<PipelineResult> Run()
		{
			var result = await this.RunUpdateCatalog(RemoteBaseUri, CatalogName);
			return result;
		}

		public bool IsCached()
		{
			return false;
		}

		public PipelineProgress GetProgress()
		{
			if (LoadInternalCatalogPipeline == null || LoadExternalCatalogPipeline == null)
			{
				var pipelineProgress = new PipelineProgress().Set01Progress(false);
				if (IsHashLoaded)
				{
					pipelineProgress.Count = 1;
				}

				return pipelineProgress;
			}
			else
			{
				return LoadInternalCatalogPipeline.CombineProgress(LoadExternalCatalogPipeline);
			}
		}

		public void Invalidate()
		{
			LoadCatalogTask = null;

			if (LoadInternalCatalogPipeline != null)
			{
				LoadInternalCatalogPipeline.Invalidate();
			}

			if (LoadExternalCatalogPipeline != null)
			{
				LoadExternalCatalogPipeline.Invalidate();
			}
		}
	}
}