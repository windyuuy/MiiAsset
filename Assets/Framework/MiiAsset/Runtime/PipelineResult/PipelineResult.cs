using System;
using UnityEngine;

namespace Framework.MiiAsset.Runtime
{
	public class PipelineResult
	{
		public virtual bool IsOk { get; set; } = false;
		public Exception Exception;

		public int Code;
		public string Msg;
		public PipelineErrorType ErrorType;

		public void Print()
		{
			if (!this.IsOk)
			{
				Debug.LogError($"Pipeline-Error: ErrorType: {ErrorType}, Code: {Code}, Msg: {Msg}");

				if (this.Exception != null)
				{
					Debug.LogException(this.Exception);
				}
			}
			else
			{
				Debug.Log($"Pipeline-Done");
			}
		}

		public void Merge(PipelineResult result)
		{
			this.IsOk = result.IsOk;
			this.Exception = result.Exception;
			this.ErrorType = result.ErrorType;
			this.Code = result.Code;
			this.Msg = result.Msg;
		}
	}

	public enum PipelineErrorType
	{
		NetError = 1,
		FileSystemError = 2,
		DataIncorrect,
	}
}