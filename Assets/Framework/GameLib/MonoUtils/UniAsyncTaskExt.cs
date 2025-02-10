using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MonoExtLib.Loom;
using UnityEngine;

namespace MonoExtLib.AsyncExt
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

		private static IEnumerator _runTaskIterForObjectType(ResourceRequest iter, TaskCompletionSource<object> taskSource)
		{
			yield return iter;
			taskSource.SetResult(iter.asset);
		}
		public static Task<object> GetTask(this ResourceRequest iter, MonoBehaviour comp = null)
		{
			var taskSource = new TaskCompletionSource<object>();
			(comp ?? LoomMG.SharedLoom).StartCoroutine(_runTaskIterForObjectType(iter, taskSource));
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

		private static IEnumerator RunTaskIter(IEnumerator iter, CancellationToken cancellationToken,
			TaskCompletionSource<bool> taskSource)
		{
			yield return iter;
			if (!cancellationToken.IsCancellationRequested)
			{
				taskSource.SetResult(true);
			}
		}

		public static Task GetTask(this IEnumerator iter, CancellationToken cancellationToken,
			MonoBehaviour comp = null)
		{
			var taskSource = new TaskCompletionSource<bool>();
			cancellationToken.Register(() => { taskSource.TrySetCanceled(cancellationToken); });
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

		/// <summary>
		/// 串联任务
		/// </summary>
		/// <param name="tasks"></param>
		/// <returns></returns>
		public static async Task Series(this IEnumerable<Task> tasks)
		{
			foreach (var task in tasks)
			{
				await task;
			}
		}

		public static async Task ContinueWithResult(this Task task, Action action)
		{
			await task;
			action();
		}

		public static async Task ContinueWithResult<T>(this Task<T> task, Action<T> action)
		{
			var result = await task;
			action(result);
		}

		public static async Task ContinueWithResult<T>(this Task<T> task, Func<T, Task> action)
		{
			var result = await task;
			await action(result);
		}
	}
}