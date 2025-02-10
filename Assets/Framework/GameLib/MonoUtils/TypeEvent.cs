using System;
using System.Collections.Generic;

namespace MonoExtLib.Event
{
	public class TypeEvent
	{
		/// <summary> 事件表 </summary>
		private readonly Dictionary<Type, object> _eventDict = new();

		/// <summary> 添加事件监听器 </summary>
		/// <param name="eventHandler">事件处理器</param>
		public void On<T>(Action<T> eventHandler)
		{
			var eventType = typeof(T);
			if (_eventDict.TryGetValue(eventType, out var callbacks))
			{
				_eventDict[eventType] = (Action<T>)callbacks + eventHandler;
			}
			else
			{
				_eventDict.Add(eventType, eventHandler);
			}
		}

		/// <summary> 移除事件监听器 </summary>
		/// <param name="eventHandler">事件处理器</param>
		public void Off<T>(Action<T> eventHandler)
		{
			var eventType = typeof(T);
			if (_eventDict.TryGetValue(eventType, out var callbacks))
			{
				callbacks = (Action<T>)Delegate.RemoveAll((Action<T>)callbacks, eventHandler);
				if (callbacks == null)
				{
					_eventDict.Remove(eventType);
				}
				else
				{
					_eventDict[eventType] = callbacks;
				}
			}
		}

		/// <summary> 是否已经拥有该类型的事件监听器 </summary>
		public bool IsExist<T>()
		{
			var eventType = typeof(T);
			return _eventDict.ContainsKey(eventType);
		}

		/// <summary> 发送事件 </summary>
		/// <param name="eventArg">事件参数</param>
		public void Send<T>(T eventArg)
		{
			var eventType = typeof(T);
			if (_eventDict.TryGetValue(eventType, out var callbacks))
			{
				((Action<T>)callbacks).Invoke(eventArg);
			}
		}

		/// <summary>
		/// 只向第一个监听发送事件
		/// </summary>
		/// <param name="eventArg"></param>
		/// <typeparam name="T"></typeparam>
		public void DriveHead<T>(T eventArg)
		{
			var eventType = typeof(T);
			if (_eventDict.TryGetValue(eventType, out var callbacks))
			{
				if (callbacks != null)
				{
					var action = (Action<T>)callbacks;
					var list = action.GetInvocationList();
					if (list.Length > 0)
					{
						var call0 = list[0];
						var call = call0 as Action<T>;
						_eventDict[eventType] = action - call;
						call(eventArg);
					}
				}
			}
		}

		/// <summary> 清理所有事件监听器 </summary>
		public void Clear()
		{
			_eventDict.Clear();
		}
	}
}