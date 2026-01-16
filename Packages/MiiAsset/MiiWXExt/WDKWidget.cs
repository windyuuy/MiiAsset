#if UNITY_WEBGL && SUPPORT_WDK
	using System.Threading.Tasks;
	using WDK;

	namespace MiiAsset.Runtime.IOManagers
	{
		public class WDKWidget : IWidget
		{
			public Task<WDK.ShowWidgetResult> ShowToast(string tip, float duration)
			{
				return UserAPI.Instance.Widgets.ShowToast(new()
				{
					title = tip,
					duration = duration * 1000f,
				});
			}
		}
	}
#endif