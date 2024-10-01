
using System;
using System.Threading.Tasks;

#if SUPPORT_RSGPROMISE
using RSG;

namespace RSG.Ext
{
	public class RPromise<T>
	{
		public Promise<T> promise;
		protected Action<T> succeed;
		protected Action<Exception> fail;

		public void Succeed(T ret)
		{
			succeed?.Invoke(ret);
		}

		public void Fail(Exception e)
		{
			fail?.Invoke(e);
		}

		public RPromise()
		{
			promise = new Promise<T>((resolve, reject) =>
			{
				succeed = resolve;
				fail = reject;
			});
		}

		public Task GetTask()
		{
			return this.promise.GetTask();
		}
	}

	public class RPromise
	{
		public Promise promise;
		protected Action succeed;
		protected Action<Exception> fail;

		public void Succeed()
		{
			succeed?.Invoke();
		}

		public void Fail(Exception e)
		{
			fail?.Invoke(e);
		}

		public RPromise()
		{
			promise = new Promise((resolve, reject) =>
			{
				succeed = resolve;
				fail = reject;
			});
		}

		public Task GetTask()
		{
			return this.promise.GetTask();
		}
	}
}
#endif
