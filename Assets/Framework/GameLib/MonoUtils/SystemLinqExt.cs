using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoExtLib.LinqExt
{
	public static partial class SystemLinqExt
	{
		public static void ForEach<TSource>(this IEnumerable<TSource> source,
			Action<TSource, int> selector)
		{
			var i = 0;
			foreach (var item in source)
			{
				selector(item, i);
				i++;
			}
		}

		public static void ForEach<T>(this IEnumerable<T> source, System.Action<T> action)
		{
			foreach (T item in source)
			{
				action(item);
			}
		}

		public static void WhileEach<TSource>(this IEnumerable<TSource> source,
			Func<TSource, int, bool> selector)
		{
			var i = 0;
			foreach (var item in source)
			{
				if (selector(item, i))
				{
					break;
				}

				i++;
			}
		}

		public static void WhileEach<T>(this IEnumerable<T> source, System.Func<T, bool> action)
		{
			foreach (T item in source)
			{
				if (action(item))
				{
					break;
				}
			}
		}
		
		
		public static IEnumerable<T> ForEachIter<T>(this IEnumerable<T> source,
			Action<T, int> selector)
		{
			var i = 0;
			foreach (var item in source)
			{
				selector(item, i);
				yield return item;
				i++;
			}
		}

		public static IEnumerable<T> ForEachIter<T>(this IEnumerable<T> source, System.Action<T> action)
		{
			foreach (T item in source)
			{
				action(item);
				yield return item;
			}
		}

		public static IEnumerable<T> WhileEachIter<T>(this IEnumerable<T> source,
			Func<T, int, bool> selector)
		{
			var i = 0;
			foreach (var item in source)
			{
				if (selector(item, i))
				{
					yield return item;
					yield break;
				}
				yield return item;

				i++;
			}
		}

		public static IEnumerable<T> WhileEachIter<T>(this IEnumerable<T> source, System.Func<T, bool> action)
		{
			foreach (T item in source)
			{
				if (action(item))
				{
					yield return item;
					break;
				}
				yield return item;
			}
		}
		
		public static void Done<T>(this IEnumerable<T> source)
		{
			foreach (T unused in source)
			{
			}
		}

		public static IList<T> ForEach<T>(this IList<T> source, Action<T, int> call)
		{
			for (var i = 0; i < source.Count; i++)
			{
				var item = source[i];
				call(item, i);
			}

			return source;
		}

		public static IList<T> ForEach<T>(this IList<T> source, Action<T> call)
		{
			foreach (var item in source)
			{
				call(item);
			}

			return source;
		}

		public static T[] ForEach<T>(this T[] source, Action<T, int> call)
		{
			for (var i = 0; i < source.Length; i++)
			{
				var item = source[i];
				call(item, i);
			}

			return source;
		}

		public static T[] ForEach<T>(this T[] source, Action<T> action)
		{
			foreach (var item in source)
			{
				action(item);
			}

			return source;
		}

		public static void AddRange<T>(this Queue<T> queue, IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				queue.Enqueue(item);
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

		public static T MinItem<T>(this IEnumerable<T> source, Func<T, T, bool> compare)
		{
			if (source == null)
			{
				throw new NullReferenceException("source is null");
			}

			var defaultValue = default(T);
			T minItem = defaultValue;
			foreach (var item in source)
			{
				if (object.Equals(minItem, defaultValue))
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

		public static T MaxItem<T>(this IEnumerable<T> source, Func<T, T, bool> compare)
		{
			if (source == null)
			{
				throw new NullReferenceException("source is null");
			}

			var defaultValue = default(T);
			T minItem = defaultValue;
			foreach (var item in source)
			{
				if (object.Equals(minItem, defaultValue))
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

		public static T MaxItem<T>(this IEnumerable<T> source, Func<T, double> compare)
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

		public static IEnumerable<R> MergeGroup<T, R>(this IEnumerable<T> ts, Func<T, IEnumerable<R>> call)
		{
			var iter = Enumerable.Empty<R>();
			foreach (var t in ts)
			{
				iter = iter.Concat(call(t));
			}

			return iter;
		}
	}
}