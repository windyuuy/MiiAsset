using System;

namespace GameLib.MonoUtils
{
	public static class DateExt
	{
		
		/// <summary>
		/// refer: https://en.wikipedia.org/wiki/ISO_8601
		/// </summary>
		/// <returns></returns>
		public static string ToUniversalIso8601(this DateTime dateTime)
		{
			return dateTime.ToUniversalTime().ToString("u").Replace(" ", "T");
		}
		
		/// <summary>
		/// refer: https://en.wikipedia.org/wiki/ISO_8601
		/// </summary>
		/// <returns></returns>
		public static string ToIso8601(this DateTime dateTime)
		{
			return dateTime.ToString("u").Replace(" ", "T");
		}
	}
}