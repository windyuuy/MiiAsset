using System.Threading.Tasks;
using WDK;

namespace MiiAsset.Runtime.IOManagers
{
	public interface IWidget
	{
		public Task<ShowWidgetResult> ShowToast(string tip, float duration);
	}
}