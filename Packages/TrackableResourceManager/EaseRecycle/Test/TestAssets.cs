using System.Collections;
using System.Collections.Generic;
using MiiAsset.AssetWeakRefer.Runtime;
using UnityEngine;

namespace EaseRecycle.Tests
{
    public class TestAssets : MonoBehaviour
    {
        public AssetReference Single1;
        public AssetReference RecycleSingle;

        public AssetReference RecycleRecursive;
        public AssetReference RecycleRecursive2;

        public AssetReference UnrecycleSingle;

        public AssetReference UnusedSingle;

    }
}
