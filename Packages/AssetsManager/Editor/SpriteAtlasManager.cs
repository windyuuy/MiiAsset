using System.Collections.Generic;
using System.IO;
using UnityEditor;
using YamlDotNet.Serialization;

public class SpriteAtlasManager
{
    public class SpriteAtlasInfo
    {
        public TSpriteAtlasAsset SpriteAtlasAsset;
        public class  TSpriteAtlasAsset
        {
            public ImporterData m_ImporterData;
            public class ImporterData
            {
                public FileInfo[] packables;
                public class FileInfo
                {
                    public string guid;
                }
            }
        }
    }
	
    protected static Dictionary<string,bool> packedTextures=new Dictionary<string, bool>();

    public static bool IsIncludeInAtlas(string path)
    {
        return packedTextures.ContainsKey(path);
    }
    static bool Inited=false;
    public static void LoadAll(){
        if(Inited){
            return;
        }
        Inited=true;
		
        var atlases=AssetDatabase.FindAssets("t:spriteatlas",new string[]
        {
            "Assets/Bundles",
        });
        foreach(var atlasGuid in atlases)
        {
            var atlasPath = AssetDatabase.GUIDToAssetPath(atlasGuid);
            StreamReader yamlReader = File.OpenText(atlasPath);
            var yamlDeserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().WithTagMapping("tag:unity3d.com,2011:612988286",typeof(SpriteAtlasInfo)).Build();
            var atlasInfo=yamlDeserializer.Deserialize<SpriteAtlasInfo>(yamlReader);
            foreach (var fileInfo in atlasInfo.SpriteAtlasAsset.m_ImporterData.packables)
            {
                var filePath = AssetDatabase.GUIDToAssetPath(fileInfo.guid);
                packedTextures[filePath] = true;
            }
        }
    }
}