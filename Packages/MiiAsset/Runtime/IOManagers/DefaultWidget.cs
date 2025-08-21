using System.Threading.Tasks;
using GDK;
using MiiAsset.Runtime.Adapter;
using UnityEngine;

namespace MiiAsset.Runtime.IOManagers
{
	public class DefaultWidget : IWidget
	{
		public Task<ShowWidgetResult> ShowToast(string tip, float duration)
		{
			// TODO: impl default
			MyLogger.LogError($"ShowToast called: {tip}, {duration}");
			return Task.FromResult(new ShowWidgetResult
			{
				IsOk = true,
			});
		}
	}
}