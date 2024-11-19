using System;
using UnityEngine;

namespace MiiAsset.Runtime.Adapter
{
	public static class MyLogger
	{
		// public static Action<object> Log = Debug.Log;
		// public static Action<object> LogWarning = Debug.LogWarning;
		// public static Action<object> LogError = Debug.LogError;
		// public static Action<Exception> LogException = Debug.LogException;

		public static void Log(object message)
		{
			Debug.Log($"-[mii]{message}");
		}

		public static void LogWarning(object message)
		{
			Debug.LogWarning($"-[mii]{message}");
		}

		public static void LogError(object message)
		{
			Debug.LogError($"-[mii]{message}");
		}

		public static void LogException(Exception exception)
		{
			Debug.LogException(exception);
		}
	}
}