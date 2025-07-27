using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrackableResourceManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace TrackableResourceManager.Editor
{
	[CustomEditor(typeof(AssetGroupConfig))]
	public class GroupConfigEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var needBuild = GUILayout.Button("导出分组");
			if (needBuild)
			{
				PreviewGroupConfig((AssetGroupConfig)this.target);
			}
		}

		public static void PreviewGroupConfig(AssetGroupConfig asset)
		{
			asset.InjectToAAGroups();
		}

		public static void InjectToAAGroups(AssetGroupConfig asset)
		{
			asset.InjectToAAGroups();
		}

		[MenuItem("Assets/CopyUKey", priority = 5)]
		public static void CopyResourceItemUKey()
		{
			var guid = Selection.assetGUIDs[0];
			CopyResourceItemUKey(guid);
		}

		[MenuItem("Assets/CopyGUID", priority = 5)]
		public static void CopyResourceItemGUID()
		{
			if (Selection.assetGUIDs.Length == 1)
			{
				GUIUtility.systemCopyBuffer = Selection.assetGUIDs[0];
			}
			else if (Selection.assetGUIDs.Length > 1)
			{
				var items = Selection.assetGUIDs.Select(guid =>
					$"{guid}|{Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid))}");
				var guids = string.Join(",", items);
				GUIUtility.systemCopyBuffer = guids;
			}
		}

		public static void CopyResourceItemUKey(string guid)
		{
			if (!Directory.Exists(AssetDatabase.GUIDToAssetPath(guid)))
			{
				if (AssetGroupConfig.GetEntryUKey(new(guid), out var uKey))
				{
					GUIUtility.systemCopyBuffer = uKey;
					Debug.Log($"复制uKey for {guid}: {uKey}");
				}
				else
				{
					Debug.LogError($"复制uKey失败 for {guid}");
				}
			}
			else
			{
				Debug.Log($"目录无法复制uKey for {guid}");
			}
		}
	}
}