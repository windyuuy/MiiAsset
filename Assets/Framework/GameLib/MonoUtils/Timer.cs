using System;
using System.Collections.Generic;
using System.Timers;

namespace lang.time
{
	public class Timer
	{
		private static UInt16 timeIndex = 0;
		private static Dictionary<UInt16, System.Timers.Timer> timers = new Dictionary<UInt16, System.Timers.Timer>();

		/// <summary>
		/// 在指定时间过后执行指定的表达式
		/// </summary>
		/// <param name="interval">事件之间经过的时间（以毫秒为单位）</param>
		/// <param name="action">要执行的表达式</param>
		public static void SetTimeout(Action action, double interval)
		{
			UInt16 index = timeIndex++;
			System.Timers.Timer timer = new System.Timers.Timer(interval);
			timer.Elapsed += delegate(object sender, System.Timers.ElapsedEventArgs e)
			{
				timer.Enabled = false;
				timers.Remove(index);
				action();
			};
			timer.Enabled = true;
			timers[index] = timer;
		}

		/// <summary>
		/// 在指定时间周期重复执行指定的表达式
		/// </summary>
		/// <param name="interval">事件之间经过的时间（以毫秒为单位）</param>
		/// <param name="action">要执行的表达式</param>
		public static void SetInterval(Action<ElapsedEventArgs> action, double interval)
		{
			UInt16 index = timeIndex++;
			System.Timers.Timer timer = new System.Timers.Timer(interval);
			timer.Elapsed += delegate (object sender, System.Timers.ElapsedEventArgs e)
			{
				action(e);
			};
			timer.Enabled = true;
			timers[index] = timer;
		}

		public static void ClearTimer()
		{
			foreach (var kv in timers)
			{
				kv.Value.Stop();
			}

			timers.Clear();
		}
	}
}