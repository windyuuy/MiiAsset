using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
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

	public class AddressReferStatus
	{
		public Dictionary<AACacheKey, LoadAddressStatus> AddressLoadMap = new();

		public LoadAddressStatus GetAddressStatus<T>(string address) where T : UnityEngine.Object
		{
			var key = new AACacheKey(address, typeof(T));
			return AddressLoadMap[key];
		}

		public void RegisterAddress<T>(string address, Task<T> task)
		{
			var key = new AACacheKey(address, typeof(T));
			if (!AddressLoadMap.TryGetValue(key, out var status))
			{
				status = new()
				{
					Address = address,
					Task = task,
					Asset = null,
					ReferCount = 1
				};
				AddressLoadMap.Add(key, status);
			}
			else
			{
				status.Task = task;
				++status.ReferCount;
			}
		}

		public void RegisterAsset<T>(string address, T asset)
		{
			if (asset != null)
			{
				var key = new AACacheKey(address, typeof(T));
				if (AddressLoadMap.TryGetValue(key, out var status))
				{
					status.Asset = asset;
				}
				else
				{
					MyLogger.LogError($"invalid address status: {address}");
				}
			}
			else
			{
				MyLogger.LogError($"load asset failed, maybe bundle is null: {address}");
			}
		}

		public async Task<bool> UnRegisterAsset(string address, Type type)
		{
			var key = new AACacheKey(address, type);
			if (AddressLoadMap.TryGetValue(key, out var status))
			{
				// if (status.ReferCount <= 0)
				{
					await status.Task;
					--status.ReferCount;
					if (status.ReferCount <= 0)
					{
						Debug.Assert(status.ReferCount == 0, "status.ReferCount==0");
						return AddressLoadMap.Remove(key);
					}
					// var asset = status.Asset;
				}
			}

			return false;
		}
	}
}