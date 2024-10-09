using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Framework.MiiAsset.Runtime.Status;
using UnityEngine.SceneManagement;

namespace Framework.MiiAsset.Runtime
{
	public class NotInTagsBundleSetException : Exception
	{
	}

	public class TagsBundleSet
	{
		protected IAssetProvider AssetProvider;

		public TagsBundleSet(IAssetProvider assetProvider, IEnumerable<string> tags)
		{
			AssetProvider = assetProvider;
			if (tags is string[] tagsArray)
			{
				Tags = tagsArray;
			}
			else
			{
				Tags = tags.ToArray();
			}
		}

		public string[] Tags { get; internal set; }

		public bool AllowTags()
		{
			return AssetProvider.AllowTags(Tags);
		}

		public Task LoadTags(AssetLoadStatusGroup loadStatus = null)
		{
			return AssetProvider.LoadTags(Tags, loadStatus);
		}

		public Task UnLoadTags()
		{
			return AssetProvider.UnLoadTags(Tags);
		}

		public long GetDownloadSize()
		{
			return AssetProvider.GetDownloadSize(Tags);
		}

		public Task<T> LoadAsset<T>(string address, AssetLoadStatusGroup loadStatus = null)
		{
			if (AssetProvider.IsAddressInTags(address, Tags))
			{
				return AssetProvider.LoadAsset<T>(address, loadStatus);
			}
			else
			{
				throw new NotInTagsBundleSetException();
			}
		}

		public Task UnLoadAsset(string address)
		{
			return AssetProvider.UnLoadAsset(address);
		}

		public Task LoadScene(string sceneAddress, LoadSceneParameters parameters = new(), AssetLoadStatusGroup loadStatus = null)
		{
			if (AssetProvider.IsAddressInTags(sceneAddress, Tags))
			{
				return AssetProvider.LoadScene(sceneAddress, parameters, loadStatus);
			}
			else
			{
				throw new NotInTagsBundleSetException();
			}
		}

		public Task UnLoadScene(string sceneAddress)
		{
			return AssetProvider.UnLoadScene(sceneAddress);
		}

		public Task DownloadTags(AssetLoadStatusGroup loadStatus = null)
		{
			return AssetProvider.DownloadTags(Tags, loadStatus);
		}
	}
}