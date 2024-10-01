
using System;
using System.Collections.Generic;

namespace fsync
{

	using MonoBehaviour = UnityEngine.MonoBehaviour;
	using Debug = UnityEngine.Debug;
	using UnityEditor;
	//
	// 摘要:
	//     Enumeration specifying the current pause state of the Editor. See Also: PlayModeStateChange,
	//     EditorApplication.pauseStateChanged, EditorApplication.isPaused.
	public enum AppPauseState
	{
		//
		// 摘要:
		//     Occurs as soon as the Editor is paused, which may occur during either edit mode
		//     or play mode.
		Paused = 0,
		//
		// 摘要:
		//     Occurs as soon as the Editor is unpaused, which may occur during either edit
		//     mode or play mode.
		Unpaused = 1
	}

	public class MonoScheduler : MonoBehaviour
	{
		/// <summary>
		/// 创建一个持久调度对象
		/// </summary>
		/// <returns></returns>
		public static MonoScheduler Create()
		{
			var gameObject = new UnityEngine.GameObject();
			var script = gameObject.AddComponent(typeof(MonoScheduler)) as MonoScheduler;
			UnityEngine.GameObject.DontDestroyOnLoad(gameObject);
			gameObject.SetActive(true);
			script.Init();

			return script;
		}

		public void Init()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.pauseStateChanged += this._OnEditorPause;
#endif
		}
		~MonoScheduler()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.pauseStateChanged -= this._OnEditorPause;
#endif
		}

		// #region 
		// void Start()
		// {
		// 	Debug.Log($"sched-start, uiframeCount:{UnityEngine.Time.frameCount}");
		// }
		// #endregion

		#region FixedUpdate
		List<Action> fixedUpdates = new List<Action>();

		void FixedUpdate()
		{
			// Debug.Log($"poison fixedupdate, udt: {UnityEngine.Time.deltaTime}, ufc: {UnityEngine.Time.frameCount}");
			foreach (Action action in fixedUpdates)
			{
				try
				{
					action.Invoke();
				}
				catch (Exception e)
				{
					Debug.LogError("call update action failed");
					Debug.LogError(e);
				}
			}
		}

		/// <summary>
		/// 增加一个每帧调度
		/// </summary>
		/// <param name="action"></param>
		public void ScheduleFixed(Action action)
		{
			this.fixedUpdates.Add(action);
		}
		public void UnscheduleFixed(Action action)
		{
			this.fixedUpdates.Remove(action);
		}
		#endregion


		#region UIUpdate
		List<Action> uiUpdates = new List<Action>();

		void Update()
		{
			// Debug.Log($"poison uiupdate, udt: {UnityEngine.Time.deltaTime}, ufc: {UnityEngine.Time.frameCount}");
			foreach (Action action in uiUpdates)
			{
				try
				{
					action.Invoke();
				}
				catch (Exception e)
				{
					Debug.LogError("call update action failed");
					Debug.LogError(e);
				}
			}
		}

		/// <summary>
		/// 增加一个每帧调度
		/// </summary>
		/// <param name="action"></param>
		public void Schedule(Action action)
		{
			this.uiUpdates.Add(action);
		}

		public void Unschedule(Action action)
		{
			this.uiUpdates.Remove(action);
		}
		#endregion


		#region LateUpdate
		List<Action> lateUpdates = new List<Action>();
		void LateUpdate()
		{
			foreach (Action action in lateUpdates)
			{
				try
				{
					action.Invoke();
				}
				catch (Exception e)
				{
					Debug.LogError("call update action failed");
					Debug.LogError(e);
				}
			}
		}
		public void ScheduleLate(Action action)
		{
			this.lateUpdates.Add(action);
		}
		public void UnscheduleLate(Action action)
		{
			this.lateUpdates.Remove(action);
		}
		#endregion


		#region OnPause & OnResume
		List<Action> pauseCallbacks = new List<Action>();
		public void OnPause(Action action)
		{
			this.pauseCallbacks.Add(action);
		}

		List<Action> resumeCallbacks = new List<Action>();
		public void OnResume(Action action)
		{
			this.resumeCallbacks.Add(action);
		}

		protected void _OnPause(AppPauseState state)
		{
			if (state == AppPauseState.Paused)
			{
				foreach (Action action in pauseCallbacks)
				{
					try
					{
						action.Invoke();
					}
					catch (Exception e)
					{
						Debug.LogError("call update action failed");
						Debug.LogError(e);
					}
				}
			}
			else
			{
				foreach (Action action in resumeCallbacks)
				{
					try
					{
						action.Invoke();
					}
					catch (Exception e)
					{
						Debug.LogError("call update action failed");
						Debug.LogError(e);
					}
				}
			}
		}
#if UNITY_EDITOR
		protected void _OnEditorPause(UnityEditor.PauseState state)
		{
			if (state == UnityEditor.PauseState.Paused)
			{
				this._OnPause(AppPauseState.Paused);
			}
			else
			{
				this._OnPause(AppPauseState.Unpaused);
			}
		}
#endif
		private void OnApplicationPause(bool pause)
		{
			if (pause)
			{
				this._OnPause(AppPauseState.Paused);
			}
			else
			{
				this._OnPause(AppPauseState.Unpaused);
			}
		}

		#endregion

	}

}
