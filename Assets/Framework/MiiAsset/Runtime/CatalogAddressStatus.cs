using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MiiAsset.Runtime
{
	public class LoadAddressStatus
	{
		public string Address;
		public Task Task;
		public object Asset;
		public int ReferCount = 0;
	}

	public class CatalogAddressStatus
	{
		public Dictionary<string, LoadAddressStatus> AddressLoadMap = new();

		public void RegisterAddress<T>(string address, Task<T> task)
		{
			if (!AddressLoadMap.TryGetValue(address, out var status))
			{
				status = new()
				{
					Address = address,
					Task = task,
					Asset = null,
					ReferCount = 1
				};
				AddressLoadMap.Add(address, status);
			}
			else
			{
				status.Task = task;
				++status.ReferCount;
			}
		}

		public void RegisterAsset<T>(string address, T asset)
		{
			AddressLoadMap[address].Asset = asset;
		}

		public async Task UnRegisterAsset(string address)
		{
			if (AddressLoadMap.TryGetValue(address, out var status))
			{
				// if (status.ReferCount <= 0)
				{
					await status.Task;
					--status.ReferCount;
					if (status.ReferCount <= 0)
					{
						Debug.Assert(status.ReferCount == 0, "status.ReferCount==0");
						AddressLoadMap.Remove(address);
					}
					// var asset = status.Asset;
				}
			}
		}
	}
}