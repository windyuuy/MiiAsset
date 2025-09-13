using System;
using System.Threading.Tasks;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace MiiAsset.Runtime
{
	public static class ExtraAssetLoader
	{
		public static Task<T> LoadAsset<T>(string address, Func<string, Task<T>> assetLoader,
			Func<string, Task<SpriteAtlas>> atlasLoader)
			where T : UnityEngine.Object
		{
			if (AssetLoader.TryGetExtraAddressInfo(address, out var extraAddressInfo))
			{
				switch (extraAddressInfo.loadType)
				{
					case AddressLoadType.Common:
						return assetLoader(extraAddressInfo.address);
					case AddressLoadType.AtlasSprite:
						return LoadAtlasSprite<T>(extraAddressInfo, atlasLoader);
					default:
						throw new NotImplementedException($"invalid extra asset loadType: {extraAddressInfo.loadType}");
				}
			}
			else
			{
				return assetLoader(address);
			}
		}

		public static async Task<T> LoadAtlasSprite<T>(ExtraAddressInfo extraAddressInfo,
			Func<string, Task<SpriteAtlas>> atlasLoader) where T : Object
		{
			var spriteAtlas = await atlasLoader(extraAddressInfo.sourceAddress);
			if (spriteAtlas != null)
			{
				var sprite = spriteAtlas.GetSprite(extraAddressInfo.key);
				return sprite as T;
			}
			else
			{
				return default(T);
			}
		}
	}
}