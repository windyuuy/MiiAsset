
using System;
using System.Collections;
using System.Threading.Tasks;
using GameLib.MonoUtils;
using UnityEngine;

public class AsyncUtils
{
    public static Task<float> WaitForSeconds(float dt)
    {
        return _WaitForSeconds0(dt, null);
	}
	public static Task<float> WaitForSeconds(MonoBehaviour comp, float dt)
	{
		return _WaitForSeconds0(dt, comp);
	}
	protected static Task<float> _WaitForSeconds0(float dt,MonoBehaviour comp)
    {
        var t1 = Time.time;
        var ts = new TaskCompletionSource<float>();
        (comp??LoomMG.sharedLoom).StartCoroutine(_WaitForSeconds(dt, () =>
        {
            var t2 = Time.time;
            var dt = t2 - t1;
            ts.SetResult(dt);
        }));
        return ts.Task;
    }

    protected static IEnumerator _WaitForSeconds(float dt, Action call)
    {
        yield return new WaitForSeconds(dt);
        call();
    }

	/// <summary>
	/// 按帧等待
	/// </summary>
	/// <param name="frames"></param>
	/// <returns></returns>
	public static Task<int> WaitForFrames(int frames = 1)
	{
		return _WaitForFrames0(frames, null);
	}
	/// <summary>
	/// 按帧等待
	/// </summary>
	/// <param name="comp"></param>
	/// <param name="frames"></param>
	/// <returns></returns>
	public static Task<int> WaitForFrames(MonoBehaviour comp, int frames=1)
	{
		return _WaitForFrames0(frames, comp);
	}
	protected static Task<int> _WaitForFrames0(int frames, MonoBehaviour comp)
	{
		var ts = new TaskCompletionSource<int>();
		var monoBehaviour = (comp ?? LoomMG.sharedLoom);
		if (monoBehaviour == null)
		{
			Debug.LogError("invalid monoBehaviour to StartCoroutine");
		}
		monoBehaviour.StartCoroutine(_WaitForFrames(frames, () =>
		{
			ts.SetResult(frames);
		}));
		return ts.Task;
	}

	protected static IEnumerator _WaitForFrames(int frames, Action call)
	{
        for(var i = 0; i < frames; i++)
        {
			yield return null;
		}
		call();
	}

	public static Task WaitUntil(Func<bool> predicate)
    {
        var ts = new TaskCompletionSource<bool>();
        LoomMG.sharedLoom.StartCoroutine(_WaitUntil(predicate, () =>
        {
            ts.SetResult(true);
        }));
        return ts.Task;
    }

	public static Task WaitUntil(MonoBehaviour comp,Func<bool> predicate)
	{
		var ts = new TaskCompletionSource<bool>();
		comp.StartCoroutine(_WaitUntil(predicate, () =>
		{
			ts.SetResult(true);
		}));
		return ts.Task;
	}

	protected static IEnumerator _WaitUntil(Func<bool> predicate,Action call)
    {
        yield return new WaitUntil(predicate);
        call();
    }
}
