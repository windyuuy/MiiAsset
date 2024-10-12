﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.IOManagers;
using Framework.MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Framework.MiiAsset.Runtime
{
	public interface IAssetProvider
	{
		public interface IProviderInitOptions : IIOProtoInitOptions
		{
		}

		public Task<bool> Init(IProviderInitOptions options);

		public Task<PipelineResult> UpdateCatalog(string remoteBaseUri);

		public bool AllowTags(string[] tags);

		public Task LoadTags(string[] tags, AssetLoadStatusGroup loadStatus);
		public Task DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus);

		public Task UnLoadTags(string[] tags);

		public long GetDownloadSize(IEnumerable<string> tags);

		public bool IsAddressInTags(string address, IEnumerable<string> tags);

		public bool IsBundleInTags(string bundleFileName, IEnumerable<string> tags);

		public Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus) where T : Object;

		public Task UnloadAssetJust(string address);

		public Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus) where T : Object;

		public Task UnLoadAsset(string address);

		public Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus) where T : Object;

		public Task UnLoadAssetByRefer(string address);

		public Task<Scene> LoadScene(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus);

		public Task UnLoadScene(string sceneAddress, UnloadSceneOptions options);

		public Task<Scene> LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus);

		public Task UnLoadSceneByRefer(string sceneAddress, UnloadSceneOptions options);
		public Task<PipelineResult> CleanUpOldVersionFiles();
	}
}