using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonoExtLib.Loom
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

		private readonly List<Action> _taskList = new List<Action>();

		protected Thread MainThread;

		void Awake()
		{
			MainThread = Thread.CurrentThread;
#if UNITY_EDITOR
			Application.quitting += Quitting;
#endif
		}

		void Quitting()
		{
			Application.quitting -= Quitting;
			SceneManager.MoveGameObjectToScene(this.gameObject, SceneManager.GetActiveScene());
			GameObject.Destroy(this.gameObject);
		}

		private void OnDestroy()
		{
			Application.quitting -= Quitting;
		}

		protected readonly Queue<Action> TaskListCopy = new();

		void Update()
		{
			if (_taskList.Count > 0)
			{
				lock (_taskList)
				{
					if (_taskList.Count > 0)
					{
						foreach (var task in _taskList)
						{
							TaskListCopy.Enqueue(task);
						}

						_taskList.Clear();
					}
				}
			}

			while (TaskListCopy.Count > 0)
			{
				var task = TaskListCopy.Dequeue();
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
			lock (_taskList)
			{
				_taskList.Add(task);
			}
		}

		public void RunTask(Action task)
		{
			this.AddTask(task);

			if (MainThread == Thread.CurrentThread)
			{
				Update();
			}
		}
	}
}
