using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace MyAddressablesTools
{
    public static class AtlasTools
    {
        const int k_SpriteAssetMenuPriority = 1;
        const int k_SpriteAtlasAssetMenuPriority = k_SpriteAssetMenuPriority + 11;

        const int k_SpriteGameObjectMenuPriority = 1;
        const int k_PhysicsGameObjectMenuPriority = 2;
        const int k_SpriteMaskGameObjectMenuPriority = 6;

        internal class DoCreateSpriteAtlas : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {

			public Object[] validObjects;

			public int sides;
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var spriteAtlasAsset = new SpriteAtlasAsset();
				spriteAtlasAsset.Add(validObjects);
                spriteAtlasAsset.SetPlatformSettings(new TextureImporterPlatformSettings(){
                    crunchedCompression=true,
                    compressionQuality=35,
                    textureCompression=TextureImporterCompression.CompressedLQ,
                });
                spriteAtlasAsset.SetPackingSettings(new SpriteAtlasPackingSettings(){
                    padding=4,
                    enableRotation=false,
                    enableAlphaDilation=true,
                });

                UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { spriteAtlasAsset }, pathName, true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        static private void CreateSpriteAtlas(Object[] validObjects, string atlasName)
        {
			//var m=typeof(EditorGUIUtility).GetMethod("IconContent",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
			var ms = typeof(EditorGUIUtility).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var m =ms.First(m => m.Name == "IconContent");
			var m2=m.MakeGenericMethod(new System.Type[]{typeof(SpriteAtlasAsset)});
			var obj=m2.Invoke(null,new object[]{null});
            var icon = (obj as GUIContent).image as Texture2D;
            DoCreateSpriteAtlas action = ScriptableObject.CreateInstance<DoCreateSpriteAtlas>();
			action.validObjects= validObjects;
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, action, $"{atlasName}_Atlas.spriteatlasv2", icon, null);
        }
//		public override bool ValidateAsset(string path)
//		{
//#if UNITY_EDITOR
//			if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(SpriteAtlas))
//				return true;

//			var type = AssetDatabase.GetMainAssetTypeAtPath(path);
//			bool isTexture = typeof(Texture2D).IsAssignableFrom(type);
//			if (isTexture)
//			{
//				var importer = AssetImporter.GetAtPath(path) as TextureImporter;
//				return (importer != null) && (importer.spriteImportMode != SpriteImportMode.None);
//			}
//#endif
//			return false;
//		}

		// [MenuItem("Assets/Create/2D/Sprite Atlas", priority = k_SpriteAtlasAssetMenuPriority)]
		static void AssetsCreateSpriteAtlas(int level)
        {
            var assetPaths=Selection.assetGUIDs.Select(guid =>
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                return path;
            }).ToArray();
            var mainPath = assetPaths[0];
            var mainPP = Path.GetDirectoryName(mainPath);
            var mainPP2 = Path.GetDirectoryName(mainPP);
            var folderName = Path.GetRelativePath(mainPP2, mainPP);
			if (folderName.Equals("res", System.StringComparison.CurrentCultureIgnoreCase))
            {
                var mainPP3 = Path.GetDirectoryName(mainPP2);
                folderName= Path.GetRelativePath(mainPP3, mainPP2);
			}

			var paths=AssetDatabase.FindAssets("", assetPaths).Select(guid =>
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				return path;
			}).ToList();
			paths.Sort((p1,p2)=>{
				return Path.GetFileName(p1).CompareTo(Path.GetFileName(p2));
			});
			var validObjects = paths.Where(path =>
            {
                var mainAssetType=AssetDatabase.GetMainAssetTypeAtPath(path);
    			bool isTexture = typeof(Texture2D).IsAssignableFrom(mainAssetType);
				bool isTooLarge=false;
                if (!isTexture)
                {
                    Debug.LogError("may not valid texture adding to atlas");
                }else{
					var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if(level==1){
					    isTooLarge=(texture.width>2048)||(texture.height>2048)||((texture.width*texture.height)>(1024*1024));
                    }else if(level==2){
					    isTooLarge=(texture.width>300)||(texture.height>300)||((texture.width*texture.height)>(300*256));
                    }else{
                        throw new System.NotImplementedException($"unkown level: {level}");
                    }
				}
                return isTooLarge==false && mainAssetType!=null;
			}).Select(path=>AssetDatabase.LoadAssetAtPath<Object>(path)).ToArray();
            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2){
                CreateSpriteAtlas(validObjects, folderName);
			}
            else{
				throw new System.NotImplementedException("support atlas v2 only");
			}
        }

        [MenuItem("Assets/CreateV2/SpriteAtlas", priority	 = 40)]
        public static void FindSingleAssetReferencesMenu()
        {
            AssetsCreateSpriteAtlas(1);
        }
        [MenuItem("Assets/CreateV2/SpriteAtlas2", priority	 = 41)]
        public static void FindSingleAssetReferencesMenu2()
        {
            AssetsCreateSpriteAtlas(2);
        }
    }
}
