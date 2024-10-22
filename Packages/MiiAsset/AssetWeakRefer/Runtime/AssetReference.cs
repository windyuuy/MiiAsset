using System;
using System.Threading.Tasks;
using MiiAsset.Runtime;
using MiiAsset.Runtime.Status;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiiAsset.AssetWeakRefer.Runtime
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

		// ReSharper disable once InconsistentNaming
		public string AssetGUID => guid;

		public string Address => guid != null ? AssetLoader.GetAddressFromGuid(guid) : null;
		public object RuntimeKey => Address ?? guid;

		public Object RawAsset => asset;

		public Task<T> Load<T>(AssetLoadStatusGroup loadStatus = null)
		{
			if (Address == null)
			{
				if (RawAsset is T assetT)
				{
					return Task.FromResult<T>(assetT);
				}

				return Task.FromResult<T>(default);
			}
			else
			{
				return AssetLoader.LoadAsset<T>(Address, loadStatus);
			}
		}

		public Task UnLoad()
		{
			return AssetLoader.UnLoadAsset(Address);
		}

		public bool RuntimeKeyIsValid()
		{
			return Address != null || RawAsset != null;
		}

		public bool IsValid()
		{
			return RawAsset != null || AssetLoader.ExistAddress(Address);
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
		public Task<Scene> LoadScene(LoadSceneMode mode, AssetLoadStatusGroup loadStatus = null)
		{
			return AssetLoader.LoadScene(Address, new LoadSceneParameters()
			{
				loadSceneMode = mode,
			}, loadStatus);
		}

		public Task UnLoadScene(UnloadSceneOptions options = UnloadSceneOptions.None)
		{
			return AssetLoader.UnLoadScene(Address, options);
		}
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

		public TObject Asset => asset as TObject;

		public Task<TObject> Load(AssetLoadStatusGroup loadStatus = null)
		{
			return AssetLoader.LoadAsset<TObject>(Address, loadStatus);
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