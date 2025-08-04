using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class TextureExtEditor : Editor
{
	static TextureExtEditor()
	{
		Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
	}

	static void OnPostHeaderGUI(Editor editor)
	{
		var importers = editor.targets.Where(target => target is TextureImporter).Select(target => target as TextureImporter).ToArray();
		if (importers.Length > 0)
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("优化"))
			{
				foreach (var importer in importers)
				{
					TextureOptimizer.Optimize(importer.assetPath);
				}
			}
			if (GUILayout.Button("裁切空白"))
			{
				foreach (var importer in importers)
				{
					TextureOptimizer.ClipEmpty(importer.assetPath);
				}
			}
			if (GUILayout.Button("优化九宫"))
			{
				foreach (var importer in importers)
				{
					TextureOptimizer.OptimizeSprite9(importer.assetPath);
				}
			}
			GUILayout.EndHorizontal();
		}
	}
}
