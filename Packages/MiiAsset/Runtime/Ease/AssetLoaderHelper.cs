using System.Collections;
using UnityEngine;

namespace MiiAsset.Runtime
{
	public static class AssetLoaderHelper
	{
		static IEnumerator CheckLoadTimeout(float checkTimeoutInterval)
		{
			while (true)
			{
				yield return new WaitForSeconds(checkTimeoutInterval);
				AssetLoader.CheckTimeout();
			}
		}

		private static Coroutine CheckLoadTimeoutCo;

		static IEnumerator RunDelayTask(float delayInterval)
		{
			while (true)
			{
				yield return null;
				yield return new WaitForSeconds(delayInterval);
				yield return new WaitForEndOfFrame();
				AssetLoader.RunDelayedTasks();
			}
		}

		private static Coroutine RunDelayTaskCo;

		public static void StartDefaultBackgroundTasks(MonoBehaviour loom,
			float delayInterval = 2f,
			float checkTimeoutInterval = 2f)
		{
			if (CheckLoadTimeoutCo != null)
			{
				loom.StopCoroutine(CheckLoadTimeoutCo);
				CheckLoadTimeoutCo = null;
			}

			CheckLoadTimeoutCo = loom.StartCoroutine(CheckLoadTimeout(checkTimeoutInterval));

			if (RunDelayTaskCo != null)
			{
				loom.StopCoroutine(RunDelayTaskCo);
				RunDelayTaskCo = null;
			}

			RunDelayTaskCo = loom.StartCoroutine(RunDelayTask(delayInterval));
		}
	}
}