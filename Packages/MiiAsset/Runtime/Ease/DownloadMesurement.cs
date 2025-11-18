using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiiAsset.Runtime.Mesurement
{
	public class DonwloadedRecord
	{
		public float Timestamp;
		public long DownloadedSize;

		public DonwloadedRecord(float timestamp, long progressCount)
		{
			Timestamp = timestamp;
			DownloadedSize = progressCount;
		}
	}

	public class DownloadMesurement
	{
		public float MesureDuration = 2f;

		protected Queue<DonwloadedRecord> Records = new();
		protected Stack<DonwloadedRecord> Pool = new();

		public void AddRecord(PipelineProgress progress)
		{
			var timestamp = Time.realtimeSinceStartup;
			{
				while (Records.TryPeek(out var record))
				{
					var outdateTime = timestamp - MesureDuration;
					if (record.Timestamp < outdateTime)
					{
						record = Records.Dequeue();
						Pool.Push(record);
					}
					else
					{
						break;
					}
				}
			}

			if (!Pool.TryPop(out var record2))
			{
				record2 = new DonwloadedRecord(timestamp, (long)progress.Count);
			}
			else
			{
				record2.Timestamp = timestamp;
				record2.DownloadedSize = (long)progress.Count;
			}

			Records.Enqueue(record2);
		}

		public float DownloadSpeed
		{
			get
			{
				if (Records.TryPeek(out var record))
				{
					var lastRecord = Records.Last();
					var timestamp = Time.realtimeSinceStartup;
					var timestamp0 = record.Timestamp;
					var dt = timestamp - timestamp0;
					var bytesDiff = lastRecord.DownloadedSize - record.DownloadedSize;
					if (bytesDiff > 0)
					{
						var speed = bytesDiff / dt;
						return speed;
					}
					else
					{
						return 0f;
					}
				}
				else
				{
					return 0f;
				}
			}
		}

		public void Restart()
		{
			foreach (var record in Records)
			{
				Pool.Push(record);
			}

			Records.Clear();
		}
	}

	public static class DownloadMesurementUtils
	{
		public static string GetDownloadSpeedDisplayStr(float downloadSpeed)
		{
			const int GBi = 1000 * 1000 * 1000;
			const int MBi = 1000 * 1000;
			const int KBi = 1000;
			if (downloadSpeed == 0f)
			{
				return "0B/s";
			}
			else if (downloadSpeed >= GBi)
			{
				return $"{(downloadSpeed / GBi):f2}GB/s";
			}
			else if (downloadSpeed >= MBi)
			{
				return $"{(downloadSpeed / MBi):f2}MB/s";
			}
			else if (downloadSpeed >= KBi)
			{
				return $"{(downloadSpeed / KBi):f2}KB/s";
			}
			else
			{
				return $"{downloadSpeed:f2}B/s";
			}
		}
	}
}