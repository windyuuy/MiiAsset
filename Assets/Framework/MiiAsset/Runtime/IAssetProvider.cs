using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework.MiiAsset.Runtime
{
	public interface IAssetProvider
	{
		public IAssetProvider Init(string internalBaseUri, string externalBaseUri);

		public Task<PipelineResult> UpdateCatalog(string remoteBaseUri, string catalogName);

		public bool AllowTags(string[] tags);

		public Task LoadTags(string[] tags, AssetLoadStatusGroup loadStatus);
		public Task DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus);

		public Task UnLoadTags(string[] tags);

		public long GetDownloadSize(IEnumerable<string> tags);

		public bool IsAddressInTags(string address, IEnumerable<string> tags);

		public bool IsBundleInTags(string bundleFileName, IEnumerable<string> tags);

		public Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus);

		public Task UnloadAssetJust(string address);

		public Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus);

		public Task UnLoadAsset(string address);

		public Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus);

		public Task UnLoadAssetByRefer(string address);

		public Task<Scene> LoadScene(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus);

		public Task UnLoadScene(string sceneAddress);

		public Task<Scene> LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus);

		public Task UnLoadSceneByRefer(string sceneAddress);
	}
}