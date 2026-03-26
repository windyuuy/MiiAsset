using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MiiAsset.Runtime
{
	public class GroupScanInfo
	{
		public string ScanRoot;
		public bool IsFolder = false;
		public AAPathConfigItem Item;
	}

	[Serializable]
	public class AAPathConfigItem
	{
		public string title;
		public string path;
		[HideInInspector] public Regex pathRegex;
		public string groupName;

		public string groupRoot;
		public string scanRoot;

		// split with ;
		public string tags;

		public bool isDisabled = false;

		public bool isEncrypt = false;

		/// <summary>
		/// 是否打进包内
		/// </summary>
		public bool isOffline = true;

		public GroupScanInfo GetScanRootInfo(bool analyzeOnly)
		{
			if (Application.isPlaying && !analyzeOnly)
			{
				throw new NotImplementedException("cannot build when is playing");
			}

			var groupInfo = new GroupScanInfo();
			groupInfo.IsFolder = Directory.Exists(ScanRoot);

			groupInfo.ScanRoot = ScanRoot;
			groupInfo.IsFolder = Directory.Exists(ScanRoot);
			groupInfo.Item = this;

			return groupInfo;
		}

		internal string ScanRoot
		{
			get
			{
				if (string.IsNullOrEmpty(scanRoot))
				{
					var m2 = new Regex(@"^\(*([a-zA-Z_\/]*)[\/]").Match(this.path);
					if (m2.Success)
					{
						scanRoot = m2.Groups[1].Value;
					}
				}

				return scanRoot;
			}
		}
	}

	[Serializable]
	public class AASingleFileItem
	{
		public string group;
		public string key;
		public UnityEngine.Object asset;
		public bool isEncrypt = false;
		public bool isOffline = false;


		public string GetGroupName()
		{
			return group.Replace("/", "_").ToLower();
		}
	}

	[CreateAssetMenu(fileName = "AAPathConfig", menuName = "AppConfig/AAPathConfig", order = 0)]
	public class AAPathConfig : ScriptableObject
	{
		/// <summary>
		/// 包含的路径
		/// </summary>
		/// <returns></returns>
		public List<AAPathConfigItem> paths = new List<AAPathConfigItem>();

		/// <summary>
		/// 排除的文件扩展名
		/// </summary>
		/// <returns></returns>
		public List<string> excludeExtensions = new List<string>();

		/// <summary>
		/// 排除的路径
		/// </summary>
		/// <returns></returns>
		public List<AAPathConfigItem> excludePaths = new List<AAPathConfigItem>();

		public bool isShaderGroupOffline = true;
		public bool isMyBuiltinShaderGroupOffline = true;

		public List<AASingleFileItem> singleFiles = new List<AASingleFileItem>();

		/// <summary>
		/// 加密所有文件
		/// </summary>
		public bool isEncryptAll = false;

		public bool isEncryptBuiltin = false;
	}

	public class AssetGroupNameInfo
	{
		public string GroupName;
		public string GroupRoot;
		public bool IsFolder = false;
		public string GroupRootRename;
		public string[] Tags;
		public bool IsEncrypt;
		public bool IsRemote;
		public string Guid;
		public string AssetPath;
	}
}