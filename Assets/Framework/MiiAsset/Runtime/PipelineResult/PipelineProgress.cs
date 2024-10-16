using System;
using System.Collections.Generic;

namespace Framework.MiiAsset.Runtime
{
	public struct PipelineProgress
	{
		public ulong Total;

		public ulong Count
		{
			get
			{
				if (Total == 0)
				{
					return 0;
				}

				return CountInternal;
			}
			set
			{
				if (Total == 0 && CountInternal > 0)
				{
					CountInternal = 1;
				}
				else
				{
					CountInternal = value;
				}
			}
		}

		public bool IsDone
		{
			get
			{
				if (Total == 0)
				{
					return CountInternal == 1;
				}

				return Total == CountInternal;
			}
			internal set
			{
				if (Total == 0)
				{
					CountInternal = (ulong)(value ? 1 : 0);
				}
				else if (value)
				{
					CountInternal = Total;
				}
			}
		}

		private ulong CountInternal;

		public float Percent
		{
			get
			{
				if (Total == 0)
				{
					return CountInternal > 0 ? 1 : 0;
				}

				if (CountInternal == Total)
				{
					return 1;
				}

				return ((float)CountInternal) / Total;
			}
		}

		public static PipelineProgress Completed = new PipelineProgress().Set01Progress(true);

		public PipelineProgress(ulong total, ulong count)
		{
			this.CountInternal = count;
			this.Total = total;
		}

		public PipelineProgress Set01Progress(bool ok)
		{
			this.Total = 1000000;
			this.CountInternal = (ulong)(ok ? 1000000 : 0);
			return this;
		}

		public PipelineProgress SetDownloadedProgress(bool ok)
		{
			this.Total = 0;
			this.CountInternal = (ulong)(ok ? 1 : 0);
			return this;
		}

		public PipelineProgress Combine(PipelineProgress progress2)
		{
			var progress = new PipelineProgress
			{
				Total = Total + progress2.Total,
				CountInternal = Count + Math.Min(progress2.Total, progress2.Count),
			};
			progress.IsDone = IsDone && progress2.IsDone;

			return progress;
		}

		public static PipelineProgress CombineAll(IEnumerable<PipelineProgress> progresses)
		{
			var progress1 = new PipelineProgress().SetDownloadedProgress(false);
			var i = 0;
			foreach (var progress in progresses)
			{
				if (i == 0)
				{
					progress1 = progress;
				}
				else
				{
					progress1 = progress1.Combine(progress);
				}

				++i;
			}

			return progress1;
		}

		public PipelineProgress Complete(bool isOk = true)
		{
			if (isOk)
			{
				this.Count = this.Total;
			}

			return this;
		}

		public PipelineProgress SetProgress(float progress)
		{
			if (this.Total == 0)
			{
				if (progress >= 1)
				{
					this.Count = 1;
				}
				else
				{
					this.Count = 0;
				}
			}
			else
			{
				this.Count = (ulong)(this.Total * progress);
			}

			return this;
		}
	}
}