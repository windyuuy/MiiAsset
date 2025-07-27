using System.Collections.Generic;

using Assets.Scripts.Editor;

using UnityEditor;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(MyShaderVariantWriter))]
public class MyShaderVariantWriterEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if (GUILayout.Button("Write variant collection"))
		{
			MyShaderVariantWriter settings = target as MyShaderVariantWriter;
			settings.CollectShaderVariants();
			AssetDatabase.SaveAssets();
		}
	}
}
