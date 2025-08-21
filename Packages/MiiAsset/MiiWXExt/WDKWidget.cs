#if UNITY_WEBGL && SUPPORT_WDK
	using System.Threading.Tasks;
	using GDK;

	namespace MiiAsset.Runtime.IOManagers
	{
		public class WDKWidget : IWidget
		{
			public Task<GDK.ShowWidgetResult> ShowToast(string tip, float duration)
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