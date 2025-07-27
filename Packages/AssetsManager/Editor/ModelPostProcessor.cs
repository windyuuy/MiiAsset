using UnityEngine;
using UnityEditor;
using System;

// Adds a mesh collider to each game object that contains collider in its name
public class ModelPostProcessor : AssetPostprocessor
{
	void OnPostprocessModel(GameObject g)
	{
		ModelImporter importer = assetImporter as ModelImporter;
		try
		{
			if (importer.assetPath.StartsWith("Assets/"))
			{
				if (importer.clipAnimations.Length == 0)
				{
					importer.importAnimation = false;
				}

				if (importer.assetPath.StartsWith("Assets/Bundles/Resource/Map/"))
				{
					//importer.meshCompression = ModelImporterMeshCompression.Medium;
					importer.meshCompression = ModelImporterMeshCompression.Off;
				}
				else
				{
					importer.meshCompression = ModelImporterMeshCompression.Low;
					//importer.meshCompression = ModelImporterMeshCompression.Medium;
				}

				if (importer.animationType == ModelImporterAnimationType.Generic || importer.animationType == ModelImporterAnimationType.Human)
				{
					importer.animationCompression = ModelImporterAnimationCompression.Optimal;
				}
				else
				{
					importer.animationCompression = ModelImporterAnimationCompression.KeyframeReductionAndCompression;
				}

				importer.optimizeMeshPolygons = true;
				importer.optimizeMeshVertices = true;
				importer.importLights = false;
				importer.importCameras = false;
				importer.isReadable = false;
				importer.importBlendShapes = false;
				importer.importVisibility = false;
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"Optimize Model Failed: {assetPath}");
			Debug.LogException(e);
		}
	}
}
