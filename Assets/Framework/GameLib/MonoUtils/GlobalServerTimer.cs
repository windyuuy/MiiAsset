using System;

namespace Framework.GameLib.TimerUtils
{
	public class GlobalServerTimer
	{
		// 跨天刷新时区
		public static int RegionTime = 0;
		
		//客户端与服务器时间偏移
		private static long _serverTimeOffsetByClient;

		/// <summary>
		/// 初始化服务器当前时间，并与当前客户端时间记录时间偏移
		/// </summary>
		/// <param name="serverTime"></param>
		public static void SetServerTime(long serverTime)
		{
			var toNow = DateTime.UtcNow.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0);
			long current = Convert.ToInt64(toNow.TotalSeconds);
			_serverTimeOffsetByClient = serverTime - current;
		}
		
		/// <summary>
		/// 获取当前的服务器时间(秒级)
		/// </summary>
		/// <returns></returns>
		public static long GetCurrentServerTime()
		{
			var toNow = DateTime.UtcNow.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return Convert.ToInt64(toNow.TotalSeconds) + _serverTimeOffsetByClient;
		}
		
		/// <summary>
		/// 获得当前utc0点日期
		/// </summary>
		/// <returns></returns>
		public static DateTime GetCurrentDataTimeUTC0()
		{
			return GetDateTimeUTC0(GetCurrentServerTime());
		}

		public static DateTime GetDateTimeUTC0(long timestamp)
		{
			DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp).ToLocalTime();
			return dateTime;
		}
		
		// 跨天刷新时间判断
		public static bool CheckCrossedDay(long lastTimeStamp)
		{
			DateTime current = GetCurrentDataTimeUTC0();
			DateTime last = GetDateTimeUTC0(lastTimeStamp);
			if (current.Day == last.Day)
			{
				return last.Hour < RegionTime && current.Hour >= RegionTime;
			}
			else if (current.Day > last.Day)
			{
				return current.Hour >= RegionTime;
			}

			return false;
		}

		/// <summary>
		/// 【秒级】获取时间（北京时间）
		/// </summary>
		/// <param name="timestamp">10位时间戳</param>
		public static DateTime GetDateTimeSeconds(long timestamp)
		{
			long begtime = timestamp * 10000000;
			DateTime dt_1970 = new DateTime(1970, 1, 1, 8, 0, 0);
			long tricks_1970 = dt_1970.Ticks;//1970年1月1日刻度
			long time_tricks = tricks_1970 + begtime;//日志日期刻度
			DateTime dt = new DateTime(time_tricks);//转化为DateTime
			return dt;
		}
	}
}