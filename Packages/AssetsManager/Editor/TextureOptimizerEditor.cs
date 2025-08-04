using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextureOptimizer))]
public class TextureOptimizerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if (GUILayout.Button("优化"))
		{
			TextureOptimizer settings = target as TextureOptimizer;
			settings.Optimize();
		}
		if (GUILayout.Button("裁切空白"))
		{
			TextureOptimizer settings = target as TextureOptimizer;
			settings.ClipEmpty();
		}
	}
}
