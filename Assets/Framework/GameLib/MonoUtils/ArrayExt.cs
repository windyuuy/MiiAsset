using System.Collections.Generic;
using System;
using System.Reflection;
using System.IO;

namespace Lang.TypeHelper
{
	public static class ArrayExt
	{
		public static void ForEach<T>(this T[] array, System.Action<T, int> action)
		{
			for (int i = 0; i < array.Length; i++)
			{
				var item = array[i];
				action((T)item, i);
			}
		}

		public static void ForEach<T>(this T[] array, System.Action<T> action)
		{
			for (int i = 0; i < array.Length; i++)
			{
				var item = array[i];
				action((T)item);
			}
		}

		public static T[] Add<T>(this T[] array, T item)
		{
			if (array == null)
				array = new T[] { };
			List<T> tmpList = new List<T>(array);
			tmpList.Add(item);
			array = tmpList.ToArray();
			return array;
		}

		public static void Clear<T>(this T[] array)
		{
			System.Array.Clear(array, 0, array.Length);
		}
	}
}