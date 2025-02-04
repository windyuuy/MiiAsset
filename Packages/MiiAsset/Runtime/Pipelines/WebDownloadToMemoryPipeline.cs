﻿using System;
using System.Threading.Tasks;
using GameLib.MonoUtils;
using MiiAsset.Runtime.IOManagers;
using UnityEngine.Networking;

namespace MiiAsset.Runtime.IOStreams
{
	public class WebDownloadToMemoryPipeline : IDownloadPipeline
	{
		protected TaskCompletionSource<PipelineResult> Ts;

		protected string Uri;
		public byte[] Bytes;

		protected bool UseCache = false;
		
		public WebDownloadToMemoryPipeline Init(string uri)
		{
			Uri = uri;
			UseCache = true;
			this.Result = new();
			return this;
		}

		protected UnityWebRequest Uwr;
		public Task<PipelineResult> Run()
		{
			if (Ts == null)
			{
				async Task ReadInternal()
				{
					Result.Status = PipelineStatus.Running;
					Ts = new();
					var uwr = UnityWebRequest.Get(this.Uri);
					Uwr = uwr;

					IOManager.LocalIOProto.SetUwr(Uwr);
					var op = uwr.SendWebRequest();
					await op.GetTask();
					var code = uwr.responseCode;
					var msg = uwr.error;

					Bytes = uwr.downloadHandler.data;

					uwr.Dispose();
					uwr = null;

					Result.Code = (int)code;
					Result.IsOk = code == 200;
					Result.Msg = msg;
					if (!Result.IsOk)
					{
						Result.ErrorType = PipelineErrorType.NetError;
					}
					Result.Status = PipelineStatus.Done;

					Ts.SetResult(Result);
				}

				_ = ReadInternal();
			}

			return Ts.Task;
		}

		public void Dispose()
		{
			Bytes = null;
		}

		public PipelineResult Result { get; set; }

		public void Build()
		{
		}

		public bool IsCached()
		{
			return false;
		}

		protected PipelineProgress Progress = new PipelineProgress();

		protected long PresetSize = -1;
		public PipelineProgress GetProgress()
		{
			if (Result.Status == PipelineStatus.Init)
			{
				if (PresetSize >= 0)
				{
					Progress = new((ulong)PresetSize, 0);
				}
				else
				{
					Progress.Set01Progress(false);
				}
			}
			else if (Result.Status == PipelineStatus.Done)
			{
				Progress.Complete();
			}
			else
			{
				var uwrDownloadedBytes = Uwr.downloadedBytes;
				ulong waitDoneAddition = 100;
				Progress = new()
				{
					Total = Math.Max(uwrDownloadedBytes, (ulong)(uwrDownloadedBytes / Uwr.downloadProgress)) + waitDoneAddition,
					Count = uwrDownloadedBytes,
				};
			}

			return Progress;
		}

		public void PresetDownloadSize(long fileSize)
		{
			PresetSize = fileSize;
		}
	}
}