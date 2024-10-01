
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace System.Linq.MyExt
{
	public static class SystemLinqExt
	{
		public static void ForEach<T>(this T[] array, Action<T> action)
		{
			foreach (var item in array)
			{
				action(item);
			}
		}

		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enu, System.Action<T> action)
		{
			foreach (T item in enu) action(item);
			return enu; // make action Chainable/Fluent
		}

		public static void AddRange<T>(this Queue<T> queue, IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				queue.Enqueue(item);
			}
		}
		
		public static void AddRange<T>(this IList<T> ls, IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				ls.Add(item);
			}
		}

		public static Queue<T> ToQueue<T>(this IEnumerable<T> items, Queue<T> queue)
		{
			queue.Clear();
			foreach (var item in items)
			{
				queue.Enqueue(item);
			}

			return queue;
		}

		public static void AppendAll(this StringBuilder stringBuilder, params string[] strs)
		{
			foreach (var str in strs)
			{
				stringBuilder.Append(str);
			}
		}

		public static T MinBy<T>(this IEnumerable<T> source, Func<T, T, bool> compare)
		{
			if (source == null)
			{
				throw new NullReferenceException("source is null");
			}
			
			var defaultValue=default(T);
			T minItem=defaultValue;
			foreach (var item in source)
			{
				if(object.Equals(minItem,defaultValue))
				{
					minItem = item;
					continue;
				}
				
				// a > b, then replace
				if (compare(minItem, item))
				{
					minItem = item;
				}
			}

			return minItem;
		}

		public static T MaxBy<T>(this IEnumerable<T> source, Func<T, T, bool> compare)
		{
			if (source == null)
			{
				throw new NullReferenceException("source is null");
			}
			
			var defaultValue=default(T);
			T minItem=defaultValue;
			foreach (var item in source)
			{
				if(object.Equals(minItem,defaultValue))
				{
					minItem = item;
					continue;
				}
				
				// a > b, then replace
				if (compare(item, minItem))
				{
					minItem = item;
				}
			}

			return minItem;
		}
		
		public static T MaxBy<T>(this IEnumerable<T> source, Func<T, double> compare)
		{
			if (source == null)
			{
				throw new NullReferenceException("source is null");
			}
			
			T minItem = default(T);
			var maxValue = double.MinValue;
			foreach (var item in source)
			{
				// a > b, then replace
				var vTemp = compare(item);
				if (vTemp > maxValue)
				{
					maxValue = vTemp;
					minItem = item;
				}
			}

			return minItem;
		}
		
		/// <summary>Adds a collection to a hashset.</summary>
		/// <param name="hashSet">The hashset.</param>
		/// <param name="range">The collection.</param>
		public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> range)
		{
			foreach (T obj in range)
				hashSet.Add(obj);
		}

	}
}
