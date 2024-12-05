using System.Collections;
using UnityEngine;

namespace MiiAsset.Runtime
{
	public static class AssetLoaderHelper
	{
		static IEnumerator CheckLoadTimeout()
		{
			while (true)
			{
				yield return new WaitForSeconds(2f);
				AssetLoader.CheckTimeout();
			}
		}

		private static Coroutine CheckLoadTimeoutCo;

		static IEnumerator RunDelayTask()
		{
			while (true)
			{
				yield return null;
				yield return null;
				yield return new WaitForEndOfFrame();
				AssetLoader.RunDelayedTasks();
			}
		}

		private static Coroutine RunDelayTaskCo;

		public static void StartDefaultBackgroundTasks(MonoBehaviour loom)
		{
			if (CheckLoadTimeoutCo != null)
			{
				loom.StopCoroutine(CheckLoadTimeoutCo);
				CheckLoadTimeoutCo = null;
			}

			CheckLoadTimeoutCo = loom.StartCoroutine(CheckLoadTimeout());

			if (RunDelayTaskCo != null)
			{
				loom.StopCoroutine(RunDelayTaskCo);
				RunDelayTaskCo = null;
			}

			RunDelayTaskCo = loom.StartCoroutine(RunDelayTask());
		}
	}
}