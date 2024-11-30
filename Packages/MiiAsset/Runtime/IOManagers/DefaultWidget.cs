using System.Threading.Tasks;
using MiiAsset.Runtime.Adapter;
using UnityEngine;

namespace MiiAsset.Runtime.IOManagers
{
	public class DefaultWidget : IWidget
	{
		public ValueTask<bool> ShowToast(string tip, float duration)
		{
			// TODO: impl default
			MyLogger.LogError($"ShowToast called: {tip}, {duration}");
			return new ValueTask<bool>(true);
		}
	}
}