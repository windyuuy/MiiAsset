using System.Threading;
using UnityEngine;

namespace GameLib.Networking.Ext
{
	public static class NativeHelper
	{
		public static T CallNativeU<T>(this AndroidJavaObject nativeObj, string methodName, params object[] args)
		{
			Debug.Log($"CallNative_methodName: {methodName}");

			var mutex = new Mutex();
			T result = default(T);
			GetCurrentActivity().Call("runOnUiThread",
				new AndroidJavaRunnable(() =>
				{
					var ret = nativeObj.Call<T>(methodName, args);
					result = ret;
					mutex.ReleaseMutex();
					mutex.Dispose();
				}));

			mutex.WaitOne();

			return result;
		}

		public static AndroidJavaObject CreateNativeU(string className)
		{
			var mutex = new Mutex();
			AndroidJavaObject obj = null;
			GetCurrentActivity().Call("runOnUiThread",
				new AndroidJavaRunnable(() =>
				{
					obj = new AndroidJavaObject(className);
					mutex.ReleaseMutex();
					mutex.Dispose();
				}));

			mutex.WaitOne();
			return obj;
		}

		public static T CallNativeR<T>(this AndroidJavaObject nativeObj, string methodName, params object[] args)
		{
			Debug.Log($"CallNative_methodName: {methodName}");

			T result = nativeObj.Call<T>(methodName, args);

			return result;
		}

		public static void CallNativeR(this AndroidJavaObject nativeObj, string methodName, params object[] args)
		{
			Debug.Log($"CallNative_methodName: {methodName}");

			nativeObj.Call(methodName, args);
		}

		public static AndroidJavaObject CreateNativeR(string className)
		{
			AndroidJavaObject obj = new AndroidJavaObject(className);
			return obj;
		}

		public static T CallNativeR<T>(this AndroidJavaClass nativeCls, string methodName)
		{
			Debug.Log($"CallNative_methodName: {methodName}");

			T result = nativeCls.CallStatic<T>(methodName);

			return result;
		}

		public static AndroidJavaClass GetNativeR(string className)
		{
			AndroidJavaClass cls = new AndroidJavaClass(className);
			return cls;
		}

		private static AndroidJavaObject _activityObject;

		private static AndroidJavaObject GetCurrentActivity()
		{
			if (_activityObject != null)
			{
				return _activityObject;
			}

			var jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

			_activityObject = jc.GetStatic<AndroidJavaObject>("currentActivity");
			return _activityObject;
		}
	}
}