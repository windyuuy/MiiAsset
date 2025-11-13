using System;
using System.Collections.Generic;
using System.IO;
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

		async Task<PipelineResult> LoadCatalogTaskInternal()
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
					? new LoadRemoteTextFilePipeline().Init(internalCatalogHashUri, null)
					: new LoadTextFilePipeline().Init(internalCatalogHashUri))
				: null;

			var remoteCatalogHashUri =
				Convert2RemoteUri(remoteBaseUri, ToHashFileName(catalogName)); //ToHashFileName(remoteCatalogUri);
			using var loadRemoteHashPipeline = supportRemoteCatalog
				? new LoadRemoteTextFilePipeline().Init(remoteCatalogHashUri, null)
				: null;

			using ILoadTextAssetPipeline loadInternalCatalogPipeline = isInternalCatalogExist
				? (isInternalAsWebUri
					? new LoadRemoteCatalogPkgFromMemoryPipeline().Init(internalCatalogUri)
					: new LoadCatalogPkgPipeline().Init(internalCatalogUri))
				: null;

			LoadTextFilePipeline loadExternalHashPipeline = null;
			Task loadHashPipelinesTask;
			var existExternalCatalog = IOManager.LocalIOProto.Exists(externalCatalogHashUri) &&
			                           IOManager.LocalIOProto.Exists(externalCatalogUri);

			MyLogger.LogInfo(
				$"LoadCatalogOptions: internalCatalogUri:{internalCatalogUri}, remoteCatalogUri:{remoteCatalogUri}, externalCatalogUri:{externalCatalogUri}," +
				$" isInternalCatalogExist:{isInternalCatalogExist}, existExternalCatalog:{existExternalCatalog}, isInternalAsWebUri:{isInternalAsWebUri}");

			IEnumerable<Task<PipelineResult>> CollectValidPipelinesResult()
			{
				if (loadRemoteHashPipeline != null)
				{
					yield return loadRemoteHashPipeline.Run();
				}

				if (existExternalCatalog)
				{
					loadExternalHashPipeline = new LoadTextFilePipeline().Init(externalCatalogHashUri);
					yield return loadExternalHashPipeline.Run();
				}

				if (loadInternalHashPipeline != null)
				{
					yield return loadInternalHashPipeline.Run();
				}
			}

			loadHashPipelinesTask = Task.WhenAll(CollectValidPipelinesResult());
			//
			// if (existExternalCatalog)
			// {
			// 	loadExternalHashPipeline = new LoadTextFilePipeline().Init(externalHashUri);
			// 	if (loadInternalHashPipeline != null)
			// 	{
			// 		loadHashPipelinesTask = Task.WhenAll(
			// 			loadInternalHashPipeline.Run(),
			// 			loadExternalHashPipeline.Run(),
			// 			loadRemoteHashPipeline.Run()
			// 		);
			// 	}
			// 	else
			// 	{
			// 		loadHashPipelinesTask = Task.WhenAll(
			// 			loadExternalHashPipeline.Run(),
			// 			loadRemoteHashPipeline.Run()
			// 		);
			// 	}
			// }
			// else
			// {
			// 	if (loadInternalHashPipeline != null)
			// 	{
			// 		loadHashPipelinesTask = Task.WhenAll(
			// 			loadInternalHashPipeline.Run(),
			// 			loadRemoteHashPipeline.Run()
			// 		);
			// 	}
			// 	else
			// 	{
			// 		loadHashPipelinesTask = Task.WhenAll(
			// 			loadRemoteHashPipeline.Run()
			// 		);
			// 	}
			// }

			LoadInternalCatalogPipeline = loadInternalCatalogPipeline;
			var loadInternalCatalogTask = loadInternalCatalogPipeline?.Run();

			ILoadTextAssetPipeline loadExternalCatalogPipeline;
			if (loadInternalHashPipeline == null && loadExternalHashPipeline == null)
			{
				loadExternalCatalogPipeline = supportRemoteCatalog
					? new LoadRemoteCatalogPkgPipeline().Init(remoteCatalogUri, externalCatalogUri, true)
					: null;
			}
			else
			{
				loadExternalCatalogPipeline = null;
			}

			await loadHashPipelinesTask;

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

			var remoteHash = loadRemoteHashPipeline?.Text;
			var needUpdateCatalog = false;
			var internalHash = loadInternalHashPipeline?.Text;
			if (remoteHash != null && ((loadExternalHashPipeline == null && internalHash != remoteHash) ||
			                           (loadExternalHashPipeline != null &&
			                            loadExternalHashPipeline.Text != remoteHash)))
			{
				// load from remote
				needUpdateCatalog = true;
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				loadExternalCatalogPipeline ??= supportRemoteCatalog
					? new LoadRemoteCatalogPkgPipeline().Init(remoteCatalogUri, externalCatalogUri, true)
					: null;
			}
			else if (loadExternalHashPipeline != null
			         && loadExternalHashPipeline.Text == remoteHash && loadExternalHashPipeline.Text != internalHash)
			{
				// load from cache
				loadExternalCatalogPipeline = new LoadCatalogPkgPipeline().Init(externalCatalogUri);
			}
			else
			{
				// load from internal
				loadExternalCatalogPipeline = null;
			}

			LoadExternalCatalogPipeline = loadExternalCatalogPipeline;

			if (loadExternalCatalogPipeline != null)
			{
				if (loadInternalCatalogTask != null)
				{
					// await Task.WhenAll(loadInternalCatalogTask, loadExternalCatalogPipeline.Run());
					await loadInternalCatalogTask;
					await loadExternalCatalogPipeline.Run();
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
					MyLogger.LogException(exception);
					Result = new()
					{
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
			if (loadExternalCatalogPipeline != null)
			{
				try
				{
					externalCatalog = JsonUtility.FromJson<CatalogConfig>(loadExternalCatalogPipeline.Text);
				}
				catch (Exception exception)
				{
					var errMsg = $"{invalidJsonFormat}{externalCatalogUri}";
					MyLogger.LogError(errMsg);
					MyLogger.LogException(exception);
					Result = new()
					{
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

			// update hash file
			if (needUpdateCatalog)
			{
				_ = IOManager.LocalIOProto.WriteAllTextAsync(externalCatalogHashUri, remoteHash,
					EncodingExt.UTF8WithoutBom);
			}

			this.HandleCatalog(internalCatalog, externalCatalog, remoteBaseUri);

			loadExternalHashPipeline?.Dispose();
			loadExternalCatalogPipeline?.Dispose();

			Result = new()
			{
				IsOk = true,
			};
			return Result;
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
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
		}

		public async Task<PipelineResult> Run()
		{
			var updateCatalog = this.RunUpdateCatalog(RemoteBaseUri, CatalogName);
			return await updateCatalog;
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
	}
}