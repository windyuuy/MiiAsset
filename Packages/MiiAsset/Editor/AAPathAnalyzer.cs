using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MiiAsset.Runtime;
using UnityEngine;

namespace MiiAsset.Editor.Build
{
	public class AAPathAnalyzer
	{
		/// <summary>
		/// 解出组名
		/// </summary>
		/// <param name="aaPathConfigs"></param>
		/// <param name="assetPath"></param>
		/// <returns></returns>
		public static GroupNameInfo ParseGroupName(List<AAPathConfigItem> aaPathConfigs, string assetPath)
		{
			var groupInfo = new GroupNameInfo();

			{
				foreach (var config in aaPathConfigs)
				{
					if (string.IsNullOrEmpty(config.path))
					{
						continue;
					}

					var m = config.pathRegex.Match(assetPath);
					if (!m.Success)
					{
						m = config.pathRegex.Match(assetPath + "/");
					}

					if (m.Success)
					{
						var ss = m.Groups.Select(g => g.Value).ToArray();
						try
						{
							groupInfo.GroupName = string.Format(config.groupName, ss);
							var groupDefs = config.scanRoot.Split(" -> ", StringSplitOptions.RemoveEmptyEntries);
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

							groupInfo.IsRemote = !config.isOffline;
						}
						catch (Exception e)
						{
							Debug.LogException(e);
						}

						break;
					}
				}
			}

			return groupInfo;
		}
	}
}