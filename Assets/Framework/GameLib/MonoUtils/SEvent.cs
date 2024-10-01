
using System;
using System.Collections.Generic;

namespace Game.Event
{
	/// <summary>
	/// 事件发送器
	/// </summary>
	public class SEvent<TKey, TValue>
	{
		/// <summary> 事件表 </summary>
		private Dictionary<TKey, Action<TValue>> mEventDict = new Dictionary<TKey, Action<TValue>>();

		/// <summary> 添加事件监听器 </summary>
		/// <param name="eventType">事件类型</param>
		/// <param name="eventHandler">事件处理器</param>
		public void On(TKey eventType, Action<TValue> eventHandler)
		{
			Action<TValue> callbacks;
			if (mEventDict.TryGetValue(eventType, out callbacks))
			{
				mEventDict[eventType] = callbacks + eventHandler;
			}
			else
			{
				mEventDict.Add(eventType, eventHandler);
			}
		}

		/// <summary> 移除事件监听器 </summary>
		/// <param name="eventType">事件类型</param>
		/// <param name="eventHandler">事件处理器</param>
		public void Off(TKey eventType, Action<TValue> eventHandler)
		{
			Action<TValue> callbacks;
			if (mEventDict.TryGetValue(eventType, out callbacks))
			{
				callbacks = (Action<TValue>)Delegate.RemoveAll(callbacks, eventHandler);
				if (callbacks == null)
				{
					mEventDict.Remove(eventType);
				}
				else
				{
					mEventDict[eventType] = callbacks;
				}
			}
		}

		/// <summary> 是否已经拥有该类型的事件监听器 </summary>
		/// <param name="eventType">事件名称</param>
		public bool IsExist(TKey eventType)
		{
			return mEventDict.ContainsKey(eventType);
		}

		/// <summary> 发送事件 </summary>
		/// <param name="eventType">事件类型</param>
		/// <param name="eventArg">事件参数</param>
		public void Send(TKey eventType, TValue eventArg)
		{
			Action<TValue> callbacks;
			if (mEventDict.TryGetValue(eventType, out callbacks))
			{
				callbacks.Invoke(eventArg);
			}
		}

		/// <summary> 发送事件 </summary>
		/// <param name="eventType">事件类型</param>
		/// <param name="eventArg">事件参数</param>
		public void SendAndClear(TKey eventType, TValue eventArg)
		{
			Action<TValue> callbacks;
			if (mEventDict.TryGetValue(eventType, out callbacks))
			{
				callbacks.Invoke(eventArg);
				mEventDict.Remove(eventType);
			}
		}

		public void DriveHead(TKey eventType, TValue eventArg)
		{
			Action<TValue> callbacks;
			if (mEventDict.TryGetValue(eventType, out callbacks))
			{
				if (callbacks != null)
				{
					var list = callbacks.GetInvocationList();
					if (list.Length > 0)
					{
						var call0 = list[0];
						var call = call0 as Action<TValue>;
						mEventDict[eventType] = callbacks - call;
						call(eventArg);
					}
				}
			}
		}

		/// <summary> 清理所有事件监听器 </summary>
		public void Clear()
		{
			mEventDict.Clear();
		}

	}
}