using System.Collections.Generic;

namespace Lang.TypeHelper
{
	public static class ListExt
	{
		public static List<T> Clone<T>(this List<T> list)
		{
			var clone = new List<T>(list);
			return clone;
		}
	}
}
