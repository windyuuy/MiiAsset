﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GameLib.MonoUtils;
using MiiAsset.Editor.Optimization;
using MiiAsset.Runtime.Status;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MiiAsset.Runtime
{
	public class EditorAssetProvider : IAssetProvider
	{
		protected AAPathInfo PathInfo;

		public Task<bool> Init(IAssetProvider.IProviderInitOptions options)
		{
			PathInfo = AAPathConfigLoader.LoadDefaultConfigs();

			return Task.FromResult(true);
		}

		public Task<PipelineResult> UpdateCatalog(string remoteBaseUri)
		{
			return Task.FromResult(new PipelineResult
			{
				IsOk = true,
				Status = PipelineStatus.Done,
			});
		}

		public Task<PipelineResult> LoadLocalCatalog()
		{
			return UpdateCatalog(null);
		}

		public bool AllowTags(string[] tags)
		{
			return true;
		}

		public Task<bool> LoadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			return Task.FromResult(true);
		}

		public Task<bool> DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			return Task.FromResult(true);
		}

		public Task UnLoadTags(string[] tags)
		{
			return Task.CompletedTask;
		}

		public long GetDownloadSize(IEnumerable<string> tags)
		{
			return 0;
		}

		public bool IsAddressInTags(string address, IEnumerable<string> tags)
		{
			return true;
		}

		public bool IsBundleInTags(string bundleFileName, IEnumerable<string> tags)
		{
			return true;
		}

		public Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus)
		{
			if (!CheckPathAndTags<T>(address)) return Task.FromResult<T>(default);

			var obj = AssetDatabase.LoadAssetAtPath(address, typeof(T));
			if (obj is T data)
			{
				loadStatus?.Add(new AssetDatabaseOpStatus(true));
				return Task.FromResult(data);
			}
			else
			{
				loadStatus?.Add(new AssetDatabaseOpStatus(false));
				return Task.FromResult(default(T));
			}
		}

		private bool CheckPathAndTags<T>(string address)
		{
			var groupInfo = AAPathInfo.ParseGroupName(PathInfo, address, AssetDatabase.AssetPathToGUID(address));
			if (groupInfo == null)
			{
				return false;
			}

			return true;
		}

		public Task UnloadAssetJust(string address)
		{
			return Task.CompletedTask;
		}

		public Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus)
		{
			return LoadAssetJust<T>(address, loadStatus);
		}

		public Task UnLoadAsset(string address)
		{
			return Task.CompletedTask;
		}

		public string GetAddressFromGuid(string guid)
		{
			return AssetDatabase.GUIDToAssetPath(guid);
		}

		public bool ExistAddress(string address)
		{
			return File.Exists(address);
		}

		public bool ExistGuid(string guid)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var exist = !string.IsNullOrEmpty(path);
			return exist;
		}

		public Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus)
		{
			return LoadAssetJust<T>(address, loadStatus);
		}

		public Task UnLoadAssetByRefer(string address)
		{
			return Task.CompletedTask;
		}

		public async Task<Scene> LoadScene(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus)
		{
			if (!CheckPathAndTags<Scene>(sceneAddress)) return default;

			var op = EditorSceneManager.LoadSceneAsyncInPlayMode(sceneAddress, parameters);
			var subStatus = loadStatus?.AddAsyncOperationStatus(op);
			await op.GetTask();
			var scene = SceneManager.GetSceneByName(sceneAddress);
			return scene;
		}

		public Task UnLoadScene(string sceneAddress, UnloadSceneOptions options)
		{
			var op = SceneManager.UnloadSceneAsync(sceneAddress, options);
			return op.GetTask();
		}

		public Task<Scene> LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters, AssetLoadStatusGroup loadStatus)
		{
			return LoadScene(sceneAddress, parameters, loadStatus);
		}

		public Task UnLoadSceneByRefer(string sceneAddress, UnloadSceneOptions options)
		{
			return UnLoadScene(sceneAddress, options);
		}

		public Task<PipelineResult> CleanUpOldVersionFiles()
		{
			return Task.FromResult(new PipelineResult
			{
				IsOk = true,
				Status = PipelineStatus.Done,
			});
		}

		public void Dispose()
		{
		}
	}
}