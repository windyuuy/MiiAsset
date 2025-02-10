using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonoExtLib.AsyncExt
{
	public class TaskEnumerator : IEnumerator
	{
		private Task _task;
		protected bool IsTaskDone = false;

		public TaskEnumerator(Task task)
		{
			this._task = task;
		}

		public object Current
		{
			get { return null; }
		}

		public bool MoveNext()
		{
			return !_task.IsCompleted;
		}

		public void Reset()
		{
			if (_task.Status != TaskStatus.Running)
			{
				_task.Start();
			}
			else
			{
				throw new Exception("one task cannot be start twice");
			}
		}
	}

	public class TaskEnumerator<T> : IEnumerator<T>, IEnumerable<T>
	{
		private Task<T> _task;
		protected bool IsTaskDone = false;

		public TaskEnumerator(Task<T> task)
		{
			this._task = task;
		}

		public T Current
		{
			get
			{
				if (_task.IsCompleted)
				{
					return _task.Result;
				}
				else
				{
					return default(T);
				}
			}
		}

		object IEnumerator.Current
		{
			get
			{
				if (_task.IsCompleted)
				{
					return _task.Result;
				}
				else
				{
					return default(T);
				}
			}
		}

		public bool MoveNext()
		{
			return !_task.IsCompleted;
		}

		public void Reset()
		{
			if (_task.Status != TaskStatus.Running)
			{
				_task.Start();
			}
			else
			{
				throw new Exception("one task cannot be start twice");
			}
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this;
		}
	}
}
