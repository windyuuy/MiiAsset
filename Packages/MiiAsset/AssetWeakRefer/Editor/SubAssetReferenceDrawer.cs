using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MiiAsset.AssetWeakRefer.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MiiAsset.AssetWeakRefer.Editor
{
	[CustomPropertyDrawer(typeof(SubAssetReference), true)]
	class SubAssetReferenceDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property == null || label == null)
			{
				Debug.LogError("Error rendering drawer for SubAssetReference property.");
				return;
			}

			string labelText = label.text;
			var prop = property.GetActualObjectForSerializedProperty<SubAssetReference>(fieldInfo, ref labelText);

			labelText = ObjectNames.NicifyVariableName(labelText);
			if (labelText != label.text || string.IsNullOrEmpty(label.text))
			{
				label = new GUIContent(labelText, label.tooltip);
			}

			if (prop == null)
			{
				return;
			}

			EditorGUI.BeginChangeCheck();

			var t = GetGenericType();

			var assetDropDownRect = EditorGUI.PrefixLabel(position, label);
			var mainAssetRect = position;
			mainAssetRect.width = assetDropDownRect.xMin - position.xMin - 2;
			EditorGUI.BeginProperty(mainAssetRect, label, property);

			var objSelect = EditorGUI.ObjectField(mainAssetRect, GUIContent.none, prop.EditorAsset, t, false);
			Object subObjSelect;
			if (objSelect != null)
			{
				var subAssetsList = GetSubAssetsList(prop);
				var lastName = prop.EditorSubAssetGuid??"";
				var spriteNames = subAssetsList.Select(s => s?.name).ToArray();
				spriteNames[0] = "";
				var lastIndex = Array.FindIndex(spriteNames, (s) => s == lastName);
				spriteNames[0] = "<none>";
				var index = EditorGUI.Popup(assetDropDownRect, lastIndex, spriteNames);
				if (index >= 0)
				{
					subObjSelect = subAssetsList[index];
				}
				else
				{
					subObjSelect = null;
				}
			}
			else
			{
				subObjSelect = null;
			}

			EditorGUI.EndProperty();
			if (EditorGUI.EndChangeCheck())
			{
				if (prop.SetEditorSubObject(objSelect, subObjSelect))
				{
					EditorUtility.SetDirty(property.serializedObject.targetObject);
				}
			}
		}

		static Type GetGenericTypeFromAssetReference(AssetReference assetReferenceObject)
		{
			var type = assetReferenceObject?.GetType();
			while (type != null)
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AssetReferenceT<>))
					return type.GenericTypeArguments[0];
				type = type.BaseType;
			}

			return null;
		}

		static internal List<Object> GetSubAssetsList(AssetReference assetReferenceObject)
		{
			var subAssets = new List<Object>();
			subAssets.Add(null);
			var assetPath = AssetDatabase.GUIDToAssetPath(assetReferenceObject.AssetGUID);

			var repr = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
			if (repr.Any())
			{
				var subtype = GetGenericTypeFromAssetReference(assetReferenceObject);
				if (subtype != null)
					repr = repr.Where(o => subtype.IsInstanceOfType(o)).OrderBy(s => s.name).ToArray();
			}

			subAssets.AddRange(repr);

			var mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
			if (mainType == typeof(SpriteAtlas))
			{
				var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);
				var sprites = new Sprite[atlas.spriteCount];
				atlas.GetSprites(sprites);
				subAssets.AddRange(sprites.OrderBy(s => s.name));
			}

			return subAssets;
		}

		private Type GetGenericType()
		{
			Type t;
			if (fieldInfo.FieldType.IsGenericType)
			{
				t = fieldInfo.FieldType.GenericTypeArguments[0];
			}
			else if (fieldInfo.FieldType.BaseType?.IsGenericType ?? false)
			{
				t = fieldInfo.FieldType.BaseType.GenericTypeArguments[0];
			}
			else
			{
				t = typeof(Object);
			}

			return t;
		}
	}
}