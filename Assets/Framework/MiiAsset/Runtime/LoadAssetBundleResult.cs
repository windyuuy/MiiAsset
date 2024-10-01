using System.Threading.Tasks;
using UnityEngine;

namespace Framework.MiiAsset.Runtime
{
	public class LoadAssetBundleResult
	{
		public AssetBundle AssetBundle;
		public int Code;
		public string Msg;

		public LoadAssetBundleResult(AssetBundle assetBundle, int code, string msg)
		{
			AssetBundle = assetBundle;
			Code = code;
			Msg = msg;
		}
	}
}