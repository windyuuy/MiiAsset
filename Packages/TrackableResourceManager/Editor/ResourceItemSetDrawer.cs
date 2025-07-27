using System;
using System.Linq;
using TrackableResourceManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace TrackableResourceManager.Editor
{
	[CustomPropertyDrawer(typeof(ResourceManifestConfig.ResourceItemSet), true)]
	public class ResourceItemSetDrawer : PropertyDrawer
	{
		private const int DivHeight = 2;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var setNameProp = property.FindPropertyRelative(nameof(ResourceManifestConfig.ResourceItemSet.setName));
			var enumItemsProp = property.FindPropertyRelative("enumItems");
			var scanRulesProp = property.FindPropertyRelative("scanRules");
			var itemsProp = property.FindPropertyRelative(nameof(ResourceManifestConfig.ResourceItemSet.outputItems));
			var joinKeyProp = property.FindPropertyRelative(nameof(ResourceManifestConfig.ResourceItemSet.joinKey));

			// EditorGUI.BeginFoldoutHeaderGroup(position, true, "Item Set Items");
			// EditorGUI.EndFoldoutHeaderGroup();

			var startX = position.x + 5;
			var setNameRect = new Rect(startX, position.y + 15, position.width, 15);
			EditorGUI.PropertyField(setNameRect, setNameProp);

			var height = EditorGUI.GetPropertyHeight(enumItemsProp);
			var enumItemsRect = new Rect(startX, setNameRect.yMax + DivHeight, position.width, height);
			EditorGUI.PropertyField(enumItemsRect, enumItemsProp);

			height = EditorGUI.GetPropertyHeight(scanRulesProp);
			var scanRulesRect = new Rect(startX, enumItemsRect.yMax + DivHeight, position.width, height);
			EditorGUI.PropertyField(scanRulesRect, scanRulesProp);

			height = EditorGUI.GetPropertyHeight(joinKeyProp);
			var joinKeyPropRect = new Rect(startX, scanRulesRect.yMax + DivHeight, position.width, height);
			EditorGUI.PropertyField(joinKeyPropRect, joinKeyProp);

			height = EditorGUI.GetPropertyHeight(itemsProp);
			var itemsPropRect = new Rect(startX, joinKeyPropRect.yMax + DivHeight, position.width, height);
			EditorGUI.PropertyField(itemsPropRect, itemsProp);

			height = EditorGUI.GetPropertyHeight(itemsProp);
			var updateManifestButtonRect = new Rect(startX, itemsPropRect.y + height + DivHeight, position.width,
				20);
			{
				EditorGUI.BeginChangeCheck();
				var clicked = GUI.Button(updateManifestButtonRect, "更新清单");
				if (EditorGUI.EndChangeCheck())
				{
					if (clicked)
					{
						var obj = property.boxedValue;
						var asset = (ResourceManifestConfig)property.serializedObject.targetObject;
						var value = (ResourceManifestConfig.ResourceItemSet)obj;
						var assetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);

						value.outputItems = Array.Empty<ResourceManifestConfig.ResourceItem>();
						var resources = value.SearchMatchedResources(assetPath).ToArray();

						itemsProp.arraySize = resources.Length;
						for (var i = 0; i < resources.Length; i++)
						{
							var itemProp = itemsProp.GetArrayElementAtIndex(i);
							var resource = resources[i];
							var isKeyEmpty = string.IsNullOrWhiteSpace(resource.key);
							if (isKeyEmpty && asset.ResolveNamespace(resource.ResUri, resource.DefaultKey,
									asset.SortOrder,
									out _))
							{
								itemProp.boxedValue = new ResourceManifestConfig.ResourceItem
								{
									key = resource.DefaultKey,
									resRefer = resource.resRefer,
								};
							}
							else if (!isKeyEmpty && asset.ResolveNamespace(resource.ResUri, resource.key,
										 asset.SortOrder,
										 out _))
							{
								itemProp.boxedValue = resource;
							}
							else
							{
								itemProp.boxedValue = new ResourceManifestConfig.ResourceItem
								{
									key = "",
									resRefer = resource.resRefer,
								};
							}
						}
						GroupConfigEditor.InjectToAAGroups(asset.group);
					}
				}
			}

			height = 20;
			var previewButtonRect = new Rect(startX, updateManifestButtonRect.y + height + DivHeight, position.width,
				20);
			{
				EditorGUI.BeginChangeCheck();
				var clicked = GUI.Button(previewButtonRect, "导出分组");
				// var clicked = EditorGUI.LinkButton(buttonRect, "更新按钮");
				if (EditorGUI.EndChangeCheck() && clicked)
				{
					var asset = (ResourceManifestConfig)property.serializedObject.targetObject;
					GroupConfigEditor.PreviewGroupConfig(asset.group);
				}
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// return base.GetPropertyHeight(property, label);
			var setName = property.FindPropertyRelative(nameof(ResourceManifestConfig.ResourceItemSet.setName));
			var enumItems = property.FindPropertyRelative("enumItems");
			var scanRules = property.FindPropertyRelative("scanRules");
			var joinKey = property.FindPropertyRelative(nameof(ResourceManifestConfig.ResourceItemSet.joinKey));
			var itemsProp = property.FindPropertyRelative(nameof(ResourceManifestConfig.ResourceItemSet.outputItems));

			var height = EditorGUI.GetPropertyHeight(setName);
			var height2 = EditorGUI.GetPropertyHeight(enumItems);
			var height3 = EditorGUI.GetPropertyHeight(scanRules);
			var height4 = EditorGUI.GetPropertyHeight(joinKey);
			var height5 = EditorGUI.GetPropertyHeight(itemsProp);
			return 15 + height + DivHeight + height2 + DivHeight + height3 + DivHeight + height4 + DivHeight + height5 + DivHeight + 20 +
				   DivHeight + 20 + DivHeight;
		}
	}
}