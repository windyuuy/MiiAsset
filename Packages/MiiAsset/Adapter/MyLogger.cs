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

		public enum LogLevel
		{
			Error,
			Info,
			Debug,
		}

		public static LogLevel Level = LogLevel.Info;

		public static void Log(object message)
		{
			if (Level >= LogLevel.Debug)
			{
				Debug.Log($"-[mii]{message}");
			}
		}

		public static void LogInfo(object message)
		{
			if (Level >= LogLevel.Info)
			{
				Debug.Log($"-[mii]{message}");
			}
		}

		public static void LogWarning(object message)
		{
			if (Level >= LogLevel.Error)
			{
				Debug.LogWarning($"-[mii]{message}");
			}
		}

		public static void LogError(object message)
		{
			if (Level >= LogLevel.Error)
			{
				Debug.LogError($"-[mii]{message}");
			}
		}

		public static void LogException(Exception exception, string tip)
		{
			if (Level >= LogLevel.Error)
			{
				// Debug.LogError(tip);
				if (exception == null)
				{
					Debug.LogError("exception is null");
				}
				else
				{
					Debug.LogException(exception);
				}
			}
		}

		public static bool Assert(bool b, string s)
		{
			Debug.Assert(b, $"-[mii]{s}");
			return b;
		}
	}
}