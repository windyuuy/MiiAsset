using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEngine.Build.Pipeline;
using System.Security.Cryptography;
using System.IO;
using System.Text;

namespace MiiAsset.Editor.Build
{
	public class BuildAssetBundlesResult
	{
		public string Msg;
		public ReturnCode Code;
		public BuildResult BuildResult;

		public bool IsOk => Code switch
		{
			ReturnCode.Success => true,
			ReturnCode.SuccessCached => true,
			ReturnCode.SuccessNotRun => true,
			_ => false
		};
	}

	public class ExtraBuildOptions
	{
		public string UpdateTunnel = "";
		public string CatalogType = "";
		public bool BuildGuids = false;
		public bool BuildHintCode = true;
	}

	[Serializable]
	public class WriteRecords
	{
		public List<string> records = new();
	}

	public class TagBundle
	{
		public HashSet<string> Guids = new();
		public string[] Tags;
		public string[] TagsAdditional;

		/// <summary>
		/// for debug
		/// </summary>
		public string[] Addresses => Guids.Select(guid => AssetDatabase.GUIDToAssetPath((string)guid)).ToArray();

		public string TagsUKey;
		public string BundleFileName => $"{GetTagsKey()}_{BuildInfo.Hash}{BundleFileHash}.bundle";
		public HashSet<string> DepTagNames = new();
		public HashSet<string> Deps = new();
		public BundleDetails BuildInfo;
		public string BundleFileHash { get; private set; }
		string GetHash(string path)
		{
			var hash = SHA1.Create();
			var stream = new FileStream(path, FileMode.Open);
			byte[] hashByte = hash.ComputeHash(stream);
			stream.Close();
			var fileHash = BitConverter.ToString(hashByte).Replace("-", "");
			return fileHash;
		}
		string GetSHA1(string path)
		{
			FileStream file = new FileStream(path, FileMode.Open);
			SHA1 sha1 = new SHA1CryptoServiceProvider();
			byte[] retval = sha1.ComputeHash(file);
			file.Close();

			StringBuilder sc = new StringBuilder();
			for (int i = 0; i < retval.Length; i++)
			{
				sc.Append(retval[i].ToString("x2"));
			}
			return sc.ToString();
		}
		public void UpdateFileHashName()
		{
			var fileHash = GetHash(BuildInfo.FileName).ToLower();
			BundleFileHash = fileHash;
		}
		public bool IsOffline = false;

		/// <summary>
		/// 多少byte
		/// </summary>
		public long FileSize;

		public string GetTagsKey()
		{
			return string.Join("_", Tags);
		}

		public string GetBundleName()
		{
			return $"{GetTagsKey()}";
		}

		public string[] GetAssetNames()
		{
			return Guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();
		}

		public string[] GetAssetAddresses()
		{
			return GetAssetNames();
		}

		public string GetBundlePathWithHash(string dir)
		{
			return $"{dir}/{this.BundleFileName}";
		}
	}
}