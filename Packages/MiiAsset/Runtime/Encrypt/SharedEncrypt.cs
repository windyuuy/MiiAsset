namespace MiiAsset.Runtime.Encrypt
{
	public static class SharedEncrypt
	{
		public static readonly AssetBundleEncryptor Encryptor = new AssetBundleEncryptor();

		static SharedEncrypt()
		{
			Encryptor.Init(AssetBundleEncryptor.GetSharedKey(true));
		}
	}
}