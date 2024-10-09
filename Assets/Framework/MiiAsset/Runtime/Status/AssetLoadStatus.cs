using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.MiiAsset.Runtime.Status
{
	public interface IAssetLoadStatus
	{
		public PipelineProgress Progress { get; }
		public PipelineProgress DownloadProgress { get; }
		public long DownloadSize { get; }
	}

	public class AsyncOperationStatus : IAssetLoadStatus
	{
		protected AsyncOperation Op;

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

		internal AsyncOperationStatus AllocAsyncOperationStatus()
		{
			var status = new AsyncOperationStatus();
			this.Add(status);
			return status;
		}

		public void Add(IAssetLoadStatus status)
		{
			this.StatusList.Add(status);
		}

		public void AddAsyncOperationStatus(AsyncOperation op)
		{
			Add(new AsyncOperationStatus().Set(op));
		}
	}
}