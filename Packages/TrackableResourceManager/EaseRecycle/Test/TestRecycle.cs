using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using EaseRecycle;
using NUnit.Framework;
using UnityEngine;
using MiiAsset.AssetWeakRefer.Runtime;
using MiiAsset.Runtime;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EaseRecycle.Tests
{
	public class TestRecycle
	{
		public string assetmanifestkey = "Assets/EaseRecycle/Test/TestRecycle.prefab";

#if !DISABLE_NOREFERCOUNT_API
		[UnityTest]
		public async Task TestRecycle1()
		{
			// Use the Assert class to test conditions.
			// Use yield to skip a frame.
			var op = AssetLoader.LoadAsset<GameObject>(assetmanifestkey);
			await op;
			var testAssets = op.Result.GetComponent<TestAssets>();

			var op2 = testAssets.RecycleRecursive.Load<GameObject>();
			await op2;
			var asset = op2.Result;
			var recyclePool = new RecyclePool();

			recyclePool.LoadPrefab(asset);

			var image = recyclePool.Get("Image-bbeb2f9a174ba2e4dae9d493cc9e236e");
			Debug.Assert(image != null);
			var image2 = recyclePool.Get("Image-bbeb2f9a174ba2e4dae9d493cc9e236e");
			Debug.Assert(image.GetComponent<RecycleMark>().Uid == image2.GetComponent<RecycleMark>().Uid);
			var image3 = recyclePool.Get("Image-bbeb2f9a174ba2e4dae9d493cc9e236e");
			Debug.Assert(image.GetComponent<RecycleMark>().Uid == image3.GetComponent<RecycleMark>().Uid);

			_ = AssetLoader.UnLoadAsset<GameObject>(assetmanifestkey);
			_ = testAssets.UnrecycleSingle.UnLoad<GameObject>();
		}

		[UnityTest]
		public async Task TestRecycle1_2()
		{
			var op = AssetLoader.LoadAsset<GameObject>(assetmanifestkey);
			await op;
			var testAssets = op.Result.GetComponent<TestAssets>();

			var op2 = testAssets.RecycleRecursive2.Load<GameObject>();
			await op2;
			var asset = op2.Result;
			var recyclePool = new RecyclePool();

			recyclePool.LoadPrefab(asset);

			var image = recyclePool.Get("Image-bbeb2f9a174ba2e4dae9d493cc9e236e");
			Debug.Assert(image != null);
			var image2 = recyclePool.Get("Image-bbeb2f9a174ba2e4dae9d493cc9e236e");
			Debug.Assert(image.GetComponent<RecycleMark>().Uid == image2.GetComponent<RecycleMark>().Uid);
			var image3 = recyclePool.Get("Image-bbeb2f9a174ba2e4dae9d493cc9e236e");
			Debug.Assert(image.GetComponent<RecycleMark>().Uid == image3.GetComponent<RecycleMark>().Uid);

			_ = AssetLoader.UnLoadAsset<GameObject>(assetmanifestkey);
			_ = testAssets.UnrecycleSingle.UnLoad<GameObject>();
		}

		[UnityTest]
		public async Task TestRecycle1_3()
		{
			var op = AssetLoader.LoadAsset<GameObject>(assetmanifestkey);
			await op;
			var testAssets = op.Result.GetComponent<TestAssets>();

			var op2 = testAssets.Single1.Load<GameObject>();
			await op2;
			var asset = op2.Result;
			var recyclePool = new RecyclePool();

			recyclePool.LoadPrefab(asset);

			var image = recyclePool.Get("Image-bbeb2f9a174ba2e4dae9d493cc9e236e");
			Debug.Assert(image != null);
			var image2 = recyclePool.Get("Image-bbeb2f9a174ba2e4dae9d493cc9e236e");
			Debug.Assert(image.GetComponent<RecycleMark>().Uid == image2.GetComponent<RecycleMark>().Uid);
			var image3 = recyclePool.Get("Image-bbeb2f9a174ba2e4dae9d493cc9e236e");
			Debug.Assert(image.GetComponent<RecycleMark>().Uid == image3.GetComponent<RecycleMark>().Uid);

			_ = AssetLoader.UnLoadAsset<GameObject>(assetmanifestkey);
			_ = testAssets.UnrecycleSingle.UnLoad<GameObject>();
		}

		[UnityTest]
		public async Task TestRecycle1_4()
		{
			var op = AssetLoader.LoadAsset<GameObject>(assetmanifestkey);
			await op;
			var testAssets = op.Result.GetComponent<TestAssets>();

			var op2 = testAssets.Single1.Load<GameObject>();
			await op2;
			var asset = op2.Result;
			var recyclePool = new RecyclePool();

			recyclePool.LoadPrefab(asset, "uid1_4");

			var image = recyclePool.Get("uid1_4");
			Debug.Assert(image != null);
			var image2 = recyclePool.Get("uid1_4");
			Debug.Assert(image.GetComponent<RecycleMark>().Uid == image2.GetComponent<RecycleMark>().Uid);
			var image3 = recyclePool.Get("uid1_4");
			Debug.Assert(image.GetComponent<RecycleMark>().Uid == image3.GetComponent<RecycleMark>().Uid);

			_ = AssetLoader.UnLoadAsset<GameObject>(assetmanifestkey);
			_ = testAssets.UnrecycleSingle.UnLoad<GameObject>();
		}

		[UnityTest]
		public async Task TestRecycle2()
		{
			var op = AssetLoader.LoadAsset<GameObject>(assetmanifestkey);
			await op;
			var testAssets = op.Result.GetComponent<TestAssets>();

			var op2 = testAssets.RecycleSingle.Load<GameObject>();
			await op2;
			var asset = op2.Result;
			var recyclePool = new RecyclePool();

			recyclePool.LoadPrefab(asset);

			var image = recyclePool.Get("RecycleSingle-1a1365e071cc9e3468bfeae4849f9ed7");
			Debug.Assert(image != null);
			var image2 = recyclePool.Get("RecycleSingle-1a1365e071cc9e3468bfeae4849f9ed7");
			Debug.Assert(image.GetComponent<RecycleMark>().Uid == image2.GetComponent<RecycleMark>().Uid);
			var image3 = recyclePool.Get("RecycleSingle-1a1365e071cc9e3468bfeae4849f9ed7");
			Debug.Assert(image.GetComponent<RecycleMark>().Uid == image3.GetComponent<RecycleMark>().Uid);

			_ = AssetLoader.UnLoadAsset<GameObject>(assetmanifestkey);
			_ = testAssets.UnrecycleSingle.UnLoad<GameObject>();
		}

		[UnityTest]
		public async Task TestRecycle3()
		{
			var op = AssetLoader.LoadAsset<GameObject>(assetmanifestkey);
			await op;
			var testAssets = op.Result.GetComponent<TestAssets>();

			var op2 = testAssets.UnrecycleSingle.Load<GameObject>();
			await op2;
			var asset = op2.Result;
			var recyclePool = new RecyclePool();

			recyclePool.LoadPrefab(asset);
			Debug.Assert(recyclePool.NodePools.Count == 0);

			_ = AssetLoader.UnLoadAsset<GameObject>(assetmanifestkey);
			_ = testAssets.UnrecycleSingle.UnLoad<GameObject>();
		}

		[UnityTest]
		public async Task TestRecycle3_2()
		{
			var op = AssetLoader.LoadAsset<GameObject>(assetmanifestkey);
			await op;
			var testAssets = op.Result.GetComponent<TestAssets>();

			var op2 = testAssets.UnrecycleSingle.Load<GameObject>();
			await op2;
			var asset = op2.Result;
			var recyclePool = new RecyclePool();

			recyclePool.LoadPrefab(asset, "uid3_2");

			var image = recyclePool.Get("uid3_2");
			Debug.Assert(image != null);
			var image2 = recyclePool.Get("uid3_2");
			Debug.Assert(image2 != null);
			var image3 = recyclePool.Get("uid3_2");
			Debug.Assert(image3 != null);

			_ = AssetLoader.UnLoadAsset<GameObject>(assetmanifestkey);
			_ = testAssets.UnrecycleSingle.UnLoad<GameObject>();
		}

		[UnityTest]
		public async Task TestRecycle4()
		{
			var op = AssetLoader.LoadAsset<GameObject>(assetmanifestkey);
			await op;
			var testAssets = op.Result.GetComponent<TestAssets>();

			var op2 = testAssets.UnusedSingle.Load<GameObject>();
			await op2;
			var asset = op2.Result;
			var recyclePool = new RecyclePool();

			recyclePool.LoadPrefab(asset);
			Debug.Assert(recyclePool.NodePools.Count == 0);

			_ = AssetLoader.UnLoadAsset<GameObject>(assetmanifestkey);
			_ = testAssets.UnrecycleSingle.UnLoad<GameObject>();
		}
#endif
	}
}
