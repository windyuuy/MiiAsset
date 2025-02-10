using System;
using System.Timers;

namespace Lang.Time
{
	public class Timer
	{
		/// <summary>
		/// 在指定时间过后执行指定的表达式
		/// </summary>
		/// <param name="interval">事件之间经过的时间（以毫秒为单位）</param>
		/// <param name="action">要执行的表达式</param>
		public static System.Timers.Timer SetTimeout(Action action, double interval)
		{
			System.Timers.Timer timer = new System.Timers.Timer(interval);
			timer.Elapsed += delegate(object sender, System.Timers.ElapsedEventArgs e)
			{
				timer.Enabled = false;
				action();
			};
			timer.Enabled = true;
			return timer;
		}

		/// <summary>
		/// 在指定时间周期重复执行指定的表达式
		/// </summary>
		/// <param name="interval">事件之间经过的时间（以毫秒为单位）</param>
		/// <param name="action">要执行的表达式</param>
		public static System.Timers.Timer SetInterval(Action<ElapsedEventArgs> action, double interval)
		{
			System.Timers.Timer timer = new System.Timers.Timer(interval);
			timer.Elapsed += delegate(object sender, System.Timers.ElapsedEventArgs e) { action(e); };
			timer.Enabled = true;
			return timer;
		}

		public static void ClearTimeout(System.Timers.Timer timer)
		{
			timer.Stop();
			timer.Dispose();
		}

		public static void ClearInterval(System.Timers.Timer timer)
		{
			timer.Stop();
			timer.Dispose();
		}
	}
}