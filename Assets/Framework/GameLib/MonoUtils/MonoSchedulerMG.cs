using fsync;

namespace GameLib.MonoUtils
{
	public class MonoSchedulerMG
	{
		public static MonoScheduler SharedMonoScheduler { get; }=MonoScheduler.Create();
	}
}