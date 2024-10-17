using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.IOManagers
{
	public struct BundleWebSemaphore : IDisposable
	{
		internal static Queue<TaskCompletionSource<bool>> Tasks = new();
		public static int MaxCount;

		internal static int PendingCount;

		internal static void Init(int count)
		{
			MaxCount = count;
		}

		internal static void Require()
		{
			for (var i = PendingCount; i < Math.Min(MaxCount, Tasks.Count); i++)
			{
				var ele = Tasks.Dequeue();
				++PendingCount;
				try
				{
					ele.SetResult(true);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}

		internal static void Respond()
		{
			--PendingCount;
			Require();
		}

		private readonly Task _task;

		private BundleWebSemaphore(bool needLock)
		{
			var ts = new TaskCompletionSource<bool>();
			Tasks.Enqueue(ts);
			_task = ts.Task;

			Require();
		}

		public void Dispose()
		{
			Respond();
		}

		public static async Task<BundleWebSemaphore> Wait()
		{
			var loc = new BundleWebSemaphore(true);
			await loc._task;
			return loc;
		}
	}
}