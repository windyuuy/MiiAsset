
using System;

namespace UnityEngine.UnityExtension
{
	public static class UnityExt
	{
		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
		{
			var comp = gameObject.GetComponent<T>();
			if (comp == null)
			{
				comp = gameObject.AddComponent<T>();
			}

			return comp;
		}

		public static T GetOrAddComponent<T>(this Component comp0) where T : Component
		{
			var comp = comp0.GetComponent<T>();
			if (comp == null)
			{
				comp = comp0.gameObject.AddComponent<T>();
			}

			return comp;
		}

		public static float GetClipDuration(this Animator animator, string clipName)
		{
			if (null == animator ||
			    string.IsNullOrEmpty(clipName) ||
			    null == animator.runtimeAnimatorController)
				return 0;
			// 获取所有的clips	
			var ac = animator.runtimeAnimatorController;
			var clips = ac.animationClips;
			if (null == clips || clips.Length <= 0) return 0;
			AnimationClip clip;
			for (int i = 0, len = clips.Length; i < len; ++i)
			{
				clip = ac.animationClips[i];
				if (null != clip && clip.name == clipName)
					return clip.length;
			}

			return 0f;
		}

		public static void ChangeLayerRecursively(this Transform trans, string targetLayer, bool includeChildren = true)
		{
			if (LayerMask.NameToLayer(targetLayer) == -1)
			{
				Debug.LogError("Layer中不存在,请手动添加LayerName");
				return;
			}

			//遍历更改所有子物体layer
			trans.gameObject.layer = LayerMask.NameToLayer(targetLayer);
			if (!includeChildren)
				return;
			foreach (Transform child in trans)
			{
				child.ChangeLayerRecursively(targetLayer, includeChildren);
			}
		}

		public static void ChangeLayerRecursively(this Transform trans, int targetLayer, bool includeChildren = true)
		{
			if (targetLayer == -1)
			{
				Debug.LogError("Layer中不存在,请手动添加LayerName");
				return;
			}

			//遍历更改所有子物体layer
			trans.gameObject.layer = targetLayer;
			if (!includeChildren)
				return;
			foreach (Transform child in trans)
			{
				child.ChangeLayerRecursively(targetLayer, includeChildren);
			}
		}

		public static void AddCullingMask(this Camera cam, string targetLayer)
		{
			cam.cullingMask |= (1 << LayerMask.NameToLayer(targetLayer));
		}

		public static void RemoveCullingMask(this Camera cam, string targetLayer)
		{
			cam.cullingMask &= ~(1 << LayerMask.NameToLayer(targetLayer));
		}

		public static void AdaptRectSize(this RectTransform self, Vector2 targetSize)
		{
			if (targetSize.x == 0 || targetSize.y == 0)
			{
				self.sizeDelta = Vector2.zero;
				return;
			}

			var curSize = self.sizeDelta;
			var widthRate = curSize.x / targetSize.x;
			var heightRate = curSize.y / targetSize.y;

			if (widthRate > heightRate)
				self.sizeDelta /= widthRate;
			else
				self.sizeDelta /= heightRate;

		}

		public static Transform Seek(this Transform self, params string[] names)
		{
			var target = self;
			if (target == null)
			{
#if UNITY_EDITOR||DEVELOPMENT_BUILD
				throw new Exception("无效的对象");
#else
			Debug.LogError("无效的对象");
#endif
				return null;
			}

			foreach (var name in names)
			{
				target = target.Find(name);
				if (target == null)
				{
#if UNITY_EDITOR||DEVELOPMENT_BUILD
					throw new Exception("无效的对象");
#else
			Debug.LogError("无效的对象");
#endif
					return null;
				}
			}

			return target;
		}
	}
}
