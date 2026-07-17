using UnityEngine;
using UnityEngine.AddressableAssets;
using System;

namespace Stratton.Core
{
    [CreateAssetMenu(fileName = "Scene Asset References", menuName = "Data/Addressables/Scene Asset References")]
    public class AddressablesSceneList : ScriptableObject
    {
        [SerializeField] private SceneAssetReference[] _sceneAssetReferences;

        public SceneAssetReference[] SceneAssetReferences => _sceneAssetReferences;
    }

    [Serializable]
    public class SceneAssetReference
    {
        public string SceneName;
        public AssetReference SceneReference;
    }
}