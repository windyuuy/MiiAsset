using System;
using UnityEngine.Networking;

namespace MiiAsset.Runtime.Adapter
{
	public static class UwrUtils
	{
		public static bool IsUwrOk(this UnityWebRequest uwr)
		{
			var uwrResult = uwr.GetUwrResult();
			return uwrResult == UnityWebRequest.Result.Success;
		}

		public static UnityWebRequest.Result GetUwrResult(this UnityWebRequest uwr)
		{
			// TODO: uwr.result 获取抛异常
			try
			{
				return uwr.result;
			}
			catch (ArgumentNullException exception3)
			{
				MyLogger.LogException(exception3, "e49");
				// 此处可能下载已经完成, 但是内部状态异常, 先算作失败
				return UnityWebRequest.Result.DataProcessingError;
			}
		}
	}
}