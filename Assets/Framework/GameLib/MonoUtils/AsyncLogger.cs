using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Game.Diagnostics;

namespace lang.log
{
	enum LogType
	{
		Log,
		Warn,
		Error
	}
	class LogJob
	{
		public string message;
		public LogType type;
	}

	public class AsyncLogger
	{
		protected static AsyncLogger _instance;
		public static AsyncLogger instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new AsyncLogger();
				}
				return _instance;
			}
		}

		private List<LogJob> logList = new List<LogJob>();
		protected bool isLive = false;

		public void StartAsyncWorkThread()
		{
			ThreadStart childRef = new ThreadStart(print);
			Thread childThread = new Thread(childRef);
			childThread.Start();
		}
		public void print()
		{
			while (isLive)
			{
				LogJob[] list;
				lock (logList)
				{
					list = logList.ToArray();
					logList.Clear();
				}
				foreach (var s in list)
				{
					switch (s.type)
					{
						case LogType.Log:
							Console.Log(s.message);
							break;
						case LogType.Warn:
							Console.LogWarning(s.message);
							break;
						case LogType.Error:
							Console.LogError(s.message);
							break;
					}
				}
				Thread.Sleep(1000);
			}
		}
		public void Log(string message)
		{
			if (isLive)
			{
				var job = new LogJob()
				{
					type = LogType.Log,
					message = message
				};
				lock (logList)
				{
					logList.Add(job);
				}
			}
			else
			{
				UnityEngine.Debug.Log(message);
			}
		}

		public void Warn(string message)
		{
			if (isLive)
			{
				var job = new LogJob()
				{
					type = LogType.Warn,
					message = message + '\n' + new StackTrace().ToString()
				};
				lock (logList)
				{
					logList.Add(job);
				}
			}
			else
			{
				UnityEngine.Debug.LogWarning(message);
			}
		}

		public void Error(string message)
		{
			if (isLive)
			{
				var job = new LogJob()
				{
					type = LogType.Error,
					message = message + '\n' + new StackTrace().ToString()
				};
				lock (logList)
				{
					logList.Add(job);
				}
			}
			else
			{
				UnityEngine.Debug.LogError(message);
			}
		}

		public void Destroy()
		{
			this.isLive = false;
		}
	}
}
