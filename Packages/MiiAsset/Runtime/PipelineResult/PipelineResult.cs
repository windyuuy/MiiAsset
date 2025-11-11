using System;
using System.Linq;
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

	public class PipelineResultGroup
	{
		public PipelineResult[] Results;

		private PipelineResultGroup()
		{
			Results = Array.Empty<PipelineResult>();
		}

		public static readonly PipelineResultGroup Succeed = new();

		public PipelineResultGroup(PipelineResult[] results)
		{
			Results = results;
		}

		public virtual bool IsOk => Results.All(result => result.IsOk);

		public void PrintError()
		{
			foreach (var result in Results)
			{
				if (!result.IsOk)
				{
					result.PrintError();
				}
			}
		}
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
		public string Uri;
		public PipelineErrorType ErrorType = PipelineErrorType.NoErrorYet;
		public PipelineStatus Status = PipelineStatus.Init;

		public void PrintError()
		{
			if (!this.IsOk)
			{
				MyLogger.LogError($"Pipeline-Error: ErrorType: {ErrorType}, Code: {Code}, Msg: {Msg}, Uri: {Uri}");

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
			var uwrError = uwr.error;
			var code = (int)uwr.responseCode;
			this.Exception = isOk ? null : new Exception(uwrError);
			this.Code = code;
			this.Msg = uwrError;
			this.ErrorType = PipelineErrorType.NetError;
			this.Status = PipelineStatus.Done;

			if (!isOk)
			{
				MyLogger.LogError($"download-failed: {code}, {uwrError}");
			}
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
		NoErrorYet = 0,
		NetError = 1,
		FileSystemError = 2,
		DataIncorrect = 4,
		CatalogIncorrect = 5,
	}
}