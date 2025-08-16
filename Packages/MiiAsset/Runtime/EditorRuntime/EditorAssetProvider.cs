using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MiiAsset.Editor.Build;
using MiiAsset.Editor.Optimization;
using MiiAsset.Runtime.Adapter;
using MiiAsset.Runtime.Status;
using MonoExtLib.AsyncExt;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiiAsset.Runtime
{
	public class EditorAssetProvider : IAssetProvider
	{
		protected AAPathInfo PathInfo;
		protected readonly DepCollector DepCollector = new DepCollector();

		public Task<bool> Init(IAssetProvider.IProviderInitOptions options)
		{
			return Task.FromResult(true);
		}

		public Task<PipelineResult> UpdateCatalog(string remoteBaseUri)
		{
			PathInfo ??= AAPathConfigLoader.LoadDefaultConfigs();

			DepCollector.CollectValidAssets(PathInfo);

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

		public Task<PipelineResultGroup> LoadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			return Task.FromResult(PipelineResultGroup.Succeed);
		}

		public Task<PipelineResultGroup> DownloadTags(string[] tags, AssetLoadStatusGroup loadStatus)
		{
			return Task.FromResult(PipelineResultGroup.Succeed);
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

		public Task<T> LoadAssetJust<T>(string address, AssetLoadStatusGroup loadStatus) where T : UnityEngine.Object
		{
			if (!TryGetLoadUriWithKeyAndTags<T>(address, out var loadUri))
			{
				return Task.FromResult<T>(default);
			}

			var obj = AssetDatabase.LoadAssetAtPath(loadUri, typeof(T));
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

		/// <summary>
		/// 检查key和标签, 返回加载路径
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="address"></param>
		/// <returns></returns>
		private bool TryGetLoadUriWithKeyAndTags<T>(string address, out string loadUri)
		{
			// var groupInfo = AAPathInfo.ParseGroupName(PathInfo, address, AssetDatabase.AssetPathToGUID(address));
			// if (groupInfo == null)
			// {
			// 	return false;
			// }
			var b1 = this.DepCollector.TryGetLoadUri(address, out loadUri);
			if (!b1)
			{
				MyLogger.LogError($"asset key not exist in any bundle1: {address}");
				return false;
			}

			return true;
		}

		public Task UnloadAssetJust(string address)
		{
			return Task.CompletedTask;
		}

		public Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus) where T : UnityEngine.Object
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
			if (TryGetLoadUriWithKeyAndTags<UnityEngine.Object>(address, out var loadUri))
			{
				var existFile = File.Exists(loadUri);
				return existFile;
			}
			else
			{
				return false;
			}
		}

		public bool ExistGuid(string guid)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var exist = !string.IsNullOrEmpty(path);
			return exist;
		}

		public Task<T> LoadAssetByRefer<T>(string address, AssetLoadStatusGroup loadStatus) where T : UnityEngine.Object
		{
			return LoadAssetJust<T>(address, loadStatus);
		}

		public Task<T> LoadAssetByReferSync<T>(string address, AssetLoadStatusGroup loadStatus)
			where T : UnityEngine.Object
		{
			return LoadAssetJust<T>(address, loadStatus);
		}

		public Task UnLoadAssetByRefer(string address)
		{
			return Task.CompletedTask;
		}

		public async Task<Scene> LoadScene(string sceneAddress, LoadSceneParameters parameters,
			AssetLoadStatusGroup loadStatus)
		{
			if (!TryGetLoadUriWithKeyAndTags<Scene>(sceneAddress, out var loadUri))
			{
				return default;
			}

			var op = EditorSceneManager.LoadSceneAsyncInPlayMode(loadUri, parameters);
			var subStatus = loadStatus?.AddAsyncOperationStatus(op);
			await op.GetTask();
			var scene = SceneManager.GetSceneByPath(loadUri);
			return scene;
		}

		public Task UnLoadScene(string sceneAddress, UnloadSceneOptions options)
		{
			var op = SceneManager.UnloadSceneAsync(sceneAddress, options);
			return op.GetTask();
		}

		public Task<Scene> LoadSceneByRefer(string sceneAddress, LoadSceneParameters parameters,
			AssetLoadStatusGroup loadStatus)
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

		public bool IsAssetBundlesOfAssetLoaded(string address)
		{
			return true;
		}

		public void RunDelayedTasks()
		{
		}

		public bool TryGetExtraAddressInfo(string address, out ExtraAddressInfo extraAddressInfo)
		{
			return DepCollector.ExtraAddressInfoMap.TryGetValue(address, out extraAddressInfo);
		}

		public Task<bool> CleanAllCaches()
		{
			Debug.Log("编辑器清理缓存");
			return Task.FromResult(true);
		}

		public void Dispose()
		{
		}
	}
}