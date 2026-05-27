using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace HatNetwork
{
	public class AssetBundleDownloader
	{
		public AssetBundleDownloader()
		{
			Options = new DownloadOptions();
		}
		public AssetBundleDownloader(DownloadOptions downloadOptions)
		{
			Options = downloadOptions;
		}

		public class DownloadOptions
		{
			public uint Timeout = 8;
			public int RetryCount = 3;
			//public string BundleName = "";
		}

		public DownloadOptions Options;

		int m_Retries;
		ulong m_LastDownloadedByteCount = 0;
		float m_TimeoutTimer = 0;
		int m_TimeoutOverFrames = 0;

		private bool HasTimedOut => m_TimeoutTimer >= Options.Timeout && m_TimeoutOverFrames > 5;
		private bool HasTimedOutMuch => m_TimeoutTimer >= (Options.Timeout + 3) && m_TimeoutOverFrames > 8;

		public void UpdateTimeout(UnityWebRequest webReq, float unscaledDeltaTime)
		{
			if (webReq!=null && !webReq.isDone)
			{
				if (m_LastDownloadedByteCount != webReq.downloadedBytes)
				{
					m_TimeoutTimer = 0;
					m_TimeoutOverFrames = 0;
					m_LastDownloadedByteCount = webReq.downloadedBytes;
				}
				else
				{
					m_TimeoutTimer += unscaledDeltaTime;
					if (HasTimedOut)
					{
						webReq.Abort();
					}
					m_TimeoutOverFrames++;
				}
			}
		}

		public struct NetState
		{
			public bool complete;
			public bool success;
			public bool needRetry;
			public Exception exception;
			public Exception SafeException => exception ?? new Exception("unkown");
			public AssetBundle assetBundle;

			public void Release()
			{
				if (assetBundle != null)
				{
					assetBundle.Unload(false);
					assetBundle = null;
				}
			}
		}

		// TODO: 需要重构代码
		public Task<NetState> GetDownloadTaskWithoutRetry(UnityWebRequest uwr, MonoBehaviour component, bool retry=true)
		{
			var DownloadTaskSource = new TaskCompletionSource<NetState>();

			Debug.LogWarning($"Http-ABD-Begin: {uwr.url}: {uwr.isDone}, {uwr.error}");
			Coroutine updateCo=null;
			Coroutine coroutine = null;
			IEnumerator Complete()
			{
#if UNITY_IOS
				if (MyAddressablesUtils.EnableLocalMode)
				{
					Debug.Log($"watchabload: {uwr.url}");
				}
#endif
				yield return uwr.SendWebRequest();
				if (updateCo != null)
				{
					component.StopCoroutine(updateCo);
					updateCo = null;
				}
				component.StopCoroutine(coroutine);
				coroutine = null;
				var netState = HandleNetState(uwr);

				if (retry && !netState.success && uwr.url.StartsWith("http"))
				{
					Debug.LogError($"Http-ABD-Failed: {uwr.url}: {uwr.error}");
					MyAddressablesUtils.ShowNetError($"检查更新游戏失败(1), code:{uwr.responseCode}, msg:{uwr.error}", () =>
					{
						netState.needRetry = true;
						DownloadTaskSource.SetResult(netState);
					});
				}
				else
				{
					Debug.LogWarning($"Http-ABD-Return: {uwr.url}: {uwr.error}, {netState.success}, {netState.needRetry}");
					DownloadTaskSource.SetResult(netState);
				}
			}
			if (!uwr.isDone)
			{
				IEnumerator Update()
				{
					while (true)
					{
						yield return null;
						UpdateTimeout(uwr, Time.unscaledDeltaTime);

						if (HasTimedOutMuch)
						{
							// 超时后, 如果uwr还是未返回, 那么视作Unity出bug, 强制手动返回
							Debug.Log($"Http-ABD-HasTimedOutMuch: {uwr.url}: {uwr.error}");
							if (coroutine != null)
							{
								component.StopCoroutine(coroutine);
								coroutine = null;
							}
							component.StopCoroutine(updateCo);
							var netState = HandleNetState(uwr);
							DownloadTaskSource.SetResult(netState);
							break;
						}
					}
				}
				updateCo = component.StartCoroutine(Update());
				coroutine = component.StartCoroutine(Complete());
			}
			else
			{
				Debug.LogWarning($"Http-ABD-Reuse: {uwr.url}: {uwr.error}");
				var netState = HandleNetState(uwr);
				DownloadTaskSource.SetResult(netState);
			}

			return DownloadTaskSource.Task;
		}

		protected static readonly Exception InvalidTryException = new Exception("Invalid retryCount");
		public async Task<NetState> GetDownloadTask(UnityWebRequest webReq, MonoBehaviour component)
		{
			NetState netState = new NetState()
			{
				complete = false,
				exception = InvalidTryException,
				needRetry = false,
				success = false,
			};

			//while (m_Retries < Options.RetryCount)
			{
				netState = await GetDownloadTaskWithoutRetry(webReq, component);
				//if (!netState.needRetry)
				//{
				//	return netState;
				//}
			}

			return netState;
		}

		public AssetBundle GetAssetBundle(UnityWebRequest webReq)
		{
			var downloadHandler = webReq.downloadHandler;
			try
			{
				var assetBundle = AssetBundle.LoadFromMemory(downloadHandler.data);
				return assetBundle;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			return null;
		}

		/// <summary>
		/// (complete, succeed, needretry)
		/// </summary>
		/// <param name="webReq"></param>
		/// <returns></returns>
		public NetState HandleNetState(UnityWebRequest webReq)
		{
			NetState netState;
			UnityWebRequestResult uwrResult = null;
			if (webReq != null && !UnityWebRequestUtilities.RequestHasErrors(webReq, out uwrResult) && !HasTimedOut)
			{
				netState = new NetState()
				{
					complete = true,
					success = true,
					needRetry = false,
					exception = null,
				};
			}
			else
			{
				if (uwrResult == null)
					uwrResult = new UnityWebRequestResult(webReq);

				if (HasTimedOut)
					uwrResult.Error = "Request timeout";

				string message = $"Web request failed, retrying ({m_Retries}/{Options.RetryCount})...\n{uwrResult}";

				if (m_Retries < Options.RetryCount && uwrResult.ShouldRetryDownloadError())
				{
					m_Retries++;
					Debug.LogFormat(message);
					netState = new NetState()
					{
						complete = true,
						needRetry = true,
						success = false,
						exception = new Exception(message),
					};
				}
				else
				{
					var exception = new Exception($"Unable to load asset bundle from1 : {webReq.url}");
					netState = new NetState()
					{
						complete = true,
						needRetry = false,
						success = false,
						exception = exception,
					};
				}
			}

			return netState;
		}

		public void DisposeResource(UnityWebRequest webReq)
		{
			var downloadHandler = webReq.downloadHandler;

			downloadHandler.Dispose();
			downloadHandler = null;

			webReq.Dispose();
			webReq = null;
		}
	}
}
