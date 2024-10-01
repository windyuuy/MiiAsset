using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Framework.MiiAsset.Runtime
{
	public static class AssetLoader
	{
		private static readonly AssetBundleConsumer Consumer = new();

		public static void Init()
		{
			Consumer.Init();
		}

		public static Task UpdateCatalog(string remoteBaseUri, string catalogName)
		{
			return Consumer.UpdateCatalog(remoteBaseUri, catalogName);
		}

		public static bool AllowTags(string[] tags)
		{
			return Consumer.AllowTags(tags);
		}

		public static bool AllowTag(params string[] tags)
		{
			return Consumer.AllowTags(tags);
		}

		public static Task LoadTags(string[] tags)
		{
			return Consumer.LoadTags(tags);
		}

		public static Task LoadTag(params string[] tags)
		{
			return Consumer.LoadTags(tags);
		}

		public static Task UnLoadTags(string[] tags)
		{
			return Consumer.UnLoadTags(tags);
		}

		public static Task UnLoadTag(params string[] tags)
		{
			return Consumer.UnLoadTags(tags);
		}

		public static Task<T> LoadAssetJust<T>(string address)
		{
			return Consumer.LoadAssetJust<T>(address);
		}

		public static Task UnloadAssetJust(string address)
		{
			return Consumer.UnloadAssetJust(address);
		}

		public static Task<T> LoadAsset<T>(string address)
		{
			return Consumer.LoadAsset<T>(address);
		}

		public static Task UnLoadAsset(string address)
		{
			return Consumer.UnLoadAsset(address);
		}

		public static Task<T> LoadAssetByRefer<T>(string address)
		{
			return Consumer.LoadAssetByRefer<T>(address);
		}

		public static Task UnLoadAssetByRefer(string address)
		{
			return Consumer.UnLoadAssetByRefer(address);
		}

		public static Task LoadScene(string sceneAddress, LoadSceneParameters parameters = new())
		{
			return Consumer.LoadScene(sceneAddress, parameters);
		}

		public static Task UnLoadScene(string sceneAddress)
		{
			return Consumer.UnLoadScene(sceneAddress);
		}

		public static Task LoadSceneByRefer(string sceneAddress)
		{
			return Consumer.LoadSceneByRefer(sceneAddress);
		}

		public static Task UnLoadSceneByRefer(string sceneAddress)
		{
			return Consumer.UnLoadSceneByRefer(sceneAddress);
		}
	}
}