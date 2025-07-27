// using System;
// using System.Collections;
// using TestSharedResIds;
// using TrackableResourceManager.Runtime;
// using UISys.Runtime;
// using UnityEngine;
// using MiiAsset.AssetWeakRefer.Runtime;

// namespace TrackableResourceManager.Test
// {
// 	public class TestResourceManager : MonoBehaviour
// 	{
// 		[SerializeField] protected AssetReferenceT<UILayerRootRefer> layerRootRefer1;
// 		[SerializeField] protected AssetReferenceT<UILayerRootRefer> layerRootRefer2;
// 		private async void Start()
// 		{
// 			// var wfe=await layerRootRefer1.LoadAssetAsync().Task;
// 			// var wfe2=await layerRootRefer2.LoadAssetAsync().Task;
// 			Debug.Log("TestResourceManager");
// 			// {
// 			// 	using var rr = ResourceScope.New;
// 			// }

// 			StartCoroutine(DelayTest());
// 		}

// 		IEnumerator DelayTest()
// 		{
// 			yield return new WaitForEndOfFrame();
// 			{
// 				using var rr = ResourceScope.New;
// 				var loadAsyncOp = rr.LoadAsync<GameObject>(R.Ffffffffffffffffffffffffffffffff_HelloGroup_EwFge3r);
// 				yield return loadAsyncOp;
// 				var ret = loadAsyncOp.Result;
// 			}
// 		}
// 	}
// }
