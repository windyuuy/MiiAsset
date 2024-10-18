using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MiiAsset.Runtime
{
	public class AAPathInfo
	{
		public List<AAPathConfigItem> Paths = new();

		/// <summary>
		/// 排除的文件扩展名
		/// </summary>
		/// <returns></returns>
		public List<string> ExcludeExtensions = new List<string>();

		/// <summary>
		/// 排除的路径
		/// </summary>
		/// <returns></returns>
		public List<AAPathConfigItem> ExcludePaths = new List<AAPathConfigItem>();

		public bool IsShaderGroupOffline = true;
		public bool IsMyBuiltinShaderGroupOffline = true;

		public IEnumerable<GroupScanInfo> GetScanRootInfos()
		{
			return Paths.Select(p => p.GetScanRootInfo()).Distinct();
		}

		/// <summary>
		/// 解出组名
		/// </summary>
		/// <param name="config"></param>
		/// <param name="assetPath"></param>
		/// <returns></returns>
		public static GroupNameInfo ParseGroupName(AAPathConfigItem config, string assetPath, string guid)
		{
			if (string.IsNullOrEmpty(config.path))
			{
				return null;
			}

			var m = config.pathRegex.Match(assetPath);
			if (!m.Success)
			{
				m = config.pathRegex.Match(assetPath + "/");
			}

			if (m.Success)
			{
				var groupInfo = new GroupNameInfo();
				groupInfo.Guid = guid;
				groupInfo.AssetPath = assetPath;
				var ss = m.Groups.Select(g => g.Value).ToArray();
				try
				{
					groupInfo.GroupName = string.Format(config.groupName, ss);
					var groupDefs = config.groupRoot.Split(" -> ", StringSplitOptions.RemoveEmptyEntries);
					if (groupDefs.Length >= 1)
					{
						var groupRoot = string.Format(groupDefs[0], ss);
						var m2 = new Regex(@"^(.*?)([\\/]*\*)$").Match(groupRoot);
						if (m2.Success)
						{
							groupInfo.IsFolder = true;
							groupRoot = m2.Groups[1].Value;
						}

						groupInfo.GroupRoot = groupRoot;
						if (groupDefs.Length >= 2)
						{
							groupInfo.GroupRootRename = string.Format(groupDefs[1], ss);
						}
						else
						{
							groupInfo.GroupRootRename = null;
						}
					}

					if (config.tags == null)
					{
						groupInfo.Tags = Array.Empty<string>();
					}
					else
					{
						groupInfo.Tags = string.Format(config.tags, ss)
							.Split(";", StringSplitOptions.RemoveEmptyEntries);
					}

					groupInfo.Tags = groupInfo.Tags.Prepend(groupInfo.GroupName).ToArray(); 

					groupInfo.IsRemote = !config.isOffline;
					return groupInfo;
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

			return null;
		}
	}
}