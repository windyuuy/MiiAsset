using System;
using UnityEngine;

namespace HatNetwork
{
	[Serializable]
	public class HatLoadState
	{
		public bool IsPreLoaded = false;
		public bool IsHotLoaded = false;
		/// <summary>
		/// 远端新版资源加载完毕
		/// </summary>
		public bool IsRemoteLoaded => IsPreLoaded && IsHotLoaded;

		public long LocalVersionCode = -1;
		public long RemoteVersionCode = -1;
		public bool IsPreRemoteLoaded = false;
		public string RemoteResourceUrl = null;

		/// <summary>
		/// 远端重启标志
		/// </summary>
		public bool IsNeedRestart = true;

		/// <summary>
		/// 根据实际更新情况, 严格判断是否需要重启
		/// </summary>
		/// <returns></returns>
		public bool IsNeedRestartIndeed()
		{
			if (LocalVersionCode == RemoteVersionCode)
			{
				return false;
			}
			if (LocalVersionCode >= RemoteVersionCode && MyAddressablesUtils.EnableLocalMode)
			{
				// 禁用脚本更新之后, 可能出现脚本的localversion大于remoteversion, 此时无需重启
				return false;
			}
			if (IsPreRemoteLoaded)
			{
				return false;
			}

			if (IsNeedRestart && LocalVersionCode + 1 == RemoteVersionCode)
			{
				return false;
			}

			return true;
		}

		public string Dump()
		{
			return JsonUtility.ToJson(this);
		}

		public void Print()
		{
			Debug.LogError(Dump());
		}
	}
}