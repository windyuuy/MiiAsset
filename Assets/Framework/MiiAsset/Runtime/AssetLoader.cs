using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.Status;
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

		public static Task<PipelineResult> UpdateCatalog(string remoteBaseUri, string catalogName)
		{
			return Consumer.UpdateCatalog(remoteBaseUri, catalogName);
		}

		public static Task<PipelineResult> CleanUpOldVersionFiles()
		{
			return Consumer.CleanUpOldVersionFiles();
		}

		public static bool AllowTags(string[] tags)
		{
			return Consumer.AllowTags(tags);
		}

		public static bool AllowTag(params string[] tags)
		{
			return Consumer.AllowTags(tags);
		}

		public static Task LoadTags(IEnumerable<string> tags, AssetLoadStatusGroup loadStatus = null)
		{
			if (tags is not string[] tags1)
			{
				tags1 = tags.ToArray();
			}

			return Consumer.LoadTags(tags1, loadStatus);
		}

		public static Task LoadTags(params string[] tags)
		{
			return Consumer.LoadTags(tags, null);
		}

		public static Task DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.DownloadTags(tags, loadStatus);
		}

		public static Task UnLoadTags(string[] tags)
		{
			return Consumer.UnLoadTags(tags);
		}

		public static Task UnLoadTag(params string[] tags)
		{
			return Consumer.UnLoadTags(tags);
		}

		public static long GetDownloadSize(IEnumerable<string> tags)
		{
			return Consumer.GetDownloadSize(tags);
		}

		public static long GetDownloadSize(params string[] tags)
		{
			return Consumer.GetDownloadSize(tags);
		}

		public static bool IsAddressInTags(string address, IEnumerable<string> tags)
		{
			return Consumer.IsAddressInTags(address, tags);
		}

		public static bool IsBundleInTags(string bundleFileName, IEnumerable<string> tags)
		{
			return Consumer.IsBundleInTags(bundleFileName, tags);
		}

		public static Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.LoadAssetJust<T>(address, loadStatus);
		}

		public static Task UnloadAssetJust(string address)
		{
			return Consumer.UnloadAssetJust(address);
		}

		public static Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.LoadAsset<T>(address, loadStatus);
		}

		public static Task UnLoadAsset(string address)
		{
			return Consumer.UnLoadAsset(address);
		}

		public static Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.LoadAssetByRefer<T>(address, loadStatus);
		}

		public static Task UnLoadAssetByRefer(string address)
		{
			return Consumer.UnLoadAssetByRefer(address);
		}

		public static Task LoadScene(string sceneAddress, LoadSceneParameters parameters = new(), AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.LoadScene(sceneAddress, parameters, loadStatus);
		}

		public static Task UnLoadScene(string sceneAddress)
		{
			return Consumer.UnLoadScene(sceneAddress);
		}

		public static Task LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters = new(), AssetLoadStatusGroup loadStatus = null)
		{
			return Consumer.LoadSceneByRefer(sceneAddress, parameters, loadStatus);
		}

		public static Task UnLoadSceneByRefer(string sceneAddress)
		{
			return Consumer.UnLoadSceneByRefer(sceneAddress);
		}

		public static TagsBundleSet GetTagsBundleSet(params string[] tags)
		{
			return GetTagsBundleSet(tags, false);
		}

		public static TagsBundleSet GetTagsBundleSet(IEnumerable<string> tags, bool allowTags = false)
		{
			var tagsBundleSet = new TagsBundleSet(Consumer, tags);
			if (allowTags)
			{
				tagsBundleSet.AllowTags();
			}

			return tagsBundleSet;
		}
	}
}