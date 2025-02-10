using FSync;

namespace MonoExtLib.StringExt
{
	public class MonoSchedulerMG
	{
		public static MonoScheduler SharedMonoScheduler { get; }=MonoScheduler.Create();
	}
}