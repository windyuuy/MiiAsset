using System;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

namespace Lang.Json
{
	public class JSON
	{
		public static T Parse<T>(string value)
		{
			try
			{
				return JsonConvert.DeserializeObject<T>(value);
			}
			catch (Exception e)
			{
// #if UNITY_EDITOR || UNITY_2017_1_OR_NEWER
				UnityEngine.Debug.LogError("DeserializeObject failed:" + value);
				UnityEngine.Debug.LogError(e + "\n" + e.StackTrace);
// #endif
				throw e;
			}
		}

		public static string Stringify(object value)
		{
			return JsonConvert.SerializeObject(value);
		}

		public static string Stringify(object value, bool prettyPrint)
		{
			if (prettyPrint)
			{
				return JsonConvert.SerializeObject(value, Formatting.Indented);
			}
			else
			{
				return JsonConvert.SerializeObject(value);
			}
		}
	}
}