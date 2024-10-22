using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System;
using GameLib.MonoUtils;
// using RSG;
using UnityEngine;

public class TaskEnumerator : IEnumerator
{
	Task task;
	protected bool isTaskDone = false;
	public TaskEnumerator(Task task)
	{
		this.task = task;
	}
	public object Current
	{
		get
		{
			return null;
		}
	}

	public bool MoveNext()
	{
		return !task.IsCompleted;
	}

	public void Reset()
	{
		if (task.Status != TaskStatus.Running)
		{
			task.Start();
		}
		else
		{
			throw new Exception("one task cannot be start twice");
		}
	}
}

public class TaskEnumerator<T> : IEnumerator<T>, IEnumerable<T>
{
	Task<T> task;
	protected bool isTaskDone = false;
	public TaskEnumerator(Task<T> task)
	{
		this.task = task;
	}
	public T Current
	{
		get
		{
			if (task.IsCompleted)
			{
				return task.Result;
			}
			else
			{
				return default(T);
			}
		}
	}

	object IEnumerator.Current
	{
		get
		{
			if (task.IsCompleted)
			{
				return task.Result;
			}
			else
			{
				return default(T);
			}
		}
	}

	public bool MoveNext()
	{
		return !task.IsCompleted;
	}

	public void Reset()
	{
		if (task.Status != TaskStatus.Running)
		{
			task.Start();
		}
		else
		{
			throw new Exception("one task cannot be start twice");
		}
	}

	public void Dispose()
	{
		throw new NotImplementedException();
	}

	public IEnumerator<T> GetEnumerator()
	{
		return this;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this;
	}
}

public class TaskYield : CustomYieldInstruction
{
	Task task;
	public TaskYield(Task task)
	{
		this.task = task;
	}
	public override bool keepWaiting => !task.IsCompleted;
}

delegate void Resolve(object p);
delegate void Reject(Exception e);

public static class AsyncTaskExt
{
	// public static Task<T> GetTask<T>(this IPromise<T> promise)
	// {
	// 	return AsyncTask.Async(promise);
	// }
	// public static Task GetTask(this IPromise promise)
	// {
	// 	return AsyncTask.Async(promise);
	// }
	// public static Task<T> GetTask<T>(this IPromise<T> promise, CancellationToken cancellationToken)
	// {
	// 	return AsyncTask.Async(promise, cancellationToken);
	// }
	// public static Task GetTask(this IPromise promise, CancellationToken cancellationToken)
	// {
	// 	return AsyncTask.Async(promise, cancellationToken);
	// }
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
	
	private static IEnumerator _runTaskIter(YieldInstruction iter, TaskCompletionSource<bool> taskSource)
	{
		yield return iter;
		taskSource.SetResult(true);
	}
	public static Task GetTask(this YieldInstruction iter, MonoBehaviour comp = null)
	{
		var taskSource = new TaskCompletionSource<bool>();
		(comp ?? LoomMG.SharedLoom).StartCoroutine(_runTaskIter(iter, taskSource));
		return taskSource.Task;
	}
	private static IEnumerator _runTaskIter(IEnumerator iter, TaskCompletionSource<bool> taskSource)
	{
		yield return iter;
		taskSource.SetResult(true);
	}
	public static Task GetTask(this IEnumerator iter, MonoBehaviour comp = null)
	{
		var taskSource = new TaskCompletionSource<bool>();
		(comp ?? LoomMG.SharedLoom).StartCoroutine(_runTaskIter(iter, taskSource));
		return taskSource.Task;
	}
	private static IEnumerator _runTaskIter(IEnumerator iter, CancellationToken cancellationToken, TaskCompletionSource<bool> taskSource)
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
			(comp ?? LoomMG.SharedLoom).StartCoroutine(_runTaskIter(iter, cancellationToken, taskSource));
		}
		return taskSource.Task;
	}
	// /// <summary>
	// /// 支持异步转协程
	// /// </summary>
	// /// <param name="task"></param>
	// /// <typeparam name="T"></typeparam>
	// /// <returns></returns>
	// public static TaskEnumerator<T> ToEnumerable<T>(this IPromise<T> promise)
	// {
	// 	// TODO: 优化实现
	// 	return AsyncTaskExt.ToEnumerable(AsyncTask.Async(promise));
	// }
	// /// <summary>
	// /// 支持异步转协程
	// /// </summary>
	// /// <param name="task"></param>
	// /// <returns></returns>
	// public static TaskEnumerator ToEnumerable(this IPromise promise)
	// {
	// 	// TODO: 优化实现
	// 	return AsyncTaskExt.ToEnumerable(AsyncTask.Async(promise));
	// }

	/// <summary>
	/// 支持异步转协程
	/// </summary>
	/// <param name="task"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static TaskEnumerator<T> ToEnumerable<T>(this Task<T> task)
	{
		return new TaskEnumerator<T>(task);
	}
	/// <summary>
	/// 支持异步转协程
	/// </summary>
	/// <param name="task"></param>
	/// <returns></returns>
	public static TaskEnumerator ToEnumerable(this Task task)
	{
		return new TaskEnumerator(task);
	}

	public static TaskYield ToYieldable(this Task task)
	{
		return new TaskYield(task);
	}

	// public static IPromise ToPromise(this Task task)
	// {
	// 	return new Promise(async (resolve, reject) =>
	// 	{
	// 		try
	// 		{
	// 			await task;
	// 			resolve();
	// 		}
	// 		catch (Exception e)
	// 		{
	// 			reject(e);
	// 		}
	// 	});
	// }
	// public static IPromise<T> ToPromise<T>(this Task<T> task)
	// {
	// 	return new Promise<T>(async (resolve, reject) =>
	// 	{
	// 		try
	// 		{
	// 			var ret = await task;
	// 			resolve(ret);
	// 		}
	// 		catch (Exception e)
	// 		{
	// 			reject(e);
	// 		}
	// 	});
	// }
	// public static IPromise ToIPromise<T>(this Task<T> task)
	// {
	// 	return new Promise(async (resolve, reject) =>
	// 	{
	// 		try
	// 		{
	// 			await task;
	// 			resolve();
	// 		}
	// 		catch (Exception e)
	// 		{
	// 			reject(e);
	// 		}
	// 	});
	// }

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

public class AsyncTask
{
	// public Promise<T> ToPromise<T>(Task<T> task)
	// {
	// 	return new Promise<T>(async (resolve, reject) =>
	// 	{
	// 		try
	// 		{
	// 			var result = await task;
	// 			resolve(result);
	// 		}
	// 		catch (Exception e)
	// 		{
	// 			reject(e);
	// 		}
	// 	});
	// }
	//
	// public static T Await<T>(IPromise<T> promise)
	// {
	// 	var taskSource = new TaskCompletionSource<T>();
	// 	promise.Then((T value) =>
	// 	{
	// 		taskSource.SetResult(value);
	// 	}).Catch((Exception e) =>
	// 	{
	// 		taskSource.SetException(e);
	// 	});
	//
	// 	taskSource.Task.Wait();
	// 	return taskSource.Task.Result;
	// }
	//
	// /// <summary>
	// /// 将promise转换为异步任务
	// /// </summary>
	// /// <param name="promise"></param>
	// /// <typeparam name="T"></typeparam>
	// /// <returns></returns>
	// public static Task<T> Async<T>(IPromise<T> promise)
	// {
	// 	var taskSource = new TaskCompletionSource<T>();
	// 	promise.Then((T value) =>
	// 	{
	// 		taskSource.SetResult(value);
	// 	}).Catch((Exception e) =>
	// 	{
	// 		taskSource.SetException(e);
	// 	});
	//
	// 	return taskSource.Task;
	// }
	// /// <summary>
	// /// 将promise转换为异步任务
	// /// </summary>
	// /// <param name="promise"></param>
	// /// <typeparam name="T"></typeparam>
	// /// <returns></returns>
	// public static Task Async(IPromise promise)
	// {
	// 	var taskSource = new TaskCompletionSource<bool>();
	// 	promise.Then(() =>
	// 	{
	// 		taskSource.SetResult(true);
	// 	}).Catch((Exception e) =>
	// 	{
	// 		taskSource.SetException(e);
	// 	});
	//
	// 	return taskSource.Task;
	// }
	//
	// /// <summary>
	// /// 将promise转换为异步任务
	// /// </summary>
	// /// <param name="promise"></param>
	// /// <typeparam name="T"></typeparam>
	// /// <returns></returns>
	// public static Task<T> Async<T>(IPromise<T> promise,CancellationToken cancellationToken)
	// {
	// 	var taskSource = new TaskCompletionSource<T>();
	// 	cancellationToken.Register(() =>
	// 	{
	// 		taskSource.TrySetCanceled(cancellationToken);
	// 	});
	// 	if (!cancellationToken.IsCancellationRequested)
	// 	{
	// 		promise.Then((T value) =>
	// 		{
	// 			if (!cancellationToken.IsCancellationRequested)
	// 			{
	// 				taskSource.SetResult(value);
	// 			}
	// 		}).Catch((Exception e) =>
	// 		{
	// 			if (!cancellationToken.IsCancellationRequested)
	// 			{
	// 				taskSource.SetException(e);
	// 			}
	// 		});
	// 	}
	//
	// 	return taskSource.Task;
	// }
	// /// <summary>
	// /// 将promise转换为异步任务
	// /// </summary>
	// /// <param name="promise"></param>
	// /// <typeparam name="T"></typeparam>
	// /// <returns></returns>
	// public static Task Async(IPromise promise, CancellationToken cancellationToken)
	// {
	// 	var taskSource = new TaskCompletionSource<bool>();
	// 	cancellationToken.Register(() =>
	// 	{
	// 		taskSource.TrySetCanceled(cancellationToken);
	// 	});
	// 	if (!cancellationToken.IsCancellationRequested)
	// 	{
	// 		promise.Then(() =>
	// 		{
	// 			if (!cancellationToken.IsCancellationRequested)
	// 			{
	// 				taskSource.SetResult(true);
	// 			}
	// 		}).Catch((Exception e) =>
	// 		{
	// 			if (!cancellationToken.IsCancellationRequested)
	// 			{
	// 				taskSource.SetException(e);
	// 			}
	// 		});
	// 	}
	//
	// 	return taskSource.Task;
	// }

	public static async Task<T> LoomTask<T>(Task<T> task)
	{
		var result = await task;
		await JoinMainThread();
		return result;
	}

	public static Task<bool> JoinMainThread()
	{
		var taskSource = new TaskCompletionSource<bool>();
		LoomMG.SharedLoom.AddTask(() =>
		{
			taskSource.SetResult(true);
		});

		return taskSource.Task;
	}

	/// <summary>
	/// 生成异步任务
	/// </summary>
	/// <param name="action"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static Task<T> Run<T>(Action<Action<T>, Action<Exception>> action)
	{
		var task = New(action);
		task.Start();
		return task;
	}
	public static Task<T> New<T>(Action<Action<T>, Action<Exception>> action)
	{
		var taskSource = new TaskCompletionSource<T>();
		action.Invoke((T data) =>
		{
			taskSource.SetResult(data);
		}, (e) =>
		{
			taskSource.SetException(e);
		});
		return taskSource.Task;
	}
	public static Task Run(Action<Action, Action<Exception>> action)
	{
		var task = New(action);
		task.Start();
		return task;
	}
	public static Task New(Action<Action, Action<Exception>> action)
	{
		var taskSource = new TaskCompletionSource<object>();
		action.Invoke(() =>
		{
			taskSource.SetResult(null);
		}, (e) =>
		{
			taskSource.SetException(e);
		});
		return taskSource.Task;
	}
}
