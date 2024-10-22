using System;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AssetWeakRefer.Runtime
{
	[Serializable]
	public class AssetReference
	{
		[SerializeField] protected string guid;
		[SerializeField] protected Object asset;

		/// <summary>
		/// Construct a new AssetReference object.
		/// </summary>
		public AssetReference()
		{
		}

		/// <summary>
		/// Construct a new AssetReference object.
		/// </summary>
		/// <param name="guid">The guid of the asset.</param>
		public AssetReference(string guid)
		{
			this.guid = guid;
		}


		public string AssetGuid
		{
			get => guid;
		}

#if UNITY_EDITOR
		public Object EditorAsset => asset == null ? AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid)) : asset;

		public bool ValidateAsset(object o)
		{
			return true;
		}

		public bool SetEditorAsset(Object obj)
		{
			if (this.EditorAsset == obj)
			{
				return false;
			}

			if (obj == null)
			{
				this.asset = null;
				this.guid = null;
				return true;
			}

			var guid2 = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
			if (guid2 == "0000000000000000f000000000000000")
			{
				this.asset = obj;
				this.guid = null;
			}
			else
			{
				this.asset = null;
				this.guid = guid2;
			}

			return true;
		}

		public bool SetEditorSubObject(Object subObject)
		{
			return true;
		}
#endif
	}

	[Serializable]
	public class AssetReferenceT<TObject> : AssetReference where TObject : Object
	{
		/// <summary>
		/// Construct a new AssetReference object.
		/// </summary>
		/// <param name="guid">The guid of the asset.</param>
		public AssetReferenceT(string guid)
			: base(guid)
		{
		}
	}

	[Serializable]
	public class SpriteAssetReference : AssetReferenceT<Sprite>
	{
		public SpriteAssetReference(string guid) : base(guid)
		{
		}
	}
	
	[Serializable]
	public class GameObjectAssetReference : AssetReferenceT<GameObject>
	{
		public GameObjectAssetReference(string guid) : base(guid)
		{
		}
	}
	
	[Serializable]
	public class TextureAssetReference : AssetReferenceT<Texture>
	{
		public TextureAssetReference(string guid) : base(guid)
		{
		}
	}
	
	[Serializable]
	public class Texture2DAssetReference : AssetReferenceT<Texture2D>
	{
		public Texture2DAssetReference(string guid) : base(guid)
		{
		}
	}
	
	[Serializable]
	public class SpriteAtlasAssetReference : AssetReferenceT<SpriteAtlas>
	{
		public SpriteAtlasAssetReference(string guid) : base(guid)
		{
		}
	}
	
	[Serializable]
	public class MaterialAssetReference : AssetReferenceT<Material>
	{
		public MaterialAssetReference(string guid) : base(guid)
		{
		}
	}
	
	[Serializable]
	public class ShaderAssetReference : AssetReferenceT<Shader>
	{
		public ShaderAssetReference(string guid) : base(guid)
		{
		}
	}
	
	[Serializable]
	public class MeshAssetReference : AssetReferenceT<Mesh>
	{
		public MeshAssetReference(string guid) : base(guid)
		{
		}
	}
	
	[Serializable]
	public class AudioClipAssetReference : AssetReferenceT<AudioClip>
	{
		public AudioClipAssetReference(string guid) : base(guid)
		{
		}
	}
	//
	// [Serializable]
	// public class VideoClipAssetReference : AssetReferenceT<VideoClip>
	// {
	// 	public VideoClipAssetReference(string guid) : base(guid)
	// 	{
	// 	}
	// }
	//
	[Serializable]
	public class AnimationClipAssetReference : AssetReferenceT<AnimationClip>
	{
		public AnimationClipAssetReference(string guid) : base(guid)
		{
		}
	}
	
	[Serializable]
	public class ScriptableObjectAssetReference : AssetReferenceT<ScriptableObject>
	{
		public ScriptableObjectAssetReference(string guid) : base(guid)
		{
		}
	}
}