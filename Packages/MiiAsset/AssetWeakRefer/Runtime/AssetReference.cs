using System;
using UnityEngine;
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
}