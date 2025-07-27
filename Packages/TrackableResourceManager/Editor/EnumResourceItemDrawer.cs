using TrackableResourceManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace TrackableResourceManager.Editor
{
	[CustomPropertyDrawer(typeof(ResourceManifestConfig.EnumResourceItem))]
	public class EnumResourceItemDrawer : PropertyDrawer
	{
		private const int DivHeight = 2;
		private const int LabelHeight = 20;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var keyProp = property.FindPropertyRelative("key");
			var resReferProp = property.FindPropertyRelative("resRefer");

			var keyRect = new Rect(position.x, position.y, position.width, LabelHeight);
			var resReferRect = new Rect(position.x, keyRect.yMax, position.width,
				EditorGUI.GetPropertyHeight(resReferProp));

			// EditorGUI.PropertyField(keyRect, keyProp, new GUIContent("key"));
			EditorGUI.BeginChangeCheck();
			var key1 = EditorGUI.TextField(keyRect, keyProp.displayName, keyProp.stringValue);
			if (EditorGUI.EndChangeCheck())
			{
				if (key1.Length > 32)
				{
					Debug.LogError($"过长的key，不能超过32个字符，超过部分将被消去: {key1}");
					key1 = key1.Substring(0, 32);
				}

				keyProp.stringValue = key1;
			}

			EditorGUI.PropertyField(resReferRect, resReferProp, new GUIContent("引用资源"));
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var keyProp = property.FindPropertyRelative("key");
			var resReferProp = property.FindPropertyRelative("resRefer");

			return LabelHeight + DivHeight + EditorGUI.GetPropertyHeight(resReferProp);
		}
	}
}