using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Status
{
	public struct AsyncLoadingStatus<T>
	{
		public AsyncLoadingStatus(string address, Task<T> task, IAssetLoadStatus status)
		{
			this.Address = address;
			this.Task = task;
			this.Status = status;
			_completed = null;

			_ = Load(task);
		}

		async Task Load(Task task)
		{
			await task;
			_completed?.Invoke(this);
		}

		public string Address;
		public Task<T> Task;
		public IAssetLoadStatus Status;

		public string DebugName
		{
			get { return Address; }
		}

		public bool IsDone
		{
			get { return Status.Progress.IsDone; }
		}

		public T Result
		{
			get { return Task.Result; }
		}

		public Exception OperationException
		{
			get => Task.Exception;
		}

		public float PercentComplete
		{
			get
			{
				return Status.Progress.Percent;
			}
		}

		public bool IsCompletedSuccessfully => Status!=null && Status.Results.All(r => r.IsOk);

		private Action<AsyncLoadingStatus<T>> _completed;

		public void OnComplete(Action<AsyncLoadingStatus<T>> action)
		{
			_completed += action;
			if (Task.IsCompleted)
			{
				action?.Invoke(this);
			}
		}

		public void OffComplete(Action<AsyncLoadingStatus<T>> action)
		{
			_completed -= action;
		}

		public bool IsValid()
		{
			return !string.IsNullOrEmpty(Address);
		}
	}

	public interface IAssetLoadStatus
	{
		public PipelineProgress Progress { get; }
		public PipelineProgress DownloadProgress { get; }

		/// <summary>
		/// 待下载的大小
		/// </summary>
		public long DownloadSize { get; }

		public IEnumerable<PipelineResult> Results { get; }
	}

	public class AsyncOperationStatus : IAssetLoadStatus
	{
		protected AsyncOperation Op;
		public Exception Exception;

		public AsyncOperationStatus Set(AsyncOperation op)
		{
			Op = op;
			return this;
		}

		public PipelineProgress Progress
		{
			get
			{
				var progress = new PipelineProgress().Set01Progress(false);
				if (Op != null)
				{
					progress.SetProgress(Op.progress);
				}

				return progress;
			}
		}

		public PipelineProgress DownloadProgress =>
			new()
			{
				Total = 0,
				Count = 1,
			};

		public long DownloadSize => 0;

		public IEnumerable<PipelineResult> Results
		{
			get
			{
				yield return new PipelineResult
				{
					IsOk = Op.isDone,
					Exception = Exception,
					Code = 0,
					Msg = Op.isDone ? "" : "native-error",
					ErrorType = PipelineErrorType.FileSystemError,
					Status = PipelineStatus.Done,
				};
			}
		}
	}

	public class AssetDatabaseOpStatus : IAssetLoadStatus
	{
		public bool IsDone;

		public AssetDatabaseOpStatus()
		{
		}

		public AssetDatabaseOpStatus(bool isDone)
		{
			IsDone = isDone;
		}

		public PipelineProgress Progress
		{
			get { return new PipelineProgress().Set01Progress(IsDone); }
		}

		public PipelineProgress DownloadProgress
		{
			get { return new PipelineProgress().SetDownloadedProgress(IsDone); }
		}

		public long DownloadSize
		{
			get { return 0; }
		}

		public IEnumerable<PipelineResult> Results
		{
			get
			{
				yield return new PipelineResult
				{
					IsOk = IsDone,
					Exception = null,
					Code = 0,
					Msg = IsDone ? "" : "native-error",
					ErrorType = PipelineErrorType.FileSystemError,
					Status = PipelineStatus.Done,
				};
			}
		}
	}

	public class AssetLoadStatusGroup : IAssetLoadStatus
	{
		protected List<IAssetLoadStatus> StatusList = new();

		public PipelineProgress Progress
		{
			get { return PipelineProgress.CombineAll(StatusList.Select(status => status.Progress)); }
		}

		public PipelineProgress DownloadProgress
		{
			get { return PipelineProgress.CombineAll(StatusList.Select(status => status.DownloadProgress)); }
		}

		public long DownloadSize
		{
			get
			{
				long size = 0;
				foreach (var status in StatusList)
				{
					size += status.DownloadSize;
				}

				return size;
			}
		}

		public IEnumerable<PipelineResult> Results
		{
			get
			{
				foreach (var pipelineResultse in StatusList.Select(status => status.Results))
				{
					foreach (var pipelineResult in pipelineResultse)
					{
						yield return pipelineResult;
					}
				}
			}
		}

		internal AsyncOperationStatus AllocAsyncOperationStatus()
		{
			var status = new AsyncOperationStatus();
			this.Add(status);
			return status;
		}

		public void Add(IAssetLoadStatus status)
		{
			// Debug.Assert(!this.StatusList.Contains(status), "!this.StatusList.Contains(status)");
			this.StatusList.Add(status);
		}

		public AsyncOperationStatus AddAsyncOperationStatus(AsyncOperation op)
		{
			var loadStatus = new AsyncOperationStatus().Set(op);
			Add(loadStatus);
			return loadStatus;
		}

		public void Clear()
		{
			this.StatusList.Clear();
		}

		public void Print()
		{
			foreach (var result in this.Results)
			{
				result.Print();
			}
		}
	}
}