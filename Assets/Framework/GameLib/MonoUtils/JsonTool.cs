using System;
using System.Collections.Generic;
using System.Threading;
using lang.time;
using Newtonsoft.Json;
using UnityEngine;

namespace lang.json
{
	enum JsonJobType
	{
		Parse,
		Stringify,
		StringifyIndented
	}
	class JsonJob
	{
		public string jsonString;
		public object jsonObject;
		public JsonJobType type;
		public Action<string> stringifyCallback;
		public Action<object> parseCallback;
		public Type typeObject;
	}


	public class JSON
	{
		public T Parse<T>(string value)
		{
			try
			{
				return JsonConvert.DeserializeObject<T>(value);
			}
			catch (Exception e)
			{
				Debug.LogError("DeserializeObject failed:" + value);
				Debug.LogError(e + "\n" + e.StackTrace);
				throw e;
			}
		}
		public string Stringify(object value)
		{
			return JsonConvert.SerializeObject(value);
		}

		public string Stringify(object value, bool prettyPrint)
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

		protected static JSON _instance;
		public static JSON instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new JSON();
				}
				return _instance;
			}
		}

		private List<JsonJob> jobList = new List<JsonJob>();
		protected bool isLive = true;

		public void StartAsyncWorkThread()
		{
			ThreadStart childRef = new ThreadStart(logic);
			Thread childThread = new Thread(childRef);
			childThread.Start();
		}

		public void logic()
		{
			while (isLive)
			{
				JsonJob[] list;
				lock (jobList)
				{
					list = jobList.ToArray();
					jobList.Clear();
				}
				foreach (var s in list)
				{
					switch (s.type)
					{
						case JsonJobType.Parse:
							var o = JsonConvert.DeserializeObject(s.jsonString, s.typeObject);
							TimerTick.instance.RunInMainThread(() =>
							{
								s.parseCallback(o);
							});
							break;
						case JsonJobType.Stringify:
							var jsonStr = JsonConvert.SerializeObject(s.jsonObject);
							TimerTick.instance.RunInMainThread(() =>
							{
								s.stringifyCallback(jsonStr);
							});
							break;
						case JsonJobType.StringifyIndented:
							var jsonStrInd = JsonConvert.SerializeObject(s.jsonObject, Formatting.Indented);
							TimerTick.instance.RunInMainThread(() =>
							{
								s.stringifyCallback(jsonStrInd);
							});
							break;
					}
				}

				Thread.Sleep(50);
			}
		}

		public void StringifyAsync(object value, Action<string> callback)
		{
			var job = new JsonJob()
			{
				type = JsonJobType.Stringify,
				jsonObject = value,
				stringifyCallback = callback
			};
			lock (jobList)
			{
				jobList.Add(job);
			}
		}

		public void StringifyAsync(object value, bool prettyPrint, Action<string> callback)
		{
			var job = new JsonJob()
			{
				type = prettyPrint ? JsonJobType.StringifyIndented : JsonJobType.Stringify,
				jsonObject = value,
				stringifyCallback = callback
			};
			lock (jobList)
			{
				jobList.Add(job);
			}
		}

		public void ParseAsync(string value, Type type, Action<object> callback)
		{
			var job = new JsonJob()
			{
				type = JsonJobType.Parse,
				jsonString = value,
				parseCallback = callback,
				typeObject = type
			};
			lock (jobList)
			{
				jobList.Add(job);
			}

		}

		public void Destroy()
		{
			this.isLive = false;
		}

		public static T parse<T>(string value)
		{
			return instance.Parse<T>(value);
		}

		public static string stringify(object value, bool prettyPrint = false)
		{
			return instance.Stringify(value, prettyPrint);
		}

		public static void stringifyAsync(object value, Action<string> callback)
		{
			instance.StringifyAsync(value, callback);
		}

		public static void stringifyAsync(object value, bool prettyPrint, Action<string> callback)
		{
			instance.StringifyAsync(value, prettyPrint, callback);
		}

		public static void parseAsync(string value, Type type, Action<object> callback)
		{
			instance.ParseAsync(value, type, callback);
		}
	}
}