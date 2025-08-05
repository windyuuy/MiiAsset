using System.Threading.Tasks;
using GDK;

namespace MiiAsset.Runtime.IOManagers
{
	public interface IWidget
	{
		public Task<ShowWidgetResult> ShowToast(string tip, float duration);
	}
}