using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiiAsset.Runtime.Status;
using UnityEngine;

namespace MiiAsset.Runtime
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

	public class CatalogStatus : IDisposable
	{
		public Dictionary<string, int> AllowedTags = new();
		public Dictionary<string, IAssetBundleStatus> BundleLoadStatus = new();

		public IAssetBundleStatus GetOrCreateStatus(string bundleName)
		{
			if (!BundleLoadStatus.TryGetValue(bundleName, out var status))
			{
				status = new AssetBundleStatus(bundleName);
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
							var loadStatus = GetOrCreateStatus(bundleName);
							++loadStatus.RefCount;
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

		public Task<PipelineResult[]> DownloadTags(IEnumerable<string> tags, CatalogInfo catalogInfo, AssetLoadStatusGroup loadStatus)
		{
			var bundleNames = new HashSet<string>();
			catalogInfo.GetTagsDependBundles(tags, bundleNames);
			var tasks = bundleNames.Select(bundleName =>
			{
				var status = GetOrCreateStatus(bundleName);
				return status.Download(catalogInfo);
			});

			if (loadStatus != null)
			{
				foreach (var status in bundleNames.Select(GetOrCreateStatus))
				{
					loadStatus.Add(status);
				}
			}

			var downloadTask = Task.WhenAll(tasks);
			return downloadTask;
		}

		public Task<PipelineResult[]> LoadTags(IEnumerable<string> tags, CatalogInfo catalogInfo, AssetLoadStatusGroup loadStatus)
		{
			var bundleNames = new HashSet<string>();
			catalogInfo.GetTagsDependBundles(tags, bundleNames);
			var tasks = bundleNames.Select(bundleName =>
			{
				var status = GetOrCreateStatus(bundleName);
				return status.Load(catalogInfo);
			});

			if (loadStatus != null)
			{
				foreach (var status in bundleNames.Select(GetOrCreateStatus))
				{
					loadStatus.Add(status);
				}
			}

			return Task.WhenAll(tasks);
		}

		public bool IsAddressInTags(string address, IEnumerable<string> tags, CatalogInfo catalogInfo)
		{
			if (catalogInfo.AddressBundleMap.TryGetValue(address, out var fileName))
			{
				return IsBundleInTags(fileName, tags, catalogInfo);
			}
			else
			{
				Debug.LogError($"asset not exist in any bundle2: {address}");
			}

			return false;
		}

		public bool IsBundleInTags(string bundleFileName, IEnumerable<string> tags, CatalogInfo catalogInfo)
		{
			var depBundles = new HashSet<string>();
			catalogInfo.GetTagsDependBundles(tags, depBundles);
			if (depBundles.Contains(bundleFileName))
			{
				return true;
			}

			return false;
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
							var bundleLoadStatus = GetOrCreateStatus(bundleName);
							if (bundleLoadStatus.RefCount > 0)
							{
								--bundleLoadStatus.RefCount;
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

		public Task<PipelineResult[]> LoadBundles(HashSet<string> deps, CatalogInfo catalogInfo, AssetLoadStatusGroup loadStatus)
		{
			if (deps != null)
			{
				var task = Task.WhenAll(deps.Select(dep =>
				{
					var status = GetOrCreateStatus(dep);
					return status.Load(catalogInfo);
				}));

				if (loadStatus != null)
				{
					foreach (var status in deps.Select(GetOrCreateStatus))
					{
						loadStatus.Add(status);
					}
				}

				return task;
			}

			return Task.FromResult(Array.Empty<PipelineResult>());
		}

		public Task<PipelineResult[]> GetLoadingBundlesTasks(HashSet<string> deps, CatalogInfo catalogInfo)
		{
			if (deps != null)
			{
				var task = Task.WhenAll(deps
					.Select(dep =>
					{
						var loadStatus = GetOrCreateStatus(dep);
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
					var loadStatus = GetOrCreateStatus(dep);
					return loadStatus.UnLoad();
				}));
				return task;
			}

			return Task.CompletedTask;
		}

		public Task LoadBundlesByRefer(HashSet<string> deps, CatalogInfo catalogInfo, AssetLoadStatusGroup loadStatus)
		{
			if (deps != null)
			{
				var task = Task.WhenAll(deps.Select(dep =>
				{
					var status = GetOrCreateStatus(dep);
					++status.RefCount;
					return status.Load(catalogInfo);
				}));

				if (loadStatus != null)
				{
					foreach (var status in deps.Select(GetOrCreateStatus))
					{
						loadStatus.Add(status);
					}
				}

				return task;
			}

			return Task.CompletedTask;
		}

		public Task UnLoadBundlesByRefer(string address, HashSet<string> deps)
		{
			if (deps != null)
			{
				var task = Task.WhenAll(deps.Select(dep =>
				{
					var loadStatus = GetOrCreateStatus(dep);
					if (loadStatus.RefCount > 0)
					{
						--loadStatus.RefCount;
					}
					else
					{
						Debug.LogError($"bundle referCount invalid: [{dep}], caused with unloading[{address}]");
					}

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

		public IAssetBundleStatus GetOrCreateLoadStatusByAddress(string address, CatalogInfo catalogInfo)
		{
			if (catalogInfo.AddressBundleMap.TryGetValue(address, out var fileName))
			{
				var bundleLoadStatus = this.GetOrCreateStatus(fileName);
				return bundleLoadStatus;
			}
			else
			{
				Debug.LogError($"asset not exist in any bundle3: {address}");
				return null;
			}
		}

		public long GetDownloadSize(IEnumerable<string> tags, CatalogInfo catalogInfo)
		{
			var depBundles = new HashSet<string>();
			catalogInfo.GetTagsDependBundles(tags, depBundles);
			long size = 0;
			foreach (var dep in depBundles)
			{
				var loadStatus = GetOrCreateStatus(dep);
				size += loadStatus.GetDownloadSize(catalogInfo);
			}

			return size;
		}

		public void Dispose()
		{
			foreach (var bundleLoadStatus in this.BundleLoadStatus)
			{
				bundleLoadStatus.Value.Dispose();
			}

			this.BundleLoadStatus.Clear();
			this.AllowedTags.Clear();
		}
	}
}