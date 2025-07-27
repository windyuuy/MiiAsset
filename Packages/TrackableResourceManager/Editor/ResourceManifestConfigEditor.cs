using TrackableResourceManager.Runtime;
using UnityEditor;
using UnityEngine;

namespace TrackableResourceManager.Editor
{
	[CustomEditor(typeof(ResourceManifestConfig))]
	public class ResourceManifestConfigEditor: UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var isForCoderProp=serializedObject.FindProperty("isForCoder");
			var isForCoder = isForCoderProp.boolValue;
			if (isForCoder)
			{
				var click=GUILayout.Button("生成代码");
				base.OnInspectorGUI();
				click|=GUILayout.Button("生成代码");

				if (click)
				{
					var asset = (ResourceManifestConfig)serializedObject.targetObject;
					asset.GenerateCode();
				}
			}
			else
			{
				base.OnInspectorGUI();
			}
		}
		
	}
}