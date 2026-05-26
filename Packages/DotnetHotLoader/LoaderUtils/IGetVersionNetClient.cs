using System.Threading.Tasks;

namespace HatNetwork
{
	public interface IGetVersionNetClient
	{
		Task<MyAddressablesUtils.GetVersionResp> GetVersion();
	}
}