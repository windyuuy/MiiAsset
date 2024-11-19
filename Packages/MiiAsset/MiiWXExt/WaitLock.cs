#if UNITY_WEBGL && SUPPORT_WECHATGAME
using System.IO;
using System.Threading.Tasks;

namespace MiiAsset.Runtime.IOManagers
{
	public struct WaitLock
	{
		private int _waitCount;
		private int _doneCount;
		private bool _isReady;
		private bool _anyError;
		private bool _dontThrow;

		private readonly TaskCompletionSource<bool> _tcs;

		public WaitLock(bool dontThrow)
		{
			_dontThrow = dontThrow;
			_waitCount = 0;
			_doneCount = 0;
			_anyError = false;
			_isReady = false;
			_tcs = new TaskCompletionSource<bool>();
		}

		public void FailOnce(string reason)
		{
			DoneOnce(false, reason);
		}

		public void OkOnce()
		{
			DoneOnce(true, "");
		}

		private void DoneOnce(bool ok, string reason)
		{
			_anyError = _anyError || (!ok);
			_doneCount++;
			if (_isReady && _waitCount == _doneCount)
			{
				if (_dontThrow || ok)
				{
					_tcs.SetResult(_anyError);
				}
				else
				{
					_tcs.SetException(new IOException(reason));
				}
			}
		}

		public void AddWait()
		{
			_waitCount++;
		}

		public Task Wait()
		{
			_isReady = true;
			if (_isReady && _waitCount == _doneCount && _tcs.Task.IsCompleted == false)
			{
				_tcs.SetResult(!_anyError);
			}

			return _tcs.Task;
		}
	}
}
#endif