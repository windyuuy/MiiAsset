using System;

namespace GameLib.MonoUtils.SysExt
{
	public static class StringExt
	{
		public static string SubStringSafe(this string str, int index, int length)
		{
			return str.Substring(index, Math.Min(length, str.Length - index));
		}
	}
}