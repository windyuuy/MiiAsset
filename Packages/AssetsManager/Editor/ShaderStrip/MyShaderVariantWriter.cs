using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using static UnityEngine.ShaderVariantCollection;

namespace Assets.Scripts.Editor
{
	public class ShaderVariantInfo
	{
		public Material material;
		public string[] keywords;
		public string path;

		public bool EqualInfo(ShaderVariantInfo info)
		{
			return Enumerable.SequenceEqual(keywords, info.keywords);
		}
		public bool EqualInfo(string[] keywords)
		{
			return Enumerable.SequenceEqual(keywords, keywords);
		}
	}

	[System.Serializable]
	public class Variant
	{
		public PassType pass;
		public List<string> keywords;
	}
	[System.Serializable]
	public class SpecificShaderVariants
	{
		public SpecificShaderVariants()
		{
			if(this.shaderName != null && shader == null)
			{
				shader = Shader.Find(shaderName);
				if (shader == null)
				{
					Debug.LogErrorFormat(
						"Could not find shader '{0}'",
						shaderName);
				}
			}else if (this.shaderName == null && shader != null)
			{
				shaderName = shader.name;
			}
		}
		public Shader shader;
		public string shaderName;
		public List<Variant> variants=new List<Variant>();
	}

	[CreateAssetMenu(fileName= "MyShaderVariantWriter")]
	public class MyShaderVariantWriter:ScriptableObject
	{
		//public List<SpecificShaderVariants> additionalShaders;
		public List<Material> additionalMaterials;
		public List<SceneAsset> scenes;
		public List<GameObject> additionalPrefabs;

		public Dictionary<Shader, List<ShaderVariantInfo>> outputVariants = new Dictionary<Shader, List<ShaderVariantInfo>>();
		public ShaderVariantCollection output;

		public void CollectShaderVariants()
		{
			this.outputVariants.Clear();
			List<GameObject> seenObjects=new List<GameObject>();

			var defaultPrefabs = CollectPrefabs();
			foreach (GameObject prefab in defaultPrefabs)
			{
				AddObjectShaders(seenObjects, prefab, "additional prefabs");
			}
			foreach (GameObject prefab in additionalPrefabs)
			{
				AddObjectShaders(seenObjects, prefab, "additional prefabs");
			}
			foreach (Material material in additionalMaterials)
			{
				CollectMaterial(material, "additional materials");
			}
			var curScene = SceneManager.GetActiveScene();
			string curScenePath = curScene.path;
			foreach (var scene in scenes)
			{	
				var writeScene = EditorSceneManager.OpenScene(
					AssetDatabase.GetAssetPath(scene)
				);

				if (writeScene.IsValid())
				{
					foreach (GameObject root in writeScene.GetRootGameObjects())
					{
						AddObjectShaders(seenObjects, root, "scene object");
					}
				}
			}
			if (!string.IsNullOrEmpty(curScenePath))
			{
				EditorSceneManager.OpenScene(curScenePath, OpenSceneMode.Single);
			}
			else
			{
				EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
			}

			var SRPKeys= GenGlobalKeywordsGroups(PassType.ScriptableRenderPipeline);
			var ShadowCasterKeys = GenGlobalKeywordsGroups(PassType.ShadowCaster);

			output.Clear();
			foreach(var variant in outputVariants)
			{
				var shader = variant.Key;
				foreach(var info in variant.Value)
				{
					var data = GetShaderVariantEntriesFiltered(shader, info.keywords, info.material);
					foreach(var shaderVariant in CreateShaderVariants(shader, data.passTypes, data.keywords))
					{
						output.Add(shaderVariant);
					}

					// global variant
					AddGlobalVariant(data, info, shader, PassType.ScriptableRenderPipeline, SRPKeys);
					AddGlobalVariant(data, info, shader, PassType.Normal, SRPKeys);
					AddGlobalVariant(data, info,shader,PassType.ShadowCaster,ShadowCasterKeys);
				}
			}

			AssetDatabase.SaveAssetIfDirty(output);
			//IgnoreGlobalKeys();
		}

		public void IgnoreGlobalKeys()
		{
			var paths = AssetDatabase.FindAssets("t:ShaderStripConfig").Select(guid => AssetDatabase.GUIDToAssetPath(guid));
			var configs = paths.Select(path => AssetDatabase.LoadAssetAtPath<ShaderStripConfig>(path));
			var cur = Enumerable.Empty<ShaderVariantCollection>();
			foreach (var config in configs)
			{
				cur = cur.Concat(config.shaderVariantCollections);
			}
			var shaderVariantCollection = cur.ToArray();

			var globalKeys = Shader.globalKeywords.Select(k => k.name);
			var keywordsHead = "      - keywords: ";
			var keywordsHead2 = "          ";

			foreach (var coll in shaderVariantCollection)
			{
				var path=AssetDatabase.GetAssetPath(coll);
				var text=File.ReadAllText(path,Encoding.UTF8);
				var isInKey = false;
				var lines = new List<string>();
				var newlines=text.Split("\n").Select(line =>
				{
					if (line.StartsWith(keywordsHead))
					{
						isInKey = true;
						lines.Clear();
						var words = line.Substring(keywordsHead.Length).Split(" ");
						var words2 = words.Where(word => !globalKeys.Contains(word));
						var line2= keywordsHead + String.Join(' ', words2);
						lines.Add(line2);
						return "";
					}
					else if(isInKey && line.StartsWith(keywordsHead2) && (!line.StartsWith(keywordsHead2+" ")))
					{
						var words = line.Substring(keywordsHead2.Length).Split(" ");
						var words2 = words.Where(word => !globalKeys.Contains(word));
						var line3= String.Join(' ', words2);
						lines.Add(line3);
						return "";
					}
					else
					{
						if (isInKey)
						{
							isInKey = false;
							return string.Join(' ', lines)+"\n"+line;
						}
						else
						{
							return line;
						}
					}
				}).Where(k=>!string.IsNullOrEmpty(k));
				var newText= String.Join('\n',newlines);
				var bkPath = Path.ChangeExtension(path, "bk.shadervariants");
				//File.WriteAllText(bkPath, text, Encoding.UTF8);
				File.WriteAllText(path, newText, Encoding.UTF8);
			}
			AssetDatabase.Refresh();
		}
		public void AddGlobalVariant(ShaderVariantEntriesData data0,ShaderVariantInfo info, Shader shader, PassType passType,string[][] ShadowCasterKeys){
			if (data0.passTypes.Contains(passType))
			{
				foreach(var globalKeys in ShadowCasterKeys){
					var keywordSpace = shader.keywordSpace;
					if(globalKeys.All(key =>
					{
						return keywordSpace.FindKeyword(key) != null;
					})){
						var supportKeys=data0.keywords.Concat(globalKeys).ToArray();
						var data2 = GetShaderVariantEntriesFiltered(shader, supportKeys, info.material);
						foreach (var shaderVariant in CreateShaderVariants(shader, new PassType[] { passType }, data2.keywords))
						{
							output.Add(shaderVariant);
						}
					}
				}
			}
		}

		public string[][] GenGlobalKeywordsGroups(PassType passType)
		{
			var globalKeys=Shader.enabledGlobalKeywords.Select(k=>k.name).ToArray();

			string _MAIN_LIGHT_SHADOWS="";
			if(globalKeys.Contains("_MAIN_LIGHT_SHADOWS")){
				_MAIN_LIGHT_SHADOWS="_MAIN_LIGHT_SHADOWS";
			}else if(globalKeys.Contains("_MAIN_LIGHT_SHADOWS_CASCADE")){
				_MAIN_LIGHT_SHADOWS="_MAIN_LIGHT_SHADOWS_CASCADE";
			}

			string _ADDITIONAL_LIGHT_SHADOWS="";
			if(globalKeys.Contains("_ADDITIONAL_LIGHT_SHADOWS")){
				_ADDITIONAL_LIGHT_SHADOWS="_ADDITIONAL_LIGHT_SHADOWS";
			}else if(globalKeys.Contains("_ADDITIONAL_LIGHT_SHADOWS_CASCADE")){
				_ADDITIONAL_LIGHT_SHADOWS="_ADDITIONAL_LIGHT_SHADOWS_CASCADE";
			}

			string FOG_LINEAR="";
			if(globalKeys.Contains("FOG_LINEAR")){
				FOG_LINEAR="FOG_LINEAR";
			}else if(globalKeys.Contains("FOG_EXP2")){
				FOG_LINEAR="FOG_EXP2";
			}

			var spo=StringSplitOptions.RemoveEmptyEntries;

			if(passType==PassType.ScriptableRenderPipeline || passType==PassType.Normal)
			{
				var ss = new string[][]
				{
					$"{FOG_LINEAR} LOD_FADE_CROSSFADE _ADDITIONAL_LIGHTS {_ADDITIONAL_LIGHT_SHADOWS}".Split(" ",spo),
					$"{FOG_LINEAR} LOD_FADE_CROSSFADE _ADDITIONAL_LIGHTS {_ADDITIONAL_LIGHT_SHADOWS} {_MAIN_LIGHT_SHADOWS}".Split(" ",spo),
					$"{FOG_LINEAR} _ADDITIONAL_LIGHTS {_ADDITIONAL_LIGHT_SHADOWS}".Split(" ",spo),
					$"{FOG_LINEAR} _ADDITIONAL_LIGHTS {_ADDITIONAL_LIGHT_SHADOWS} {_MAIN_LIGHT_SHADOWS}".Split(" ",spo),
					$"{FOG_LINEAR} _ADDITIONAL_LIGHTS {_ADDITIONAL_LIGHT_SHADOWS} {_MAIN_LIGHT_SHADOWS} _REFLECTION_PROBE_BLENDING _REFLECTION_PROBE_BOX_PROJECTION".Split(" ",spo),
				};
				return ss;
			}else if(passType==PassType.ShadowCaster){
				var ss = new string[][]
				{
					$"INSTANCING_ON".Split(" ",spo),
					$"{FOG_LINEAR}".Split(" ",spo),
					$"{FOG_LINEAR} LOD_FADE_CROSSFADE _ADDITIONAL_LIGHTS".Split(" ",spo),
					$"{FOG_LINEAR} _ADDITIONAL_LIGHTS".Split(" ",spo),
					$"_ADDITIONAL_LIGHTS {_ADDITIONAL_LIGHT_SHADOWS} _MAIN_LIGHT_SHADOWS".Split(" ",spo),
				};
				return ss;
			}
			return null;
		}
		public IEnumerable<GameObject> CollectPrefabs()
		{
			var prefabs = AssetDatabase.FindAssets("t:Prefab")
				.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
				.Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path));
			return prefabs; 
		}
		public struct ShaderVariantEntriesData
		{
			public PassType[] passTypes;
			public string[] keywords;
			public string[] remainingKeywords;
		}
		public static (bool,PassType) GetPassTypeByLightMode(string lightMode)
		{
			var isValidPassType = false;
			PassType passType;
			if (lightMode.Equals("", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("Normal", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.Normal;
			}
			else if (lightMode.Equals("UniversalForward", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("UniversalGBuffer", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("UniversalForwardOnly", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("Universal2D", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("ShadowCaster", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ShadowCaster;
			}
			else if (lightMode.Equals("DepthOnly", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("DepthNormals", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("DepthNormalsOnly", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("FORWARDBASE", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("DEFERRED", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.Deferred;
			}
			else if (lightMode.Equals("SRPDefaultUnlit", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ScriptableRenderPipelineDefaultUnlit;
			}
			else if (lightMode.Equals("Meta", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = false;
				passType = PassType.Meta;
			}
			else if (lightMode.Equals("DecalGBufferMesh", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = false;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("DecalGBufferProjector", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = false;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("DBufferProjector", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = false;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("DecalScreenSpaceMesh", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = false;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("DBufferMesh", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = false;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("SceneSelectionPass", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = false;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("DecalScreenSpaceProjector", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = false;
				passType = PassType.ScriptableRenderPipeline;
			}
			else if (lightMode.Equals("Picking", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = false;
				passType = PassType.ForwardAdd;
			}
			#region Builtin-LightMode
			else if (lightMode.Equals("Always", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.Normal;
			}
			else if (lightMode.Equals("ForwardAdd", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.ForwardAdd;
			}
			else if (lightMode.Equals("PrepassBase", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.LightPrePassBase;
			}
			else if (lightMode.Equals("PrepassFinal", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.LightPrePassFinal;
			}
			else if (lightMode.Equals("Vertex", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.Vertex;
			}
			else if (lightMode.Equals("VertexLMRGBM", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.VertexLMRGBM;
			}
			else if (lightMode.Equals("VertexLM", StringComparison.OrdinalIgnoreCase))
			{
				isValidPassType = true;
				passType = PassType.VertexLM;
			}
#endregion Builtin-LightMode
			else
			{
				Debug.LogError($"unsupport lightmode: {lightMode}");
				isValidPassType = false;
				passType = PassType.ScriptableRenderPipeline;
			}

			return (isValidPassType,passType);
		}
		public static IEnumerable<ShaderVariant> CreateShaderVariants(Shader shader, IEnumerable<PassType> passTypes, string[] keywords)
		{
			var vs = passTypes.Select(passType =>
			{
				ShaderVariant shaderVariant;
				try
				{
					shaderVariant = new ShaderVariant(shader, (PassType)passType, keywords);
				}
				catch (ArgumentException e)
				{
					shaderVariant = new ShaderVariant()
					{
						shader = shader,
						passType = (PassType)passType,
						keywords = keywords,
					};
				}
				return shaderVariant;
			});
			return vs;
		}
		public static ShaderVariantEntriesData GetShaderVariantEntriesFiltered(Shader sd, string[] SelectedKeywords, Material material)
		{
			var passTypes=new List<PassType>();
			for (var passId = 0; passId < material.passCount; passId++)
			{
				var name = material.GetPassName(passId);
				if (material.GetShaderPassEnabled(name))
				{
					var lightMode = sd.FindPassTagValue(passId, new ShaderTagId("LightMode"));
					if (lightMode != null)
					{
						var (exist,passType)=GetPassTypeByLightMode(lightMode.name);
						if(exist){
							passTypes.Add(passType);
						}
					}
				}
			}

			var keywords = SelectedKeywords
				.Where(key => !Shader.globalKeywords.Any(k => k.name == key))
				.ToArray();

			ShaderVariantEntriesData svd = new ShaderVariantEntriesData();
			svd.passTypes = passTypes.Distinct().ToArray();
			svd.keywords = keywords;
			return svd;
		}
		// public static ShaderVariantEntriesData GetShaderVariantEntriesFiltered(Shader sd, string[] SelectedKeywords, Material material)
		// {
		// 	string[] keywordLists = null, remainingKeywords = null;
		// 	int[] FilteredVariantTypes = null;
		// 	var svc = new ShaderVariantCollection();
		// 	MethodInfo GetShaderVariantEntries = typeof(ShaderUtil).GetMethod("GetShaderVariantEntriesFiltered", BindingFlags.NonPublic | BindingFlags.Static); ;
		// 	object[] args = new object[] {
		// 		sd,
		// 		int.MaxValue/2,
		// 		SelectedKeywords,
		// 		svc,
		// 		 FilteredVariantTypes,
		// 		 keywordLists,
		// 		 remainingKeywords};
		// 	GetShaderVariantEntries.Invoke(obj: null, args);
		// 	var filterWords = SelectedKeywords.Where(k => remainingKeywords.Contains(k)).ToArray();
		// 	var groups = keywordLists.Distinct().Where(k =>
		// 	{
		// 		var arr = k.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
		// 		if (arr.Count != filterWords.Length)
		// 		{
		// 			return false;
		// 		}
		// 		arr.Sort();
		// 		var b = Enumerable.SequenceEqual(filterWords, arr);
		// 		return b;
		// 	}).ToArray();
		// 	var passTypes = new List<int>();
		// 	for (var i = 0; i < keywordLists.Length; i++)
		// 	{
		// 		var k = keywordLists[i];
		// 		if (groups.Contains(k))
		// 		{
		// 			passTypes.Add(FilteredVariantTypes[i]);
		// 		}
		// 	}
		// 	var passTypes2 = passTypes.Distinct().ToArray();

		// 	ShaderVariantEntriesData svd = new ShaderVariantEntriesData();
		// 	svd.passTypes = passTypes2;
		// 	svd.keywordLists = SelectedKeywords;
		// 	svd.remainingKeywords = args[6] as string[];

		// 	return svd;
		// }

		public void CollectMaterial(Material material, string path)
		{
			var ks = material.shaderKeywords.ToList();
			ks.Sort();
			var keywords = ks.ToArray();
			List<ShaderVariantInfo> exists;
			if (outputVariants.TryGetValue(material.shader,out exists))
			{
				if (!exists.Any(item => item.EqualInfo(keywords)))
				{
					exists.Add(new ShaderVariantInfo()
					{
						keywords= keywords,
						path=path,
						material=material,
					});
				}
			}
			else
			{
				exists = new List<ShaderVariantInfo>();
				outputVariants.Add(material.shader, exists);
				exists.Add(new ShaderVariantInfo()
				{
					keywords = keywords,
					path = path,
					material=material,
				});
			}
		}

		string ObjectPath(GameObject gameObject)
		{
			StringBuilder path = new StringBuilder();
			path.Append(gameObject.name);
			PrependParent(gameObject.transform, path);
			return path.ToString();
		}

		void PrependParent(Transform transform, StringBuilder path)
		{
			if (transform.parent != null)
			{
				path.Insert(
					0,
					string.Format(
						"{0}/",
						transform.parent.name,
						path));

				PrependParent(transform.parent, path);
			}
		}

		void AddObjectShaders(List<GameObject> seenObjects, GameObject gameObject, string source)
		{
			if (seenObjects.Contains(gameObject)) return;
			seenObjects.Add(gameObject);
			var components = gameObject.GetComponentsInChildren<Component>(true);
			foreach (var component in components)
			{
				if (component == null) continue;

				if (component is Renderer renderer)
				{
					Material[] materials = renderer.sharedMaterials;
					foreach (Material material in materials)
					{
						if (material != null)
						{
							CollectMaterial(
								material,
								string.Format(
									"{0} using material '{1}'",
									string.Format(
										"{0} '{1}'",
										source,
										ObjectPath(renderer.gameObject)),
									material.name));
						}
					}
				}
				else if (component is Graphic image)
				{
					CollectMaterial(
						image.material,
						string.Format(
							"{0} using material '{1}'",
							string.Format(
								"{0} '{1}'",
								source,
								ObjectPath(image.gameObject)),
							image.material.name));
				}
				else if (!component.GetType().IsSubclassOf(typeof(Transform)) &&
					component.GetType() != typeof(Transform) &&
					component.GetType() != typeof(MeshFilter) &&
					!component.GetType().IsSubclassOf(typeof(Collider)) &&
					component.GetType() != typeof(NavMeshObstacle))
				{
					AddNonSceneObjects(
						seenObjects,
						component,
						string.Format(
							"{0} '{1}' component '{2}'",
							source,
							ObjectPath(component.gameObject),
							component.GetType().ToString()));
				}
			}
		}

		void AddNonSceneObjects(
			List<GameObject> seenObjects,
			Component sourceComponent,
			string source)
		{
			var transform = sourceComponent.transform;
			var serializedObject = new SerializedObject(sourceComponent);
			var property = serializedObject.GetIterator();
			do
			{
				if (property.name != "m_GameObject" &&
					property.propertyType ==
						SerializedPropertyType.ObjectReference &&
					property.objectReferenceValue != null)
				{
					Type type = property.objectReferenceValue.GetType();

					int reference =
						property.objectReferenceValue.GetInstanceID();

					GameObject referencedObject = null;
					if (type == typeof(Material))
					{
						var material = (Material)property.objectReferenceValue;
						CollectMaterial(
							material,
							string.Format(
								"{0}.{1} material '{2}'",
								source,
								property.propertyPath,
								material.name));
					}
					if (type == typeof(GameObject))
					{
						referencedObject =
							(GameObject)property.objectReferenceValue;
					}
					else if (type.IsSubclassOf(typeof(Component)))
					{
						var component = (Component)property.objectReferenceValue;
						reference = component.gameObject.GetInstanceID();
						referencedObject = component.gameObject;
					}
					if (referencedObject != null &&
						!referencedObject.scene.IsValid() &&
						!transform.IsChildOf(referencedObject.transform))
					{
						AddObjectShaders(
							seenObjects,
							referencedObject,
							string.Format("{0} referencing",
								source));
					}
				}
			}
			while (property.Next(true));
		}
	}
}
