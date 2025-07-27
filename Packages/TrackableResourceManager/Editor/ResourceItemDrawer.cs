using System.IO;
using TrackableResourceManager.Runtime;
using UnityEditor;
using UnityEngine;
using MiiAsset.AssetWeakRefer.Runtime;

namespace TrackableResourceManager.Editor
{
	[CustomPropertyDrawer(typeof(ResourceManifestConfig.ResourceItem))]
	public class ResourceItemDrawer : PropertyDrawer
	{
		private const int DivHeight = 2;
		private const int LabelHeight = 20;

		protected string ConflictKey;
		protected string ConflictGuid;

		protected static string ChangedGuid = "";

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var keyProp = property.FindPropertyRelative("key");
			var resReferProp = property.FindPropertyRelative("resRefer");

			var windowWidth = EditorGUIUtility.currentViewWidth;
			var keyRect = new Rect(position.x, position.y+DivHeight, windowWidth - 160, LabelHeight);
			var copyButtonRect = new Rect(keyRect.xMax + DivHeight, keyRect.y, 65, LabelHeight);
			var resReferRect = new Rect(position.x, keyRect.yMax - DivHeight, position.width,
				EditorGUI.GetPropertyHeight(resReferProp));

			var isAnyChanged = false;
			EditorGUI.BeginChangeCheck();

			var obj0 = (AssetReference)resReferProp.boxedValue;
			var guid0 = obj0.AssetGUID;
			var isKeyEditable = string.IsNullOrEmpty(ConflictKey) || ConflictGuid != guid0;
			var key0 = (string)keyProp.boxedValue;
			var displayKey = isKeyEditable ? key0 : ConflictKey;
			GUI.enabled = isKeyEditable;
			var key1 = EditorGUI.TextField(keyRect, "key", displayKey);
			if (!isKeyEditable)
			{
				key1 = key0;
			}

			if (key1.Length > 32)
			{
				Debug.LogError($"过长的key，不能超过32个字符，超过部分将被消去: {key1}");
				key1 = key1.Substring(0, 32);
			}

			if (EditorGUI.EndChangeCheck())
			{
				isAnyChanged = true;
			}

			GUI.enabled = true;

			var click = GUI.Button(copyButtonRect, "复制UKey");
			if (click)
			{
				var value = (ResourceManifestConfig.ResourceItem)property.boxedValue;
				GroupConfigEditor.CopyResourceItemUKey(value.ResUri);
			}

			var isObjChanged = false;
			EditorGUI.BeginChangeCheck();

			EditorGUI.PropertyField(resReferRect, resReferProp, new GUIContent("引用资源"));

			if (EditorGUI.EndChangeCheck())
			{
				isAnyChanged = true;
				isObjChanged = true;
			}

			var obj = (AssetReference)resReferProp.boxedValue;
			var guid = obj.AssetGUID;
			if (isAnyChanged || ChangedGuid == guid)
			{
				if (isAnyChanged)
				{
					ChangedGuid = guid;
				}

				var assetPath = AssetDatabase.GUIDToAssetPath(guid);

				var asset = (ResourceManifestConfig)property.serializedObject.targetObject;
				var value = (ResourceManifestConfig.ResourceItem)property.boxedValue;
				if (asset.ResolveNamespace(guid, value.key, asset.SortOrder, out var conflictKey))
				{
					if (isObjChanged && string.IsNullOrWhiteSpace(key1))
					{
						key1 = ResourceManifestConfig.FormatKey(Path.GetFileName(assetPath));
					}

					keyProp.boxedValue = key1;

					var assetPathToGuid =
						AssetDatabase.AssetPathToGUID(
							AssetDatabase.GetAssetPath(property.serializedObject.targetObject));

					if (value is not ResourceManifestConfig.EnumResourceItem)
					{
						asset.group.RemoveKey(guid, asset.SortOrder, assetPathToGuid);
						asset.group.AddKey(key1, guid, asset.SortOrder, assetPathToGuid);
					}

					ConflictKey = null;
					ConflictGuid = guid;
				}
				else
				{
					ConflictKey = conflictKey;
					ConflictGuid = guid;
					keyProp.boxedValue = "";
				}
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var keyProp = property.FindPropertyRelative("key");
			var resReferProp = property.FindPropertyRelative("resRefer");

			return LabelHeight + DivHeight + EditorGUI.GetPropertyHeight(resReferProp);
		}
	}
}