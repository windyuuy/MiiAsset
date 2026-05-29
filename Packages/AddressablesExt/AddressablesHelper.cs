using System.Threading.Tasks;
using MonoExtLib.AsyncExt;
using MiiAsset.AssetWeakRefer.Runtime;

namespace UnityEngine.AddressableAssets.MyExt
{
	public static class AddressablesHelper
	{

#if !DISABLE_NOREFERCOUNT_API
		public static async Task<Object> LoadAssetFast(this AssetReference uiActionSelf)
		{
			return uiActionSelf.RawAsset != null && uiActionSelf.IsValid()
				? uiActionSelf.RawAsset
				: await uiActionSelf.Load<Object>();
			// return await Addressables.LoadAssetAsync<Object>(uiActionSelf).Task;
		}

		public static async Task<T> LoadAssetFast<T>(this AssetReference uiActionSelf) where T : Object
		{
			return uiActionSelf.RawAsset != null && uiActionSelf.IsValid()
				? (T)(uiActionSelf.RawAsset)
				: await uiActionSelf.Load<T>();
		}

		public static async Task<T> LoadAssetFast<T>(this AssetReferenceT<T> uiActionSelf) where T : Object
		{
			return uiActionSelf.Asset != null && uiActionSelf.IsValid()
				? (T)(uiActionSelf.Asset)
				: await uiActionSelf.Load<T>();
		}

		/// <summary>
		/// delay release to improve performance
		/// </summary>
		/// <param name="uiActionSelf"></param>
		/// <param name="asset"></param>
		public static async Task DelayRelease(this AssetReference uiActionSelf, Object asset)
		{
			if (uiActionSelf != null && uiActionSelf.IsValid())
				// if (asset != null)
			{
				await UniAsyncUtils.WaitForEndOfFrame();
				await uiActionSelf.UnLoad(asset.GetType());
				// Addressables.Release(asset);
			}
		}
#endif
	}
}