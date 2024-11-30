using System.Threading.Tasks;

namespace MiiAsset.Runtime.IOManagers
{
	public interface IWidget
	{
		public ValueTask<bool> ShowToast(string tip, float duration);
	}
}