using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Player;

namespace MiiAsset.Editor.BuildPipelineUtilities
{
    [Serializable]
    struct ObjectTypes
    {
        public ObjectIdentifier ObjectID;
        public Type[] Types;

        public ObjectTypes(ObjectIdentifier objectID, Type[] types)
        {
            ObjectID = objectID;
            Types = types;
        }
    }

    static class ExtensionMethods
    {
        public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }

        public static void GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value) where TValue : new()
        {
            if (dictionary.TryGetValue(key, out value))
                return;

            value = new TValue();
            dictionary.Add(key, value);
        }

        public static void Swap<T>(this IList<T> list, int first, int second)
        {
            T temp = list[second];
            list[second] = list[first];
            list[first] = temp;
        }

        /// <summary>
        /// <summary>递归收集引用的对象</summary>
        /// <para>递归分析不在依赖列表中的资源依赖, 直到不再差分新的资源依赖, 最终结果为依赖列表和不在依赖列表中的资源的和</para>
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="references"></param>
        /// <param name="target"></param>
        /// <param name="typeDB"></param>
        /// <param name="dependencies"></param>
        /// <returns></returns>
        public static ObjectIdentifier[] FilterReferencedObjectIDs(GUID asset, ObjectIdentifier[] references, BuildTarget target, TypeDB typeDB, HashSet<GUID> dependencies)
        {
            // Expectation: references is populated with DependencyType.ValidReferences only for the given asset
            var collectedImmediateReferences = new HashSet<ObjectIdentifier>();
            var encounteredDependencies = new HashSet<ObjectIdentifier>();
            while (references.Length > 0)
            {
                // Track which roots we encounter to do dependency pruning
                encounteredDependencies.UnionWith(references.Where(x => x.guid != asset && dependencies.Contains(x.guid)));
                // We only want to recursively grab references for objects being pulled in and won't go to another bundle
                ObjectIdentifier[] immediateReferencesNotInOtherBundles =  references.Where(x => !dependencies.Contains(x.guid) && !collectedImmediateReferences.Contains(x)).ToArray();
                collectedImmediateReferences.UnionWith(immediateReferencesNotInOtherBundles);
                // Grab next set of valid references and loop
                references = ContentBuildInterface.GetPlayerDependenciesForObjects(immediateReferencesNotInOtherBundles, target, typeDB, DependencyType.ValidReferences);
            }

            // We need to ensure that we have a reference to a visible representation so our runtime dependency appending process
            // can find something that can be appended, otherwise the necessary data will fail to load correctly in all cases. (EX: prefab A has reference to component on prefab B)
            foreach (var dependency in encounteredDependencies)
            {
                // For each dependency, add just the main representation as a reference
                var representations = ContentBuildInterface.GetPlayerAssetRepresentations(dependency.guid, target);
                collectedImmediateReferences.Add(representations.First());
            }
            collectedImmediateReferences.UnionWith(encounteredDependencies);
            return collectedImmediateReferences.ToArray();
        }

    }
}
