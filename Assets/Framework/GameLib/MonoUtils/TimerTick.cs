using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;

namespace lang.time
{
	class TimeoutInfo
	{
		public Action callback;
		public float stopTime;
		public int id;
	}

	public class TimerTick : MonoBehaviour
	{
		public static TimerTick _instance;

		public static TimerTick instance
		{
			get
			{
				if (_instance != null)
				{
					return _instance;
				}
				var obj = new GameObject();
				_instance = obj.AddComponent<TimerTick>();
				GameObject.DontDestroyOnLoad(_instance);
				return _instance;
			}
		}

		List<Action> callbackList = new List<Action>();

		List<TimeoutInfo> timeoutList = new List<TimeoutInfo>();
		int timeoutIndex = 0;

		//用于在主线程执行
		List<Action> taskList = new List<Action>();
		protected Thread mainThread;

		public TimerTick()
		{
			mainThread = Thread.CurrentThread;
		}

		void Update()
		{
			foreach (var callback in callbackList.ToArray())
			{
				try
				{
					callback();
				}
				catch (Exception e)
				{
					Debug.LogError(e.ToString());
				}
			}

			foreach (var timeout in timeoutList.ToArray())
			{
				if (Time.realtimeSinceStartup > timeout.stopTime)
				{
					try
					{
						timeout.callback();
					}
					catch (Exception e)
					{
						Debug.LogError(e.ToString());
					}
					timeoutList.Remove(timeout);
				}
			}

			Action[] taskList;
			lock (this.taskList)
			{
				taskList = this.taskList.ToArray();
				this.taskList.Clear();
			}

			foreach (var task in taskList)
			{
				try
				{
					task();
				}
				catch (Exception e)
				{
					Debug.LogError(e.ToString());
				}
			}
		}

		public void AddListener(Action callback)
		{
			callbackList.Add(callback);
		}

		public void RemoveListener(Action callback)
		{
			callbackList.Remove(callback);
		}


		public int SetTimeout(Action callback, int millisecond)
		{
			int id = timeoutIndex++;
			timeoutList.Add(new TimeoutInfo()
			{
				id = id,
				callback = callback,
				stopTime = Time.realtimeSinceStartup + millisecond / 1000.0f
			});
			return id;
		}

		public void ClearTimeout(int id)
		{
			for (int i = 0; i < timeoutList.Count; i++)
			{
				if (timeoutList[i].id == id)
				{
					timeoutList.RemoveAt(i);
					break;
				}
			}
		}

		public void RunInMainThread(Action task)
		{
			if (Thread.CurrentThread == mainThread)
			{
				try
				{
					task();
				}
				catch (Exception e)
				{
					Debug.LogError(e.ToString());
				}
				return;
			}

			lock (taskList)
			{
				taskList.Add(task);
			}
		}
	}
}
