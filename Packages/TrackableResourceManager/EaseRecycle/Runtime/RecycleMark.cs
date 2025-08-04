using UnityEditor;
using UnityEngine;

namespace EaseRecycle
{
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	public class RecycleMark : MonoBehaviour
#if UNITY_EDITOR
		, ISerializationCallbackReceiver
#endif
	{
		/// <summary>
		/// recycle uid
		/// </summary>
		[SerializeField] private string uid;

		public string Uid
		{
			get => uid;
			set => uid = value;
		}

		internal void Init()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (this!=null && string.IsNullOrEmpty(this.uid))
				{
					if (PrefabUtility.IsAnyPrefabInstanceRoot(this.gameObject))
					{
						var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this.gameObject);
						uid = AssetDatabase.AssetPathToGUID(path);
						var thePrefab = AssetDatabase.LoadAssetAtPath<RecycleMark>(path);
						thePrefab.uid = $"{this.gameObject.name}-{uid}";
						EditorUtility.SetDirty(thePrefab);
					}
					else
					{
						uid = RecycleUils.GenUID(this.gameObject.name);
					}
				}
			}
#endif
		}

#if UNITY_EDITOR
		public void OnBeforeSerialize()
		{
			Init();
		}

		public void OnAfterDeserialize()
		{
		}
#endif
	}
}