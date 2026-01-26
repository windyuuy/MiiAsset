using System;
using System.Collections;
using System.Runtime.CompilerServices;
using MonoExtLib.Loom;

namespace MiiAsset.Runtime.Utils
{
	internal static class JsDelay
	{
	#if UNITY_WEBGL
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void DelayExec(Action action)
		{
			LoomMG.SharedLoom.StartCoroutine(DelayIter(action));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static IEnumerator DelayIter(Action action)
		{
			yield return null;
			yield return null;
			action();
		}
	#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void DelayExec(Action action)
		{
			action();
		}
	#endif

	}
}