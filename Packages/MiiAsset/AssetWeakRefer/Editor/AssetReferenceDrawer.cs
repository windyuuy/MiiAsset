using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AssetWeakRefer.Runtime;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.AddressableAssets.GUI
{
	[CustomPropertyDrawer(typeof(AssetReference), true)]
	class AssetReferenceDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property == null || label == null)
			{
				Debug.LogError("Error rendering drawer for AssetReference property.");
				return;
			}

			string labelText = label.text;
			var prop = property.GetActualObjectForSerializedProperty<AssetReference>(fieldInfo, ref labelText);

			labelText = ObjectNames.NicifyVariableName(labelText);
			if (labelText != label.text || string.IsNullOrEmpty(label.text))
			{
				label = new GUIContent(labelText, label.tooltip);
			}

			if (prop == null)
			{
				return;
			}

			EditorGUI.BeginProperty(position, label, property);

			EditorGUI.BeginChangeCheck();
			var obj = prop.EditorAsset;

			var t = GetGenericType();

			var objSelect = EditorGUI.ObjectField(position, label, obj, t, false);

			if (EditorGUI.EndChangeCheck())
			{
				if (prop.SetEditorAsset(objSelect))
				{
					EditorUtility.SetDirty(property.serializedObject.targetObject);
				}
			}

			EditorGUI.EndProperty();
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

	/// <summary>
	/// Used to manipulate data from a serialized property.
	/// </summary>
	public static class SerializedPropertyExtensions
	{
		/// <summary>
		/// Used to extract the target object from a serialized property.
		/// </summary>
		/// <typeparam name="T">The type of the object to extract.</typeparam>
		/// <param name="property">The property containing the object.</param>
		/// <param name="field">The field data.</param>
		/// <param name="label">The label name.</param>
		/// <returns>Returns the target object type.</returns>
		public static T GetActualObjectForSerializedProperty<T>(this SerializedProperty property, FieldInfo field, ref string label)
		{
			try
			{
				if (property == null || field == null)
					return default(T);
				var serializedObject = property.serializedObject;
				if (serializedObject == null)
				{
					return default(T);
				}

				var targetObject = serializedObject.targetObject;

				if (property.depth > 0)
				{
					var slicedName = property.propertyPath.Split('.').ToList();
					List<int> arrayCounts = new List<int>();
					for (int index = 0; index < slicedName.Count; index++)
					{
						arrayCounts.Add(-1);
						var currName = slicedName[index];
#if NET_UNITY_4_8
                        if (currName.EndsWith(']'))
#else
						if (currName.EndsWith("]"))
#endif
						{
							var arraySlice = currName.Split('[', ']');
							if (arraySlice.Length >= 2)
							{
								arrayCounts[index - 2] = Convert.ToInt32(arraySlice[1]);
								slicedName[index] = string.Empty;
								slicedName[index - 1] = string.Empty;
							}
						}
					}

					while (string.IsNullOrEmpty(slicedName.Last()))
					{
						int i = slicedName.Count - 1;
						slicedName.RemoveAt(i);
						arrayCounts.RemoveAt(i);
					}

#if NET_UNITY_4_8
                    if (property.propertyPath.EndsWith(']'))
#else
					if (property.propertyPath.EndsWith("]"))
#endif
					{
						var slice = property.propertyPath.Split('[', ']');
						if (slice.Length >= 2)
							label = "Element " + slice[slice.Length - 2];
					}

					return DescendHierarchy<T>(targetObject, slicedName, arrayCounts, 0);
				}

				var obj = field.GetValue(targetObject);
				return (T)obj;
			}
			catch
			{
				return default(T);
			}
		}

		static T DescendHierarchy<T>(object targetObject, List<string> splitName, List<int> splitCounts, int depth)
		{
			if (depth >= splitName.Count)
				return default(T);

			var currName = splitName[depth];

			if (string.IsNullOrEmpty(currName))
				return DescendHierarchy<T>(targetObject, splitName, splitCounts, depth + 1);

			int arrayIndex = splitCounts[depth];

			var newField = targetObject.GetType().GetField(currName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			if (newField == null)
			{
				Type baseType = targetObject.GetType().BaseType;
				while (baseType != null && newField == null)
				{
					newField = baseType.GetField(currName,
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					baseType = baseType.BaseType;
				}
			}

			var newObj = newField.GetValue(targetObject);
			if (depth == splitName.Count - 1)
			{
				T actualObject = default(T);
				if (arrayIndex >= 0)
				{
					if (newObj.GetType().IsArray && ((System.Array)newObj).Length > arrayIndex)
						actualObject = (T)((System.Array)newObj).GetValue(arrayIndex);

					var newObjList = newObj as IList;
					if (newObjList != null && newObjList.Count > arrayIndex)
					{
						actualObject = (T)newObjList[arrayIndex];

						//if (actualObject == null)
						//    actualObject = new T();
					}
				}
				else
				{
					actualObject = (T)newObj;
				}

				return actualObject;
			}
			else if (arrayIndex >= 0)
			{
				if (newObj is IList)
				{
					IList list = (IList)newObj;
					newObj = list[arrayIndex];
				}
				else if (newObj is System.Array)
				{
					System.Array a = (System.Array)newObj;
					newObj = a.GetValue(arrayIndex);
				}
			}

			return DescendHierarchy<T>(newObj, splitName, splitCounts, depth + 1);
		}

		internal static string GetPropertyPathArrayName(string propertyPath)
		{
#if NET_UNITY_4_8
            if (propertyPath.EndsWith(']'))
#else
			if (propertyPath.EndsWith("]"))
#endif
			{
				int leftBracket = propertyPath.LastIndexOf('[');
				if (leftBracket > -1)
				{
					string arrayString = propertyPath.Substring(0, leftBracket);
					if (arrayString.EndsWith(".data", StringComparison.OrdinalIgnoreCase))
						return arrayString.Substring(0, arrayString.Length - 5); // remove ".data"
				}
			}

			return string.Empty;
		}
	}
}