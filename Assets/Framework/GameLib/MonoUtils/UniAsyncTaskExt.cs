using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GameLib.MonoUtils
{
	public class UniTaskYield : CustomYieldInstruction
	{
		readonly Task _task;
		public UniTaskYield(Task task)
		{
			this._task = task;
		}
		public override bool keepWaiting => !_task.IsCompleted;
	}

	public static class UniAsyncTaskExt
	{
		private static IEnumerator RunTaskIter(YieldInstruction iter, TaskCompletionSource<bool> taskSource)
		{
			yield return iter;
			taskSource.SetResult(true);
		}
		public static Task GetTask(this YieldInstruction iter, MonoBehaviour comp = null)
		{
			var taskSource = new TaskCompletionSource<bool>();
			(comp ? comp : LoomMG.SharedLoom).StartCoroutine(RunTaskIter(iter, taskSource));
			return taskSource.Task;
		}
		private static IEnumerator RunTaskIter(IEnumerator iter, TaskCompletionSource<bool> taskSource)
		{
			yield return iter;
			taskSource.SetResult(true);
		}
		public static Task GetTask(this IEnumerator iter, MonoBehaviour comp = null)
		{
			var taskSource = new TaskCompletionSource<bool>();
			(comp ? comp : LoomMG.SharedLoom).StartCoroutine(RunTaskIter(iter, taskSource));
			return taskSource.Task;
		}
		private static IEnumerator RunTaskIter(IEnumerator iter, CancellationToken cancellationToken, TaskCompletionSource<bool> taskSource)
		{
			yield return iter;
			if (!cancellationToken.IsCancellationRequested)
			{
				taskSource.SetResult(true);
			}
		}
		public static Task GetTask(this IEnumerator iter, CancellationToken cancellationToken, MonoBehaviour comp = null)
		{
			var taskSource = new TaskCompletionSource<bool>();
			cancellationToken.Register(() =>
			{
				taskSource.TrySetCanceled(cancellationToken);
			});
			if (!cancellationToken.IsCancellationRequested)
			{
				(comp ? comp : LoomMG.SharedLoom).StartCoroutine(RunTaskIter(iter, cancellationToken, taskSource));
			}
			return taskSource.Task;
		}

		public static UniTaskYield ToYieldable(this Task task)
		{
			return new UniTaskYield(task);
		}

	}
}