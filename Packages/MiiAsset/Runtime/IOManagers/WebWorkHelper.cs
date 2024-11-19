using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameLib.MonoUtils;
using MiiAsset.Runtime.Adapter;
using UnityEngine;

namespace MiiAsset.Runtime.IOManagers
{
    public struct BundleWebSemaphore : IDisposable
    {
        internal static Queue<TaskCompletionSource<bool>> Tasks = new();
        public static int MaxCount;

        internal static int PendingCount;

        internal static async void Init(int initCount, int maxCount)
        {
            MaxCount = initCount;
            MyLogger.Log($"下载并发数限制：{initCount}->{maxCount}");

            await UniAsyncUtils.WaitForSeconds(0.4f);
            MaxCount = (int)(initCount*0.7f + maxCount * 0.3f);

            await UniAsyncUtils.WaitForSeconds(0.8f);
            MaxCount = (int)(initCount*0.5f + maxCount * 0.5f);

            await UniAsyncUtils.WaitForSeconds(1f);
            MaxCount = maxCount;
        }

        internal static void Require()
        {
            for (var i = PendingCount; i < Math.Min(MaxCount, Tasks.Count); i++)
            {
                var ele = Tasks.Dequeue();
                ++PendingCount;
                try
                {
                    ele.SetResult(true);
                }
                catch (Exception exception)
                {
                    MyLogger.LogException(exception);
                }
            }

            // MyLogger.Log($"当前下载并发数：{PendingCount}");
        }

        internal static void Respond()
        {
            --PendingCount;
            Require();
        }

        private readonly Task _task;

        private BundleWebSemaphore(bool needLock)
        {
            var ts = new TaskCompletionSource<bool>();
            Tasks.Enqueue(ts);
            _task = ts.Task;

            Require();
        }

        public void Dispose()
        {
            Respond();
        }

        public static async Task<BundleWebSemaphore> Wait()
        {
            var loc = new BundleWebSemaphore(true);
            await loc._task;
            return loc;
        }
    }
}