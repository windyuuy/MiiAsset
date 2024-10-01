using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GameLib.MonoUtils
{
	public class MyLoom : MonoBehaviour
	{

		public static MyLoom CreateOne()
		{
			var obj = new GameObject("MySharedLoom");
			var inst = obj.AddComponent<MyLoom>();
			GameObject.DontDestroyOnLoad(inst);
			return inst;
		}

		List<Action> taskList = new List<Action>();

		protected Thread mainThread;
		void Awake()
		{
			mainThread = Thread.CurrentThread;
		}
		protected Queue<Action> taskListCopy = new();
		void Update()
		{
			if (taskList.Count > 0)
			{
				lock (taskList)
				{
					if (taskList.Count > 0)
					{
						foreach (var task in taskList)
						{
							taskListCopy.Enqueue(task);
						}
						taskList.Clear();
					}
				}
			}

			while (taskListCopy.Count > 0)
			{
				var task = taskListCopy.Dequeue();
				try
				{
					task.Invoke();
				}
				catch (Exception e)
				{
					Debug.LogError(e);
				}
			}
		}

		public void AddTask(Action task)
		{
			lock (taskList)
			{
				taskList.Add(task);
			}
		}

		public void RunTask(Action task)
		{
			this.AddTask(task);

			if (mainThread == Thread.CurrentThread)
			{
				Update();
			}
		}
	}
}
