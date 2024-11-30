using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiiAsset.Runtime.IOManagers;
using MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiiAsset.Runtime
{
	public interface IAssetProvider : IDisposable
	{
		public interface IProviderInitOptions : IIOProtoInitOptions
		{
			/// <summary>
			/// bundle最大下载并发数
			/// </summary>
			public int MaxDownloadCoCount { get; }

			public int InitDownloadCoCount { get; }
		}

		public Task<bool> Init(IProviderInitOptions options);

		public Task<PipelineResult> UpdateCatalog(string remoteBaseUri);
		public Task<PipelineResult> LoadLocalCatalog();

		public bool AllowTags(string[] tags);

		public Task<bool> LoadTags(string[] tags, AssetLoadStatusGroup loadStatus);
		public Task<bool> DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus);

		public Task UnLoadTags(string[] tags);

		public long GetDownloadSize(IEnumerable<string> tags);

		public bool IsAddressInTags(string address, IEnumerable<string> tags);

		public bool IsBundleInTags(string bundleFileName, IEnumerable<string> tags);

		public Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus);

		public Task UnloadAssetJust(string address);

		public Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus);

		public Task UnLoadAsset(string address);

		public string GetAddressFromGuid(string guid);

		bool ExistAddress(string address);
		bool ExistGuid(string guid);

		public Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus);

		public Task UnLoadAssetByRefer(string address);

		public Task<Scene> LoadScene(string sceneAddress, LoadSceneParameters parameters,
			AssetLoadStatusGroup loadStatus);

		public Task UnLoadScene(string sceneAddress, UnloadSceneOptions options);

		public Task<Scene> LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters,
			AssetLoadStatusGroup loadStatus);

		public Task UnLoadSceneByRefer(string sceneAddress, UnloadSceneOptions options);
		public Task<PipelineResult> CleanUpOldVersionFiles();
		public bool IsAssetBundlesOfAssetLoaded(string address);
	}
}