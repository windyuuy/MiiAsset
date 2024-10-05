using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework.MiiAsset.Runtime
{
	public interface IResourceLoadSource
	{
		public string GetSourceUri(string bundleName);
		public string GetCacheUri(string bundleName);
	}

	public class ResourceLoadSource : IResourceLoadSource
	{
		public ResourceLoadSource(string sourceBaseUri, string cacheBaseUri)
		{
			this.SourceBaseUri = sourceBaseUri;
			this.CacheBaseUri = cacheBaseUri;
		}

		public string SourceBaseUri;

		public string GetSourceUri(string bundleName)
		{
			if (SourceBaseUri == null)
			{
				return null;
			}

			return SourceBaseUri + bundleName;
		}

		public string CacheBaseUri;

		public string GetCacheUri(string bundleName)
		{
			if (CacheBaseUri == null)
			{
				return null;
			}

			return CacheBaseUri + bundleName;
		}
	}

	public interface IAssetBundleLoadStatus
	{
		public Task<PipelineResult> Task { get; }
		public int RefCount { get; set; }
		public AssetBundle AssetBundle { get; }
		Task<PipelineResult> Load(CatalogInfo catalogInfo);
		Task UnLoad();
		Task<T> LoadAssetJust<T>(string address);
		Task<T> LoadAssetByRefer<T>(string address);
		Task UnLoadAssetByRefer(string address);
		Task UnLoadAssetJust(string address);
	}

	public class LoadAssetStatus
	{
		public Task<object> Task;
		public int RefCount;
		public string Address;
	}

	public class AssetBundleLoadStatus : IAssetBundleLoadStatus
	{
		public string BundleName;

		public AssetBundleLoadStatus(string bundleName)
		{
			this.BundleName = bundleName;
		}

		public Task<PipelineResult> Task { get; set; } = null;
		public int RefCount { get; set; }

		internal ILoadAssetBundlePipeline LoadAssetBundlePipeline;

		public AssetBundle AssetBundle { get; set; }

		public Task<PipelineResult> Load(CatalogInfo catalogInfo)
		{
			Debug.Assert(RefCount > 0, $"Bundle is not allowed: {this.BundleName}");
			if (AssetBundle == null)
			{
				if (Task == null || (Task.IsCompleted && !Task.IsCompletedSuccessfully))
				{
					Task = LoadInternal(catalogInfo);
				}
			}

			return Task;
		}

		public PipelineResult Result = new();

		async Task<PipelineResult> LoadInternal(CatalogInfo catalogInfo)
		{
			var unloadTask = UnloadTask;
			if (unloadTask != null)
			{
				await unloadTask;
			}

			var bundleInfo = catalogInfo.GetAssetBundleInfo(BundleName);
			var loadSource = catalogInfo.BundleLoadSourceMap[BundleName];
			using (var loadAssetBundlePipeline = bundleInfo.GetLoadAssetBundlePipeline(loadSource))
			{
				this.LoadAssetBundlePipeline = loadAssetBundlePipeline;

				var result = await loadAssetBundlePipeline.Run();
				Result.Merge(result);
				var assetBundle = loadAssetBundlePipeline.AssetBundle;
				Debug.Assert(this.AssetBundle == null, $"this.AssetBundle==null, {this.BundleName}");
				this.AssetBundle = assetBundle;
				this.LoadAssetBundlePipeline = null;
			}

			if (this.AssetBundle == null)
			{
				Debug.LogError($"invalid assetbundle: {BundleName}");
			}

			return Result;
		}

		internal Task UnloadTask;

		public async Task UnLoad()
		{
			Debug.Assert(RefCount == 0);
			if (LoadAssetBundlePipeline != null)
			{
				await Task;
			}

			if (AssetBundle != null)
			{
				UnloadTask = this.AssetBundle.UnloadAsync(true).GetTask();
				await UnloadTask;
				UnloadTask = null;
			}
		}

		public async Task<T> LoadAssetJust<T>(string address)
		{
			if (AssetBundle == null && !Task.IsCompletedSuccessfully)
			{
				throw new Exception($"assetbundle not load yet: {BundleName}, cannot load by asset key: {address}");
			}

			var op = AssetBundle.LoadAssetAsync<T>(address);
			var task = op.GetTask();
			await task;
			if (op.asset is T asset)
			{
				return asset;
			}
			else
			{
				throw new InvalidCastException($"invalid asset Type<{nameof(T)}> to load: {address}");
			}
		}

		public Task UnLoadAssetJust(string address)
		{
			return System.Threading.Tasks.Task.CompletedTask;
		}

		protected Dictionary<string, LoadAssetStatus> AssetStatusMap = new();

		public LoadAssetStatus GetOrCreateAssetStatus(string address)
		{
			if (!AssetStatusMap.TryGetValue(address, out var status))
			{
				status = new()
				{
					Address = address,
				};
				AssetStatusMap.Add(address, status);
			}

			return status;
		}

		public async Task<T> LoadAssetByRefer<T>(string address)
		{
			var op = AssetBundle.LoadAssetAsync<T>(address);
			var task = op.GetTask();
			var status = GetOrCreateAssetStatus(address);
			status.Task = task;
			++status.RefCount;
			await task;
			if (op.asset is T asset)
			{
				return asset;
			}
			else
			{
				throw new InvalidCastException($"invalid asset Type<{nameof(T)}> to load: {address}");
			}
		}

		public Task UnLoadAssetByRefer(string address)
		{
			var status = GetOrCreateAssetStatus(address);
			--status.RefCount;
			return System.Threading.Tasks.Task.CompletedTask;
		}
	}

	public class CatalogStatus
	{
		public Dictionary<string, int> AllowedTags = new();
		public Dictionary<string, IAssetBundleLoadStatus> BundleLoadStatus = new();

		public IAssetBundleLoadStatus GetOrCreateLoadStatus(string bundleName)
		{
			if (!BundleLoadStatus.TryGetValue(bundleName, out var status))
			{
				status = new AssetBundleLoadStatus(bundleName);
				BundleLoadStatus.Add(bundleName, status);
			}

			return status;
		}

		public void AllowTags(IEnumerable<string> tags, CatalogInfo catalogInfo)
		{
			foreach (var tag in tags)
			{
				if (!AllowedTags.TryGetValue(tag, out var referCount))
				{
					referCount = 1;

					var bundleNames = catalogInfo.GetTagDependBundles(tag);
					if (bundleNames != null)
					{
						foreach (var bundleName in bundleNames)
						{
							var loadStatus = GetOrCreateLoadStatus(bundleName);
							loadStatus.RefCount++;
						}
					}
				}
				else
				{
					referCount++;
				}

				AllowedTags[tag] = referCount;
			}
		}

		public Task LoadTags(IEnumerable<string> tags, CatalogInfo catalogInfo)
		{
			var bundleNames = new HashSet<string>();
			catalogInfo.GetTagsDependBundles(tags, bundleNames);
			var tasks = bundleNames.Select(bundleName =>
			{
				var loadStatus = GetOrCreateLoadStatus(bundleName);
				return loadStatus.Load(catalogInfo);
			});
			return Task.WhenAll(tasks);
		}

		public Task UnloadTags(IEnumerable<string> tags, CatalogInfo catalogInfo)
		{
			var tasks = new List<Task>();
			foreach (var tag in tags)
			{
				if (AllowedTags.TryGetValue(tag, out var referCount))
				{
					if (referCount > 0)
					{
						referCount--;
						AllowedTags[tag] = referCount;
					}
					else
					{
						Debug.LogError($"tag referCount invalid: {tag}");
					}

					if (referCount == 0)
					{
						var bundleNames = catalogInfo.GetTagDependBundles(tag);
						foreach (var bundleName in bundleNames)
						{
							var bundleLoadStatus = GetOrCreateLoadStatus(bundleName);
							if (bundleLoadStatus.RefCount > 0)
							{
								bundleLoadStatus.RefCount--;
							}
							else
							{
								Debug.LogError($"bundle referCount invalid: {bundleName}");
							}

							if (bundleLoadStatus.RefCount == 0)
							{
								var task = bundleLoadStatus.UnLoad();
								tasks.Add(task);
							}
						}
					}
				}
			}

			return Task.WhenAll(tasks);
		}

		public Task<PipelineResult[]> LoadBundles(HashSet<string> deps, CatalogInfo catalogInfo)
		{
			if (deps != null)
			{
				var task = Task.WhenAll(deps.Select(dep =>
				{
					var loadStatus = GetOrCreateLoadStatus(dep);
					return loadStatus.Load(catalogInfo);
				}));
				return task;
			}

			return Task.FromResult(Array.Empty<PipelineResult>());
		}

		public Task<PipelineResult[]> GetLoadingBundleTasks(HashSet<string> deps, CatalogInfo catalogInfo)
		{
			if (deps != null)
			{
				var task = Task.WhenAll(deps
					.Select(dep =>
					{
						var loadStatus = GetOrCreateLoadStatus(dep);
						return loadStatus.Task;
					})
					.Where(task => task != null));
				return task;
			}

			return Task.FromResult(Array.Empty<PipelineResult>());
		}

		public Task UnLoadBundles(HashSet<string> deps)
		{
			if (deps != null)
			{
				var task = Task.WhenAll(deps.Select(dep =>
				{
					var loadStatus = GetOrCreateLoadStatus(dep);
					return loadStatus.UnLoad();
				}));
				return task;
			}

			return Task.CompletedTask;
		}

		public Task LoadBundlesByRefer(HashSet<string> deps, CatalogInfo catalogInfo)
		{
			if (deps != null)
			{
				var task = Task.WhenAll(deps.Select(dep =>
				{
					var loadStatus = GetOrCreateLoadStatus(dep);
					++loadStatus.RefCount;
					return loadStatus.Load(catalogInfo);
				}));
				return task;
			}

			return Task.CompletedTask;
		}

		public Task UnLoadBundlesByRefer(HashSet<string> deps)
		{
			if (deps != null)
			{
				var task = Task.WhenAll(deps.Select(dep =>
				{
					var loadStatus = GetOrCreateLoadStatus(dep);
					--loadStatus.RefCount;
					if (loadStatus.RefCount == 0)
					{
						return loadStatus.UnLoad();
					}
					else
					{
						return Task.CompletedTask;
					}
				}));
				return task;
			}

			return Task.CompletedTask;
		}

		public IAssetBundleLoadStatus GetOrCreateLoadStatusByAddress(string address, CatalogInfo catalogInfo)
		{
			if (catalogInfo.AddressBundleMap.TryGetValue(address, out var fileName))
			{
				var bundleLoadStatus = this.GetOrCreateLoadStatus(fileName);
				return bundleLoadStatus;
			}
			else
			{
				throw new Exception($"asset not exist in any bundle: {address}");
			}
		}
	}
}