using System.Collections.Generic;

namespace MiiAsset.Runtime
{
	public class PooledLinkedList<T>
	{
		public readonly LinkedList<T> Coll = new LinkedList<T>();
		private static readonly Stack<LinkedListNode<T>> Pool = new Stack<LinkedListNode<T>>();

		public LinkedListNode<T> AddLast(T item)
		{
			if (Pool.TryPop(out LinkedListNode<T> node))
			{
				node.Value = item;
			}
			else
			{
				node = new LinkedListNode<T>(item);
			}

			Coll.AddLast(node);
			return node;
		}

		public bool Remove(LinkedListNode<T> node)
		{
			if (node.Next == null && node.Previous == null && node.List == null)
			{
				// 已经移除过
				return false;
			}

			Coll.Remove(node);
			// node.Value = default;
			Pool.Push(node);
			return true;
		}

		public IEnumerable<LinkedListNode<T>> ToEnumerable()
		{
			var coll = Coll;
			var current = coll.First;
			while (current != null)
			{
				var next = current.Next;
				yield return current;
				current = next;
			}
		}
	}
}