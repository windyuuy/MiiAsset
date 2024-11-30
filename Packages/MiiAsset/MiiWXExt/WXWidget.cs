using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using MiiAsset.Runtime.Adapter;
using UnityEngine;
using WeChatWASM;

namespace MiiAsset.Runtime.IOManagers
{
	public class WXWidget : IWidget
	{
		public ValueTask<bool> ShowToast(string tip, float duration)
		{
			var ts = new TaskCompletionSource<bool>();
			WX.ShowToast(new ShowToastOption
			{
				title = tip,
				duration = duration * 1000f,
				fail = (resp) =>
				{
					MyLogger.LogError(resp.errMsg);
					ts.SetResult(false);
				},
				success = (resp) => { ts.SetResult(true); }
			});
			return new ValueTask<bool>(ts.Task);
		}
	}
}