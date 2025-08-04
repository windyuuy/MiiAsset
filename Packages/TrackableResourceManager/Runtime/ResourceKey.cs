using System;
using System.Text.RegularExpressions;

namespace TrackableResourceManager.Runtime
{
	public interface IResourceKey
	{
	}

	[Serializable]
	public readonly struct ResourceKey : IResourceKey
	{
		/// <summary>
		/// key or subUri
		/// </summary>
		public readonly string Key;

		internal ResourceKey(string key0)
		{
			this.Key = key0;
		}

		private static readonly Regex ParseKeyRegex = new Regex(@":([a-zA-Z0-9\u4E00-\u9FA5_]+)$");

		public string Name
		{
			get
			{
				var match = ParseKeyRegex.Match(Key);
				if (match.Success)
				{
					return match.Groups[1].Value;
				}

				return Key;
			}
		}

		private static readonly Regex ParseExcelRegex =
			new Regex(@"@[a-zA-Z0-9\u4E00-\u9FA5_]+:(?:[a-zA-Z0-9\u4E00-\u9FA5_]+:)?[a-zA-Z0-9\u4E00-\u9FA5_\.]+$");

		public static ResourceKey ParseFromLiteral(string key)
		{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
			var m = ParseExcelRegex.Match(key);
			if (!m.Success)
			{
				string msg = $"invalid resource key format: {key}";
#if UNITY_EDITOR
				throw new ArgumentException(msg);
#else
				UnityEngine.Debug.LogError(msg);
#endif
			}

#endif
			return new ResourceKey(key);
		}
	}
}