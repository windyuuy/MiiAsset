using System;
using MiiAsset.Runtime.Adapter;
using UnityEngine.Networking;

namespace MiiAsset.Runtime
{
	public enum PipelineStatus
	{
		Init,
		Running,
		Done,
	}

	public class PipelineResult
	{
		public PipelineResult()
		{
		}

		public virtual bool IsOk { get; set; } = false;
		public bool IsDone => Status == PipelineStatus.Done;
		public Exception Exception;

		public long Code;
		public string Msg;
		public PipelineErrorType ErrorType;
		public PipelineStatus Status = PipelineStatus.Init;

		public void Print()
		{
			if (!this.IsOk)
			{
				MyLogger.LogError($"Pipeline-Error: ErrorType: {ErrorType}, Code: {Code}, Msg: {Msg}");

				if (this.Exception != null)
				{
					MyLogger.LogException(this.Exception);
				}
			}
			else
			{
				MyLogger.Log($"Pipeline-Done");
			}
		}

		public void Merge(PipelineResult result)
		{
			this.Status = result.Status;
			this.IsOk = result.IsOk;
			this.Exception = result.Exception;
			this.ErrorType = result.ErrorType;
			this.Code = result.Code;
			this.Msg = result.Msg;
		}

		public void SetWithUwr(UnityWebRequest uwr)
		{
			var isOk = uwr.result == UnityWebRequest.Result.Success;
			this.IsOk = isOk;
			this.Exception = isOk ? null : new Exception(uwr.error);
			this.Code = (int)uwr.responseCode;
			this.Msg = uwr.error;
			this.ErrorType = PipelineErrorType.NetError;
			this.Status = PipelineStatus.Done;
		}

		public void SetOk()
		{
			this.IsOk = true;
			this.Exception = null;
			this.Code = 0;
			this.Msg = null;
			this.Status = PipelineStatus.Done;
		}
	}

	public enum PipelineErrorType
	{
		NetError = 1,
		FileSystemError = 2,
		DataIncorrect = 4,
		CatalogIncorrect = 5,
	}
}