using System;
using System.Linq;
using Shmackle.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Shmackle.Scripts.Player
{
    public enum SpineType
    {
        Pack1,
        Pack2,
        Pack3,
        Pack7
    }

    public enum SpineBlendShapeKey
    {
        Bulky,
        Samurai,
        SpaceSuit,
        KnightArmorAndTheThrill
    }
    
    public class CharacterSpineController : MonoBehaviour
    {
        [SerializeField]
        [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
        private SerializedDictionary<SpineType, SkinnedMeshRenderer> _spineMaps;

        [SerializeField]
        [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
        protected DripData_Runtime _dripDataRuntime;
        
        public DripData_Runtime DripDataRuntime => _dripDataRuntime;
        
        public virtual void Setup(DripData_Runtime dripData)
        {
            _dripDataRuntime = dripData;
            
            var dict = Enum.GetValues(typeof(SpineBlendShapeKey))
                           .Cast<SpineBlendShapeKey>()
                           .ToDictionary(e => e, e => 0);
            
            var targetSpineType = dripData != null ? dripData.SpineType : SpineType.Pack1;
            
            foreach (var spine in _spineMaps)
            {
                if (spine.Value != null)
                {
                    if (spine.Value.gameObject != null)
                    {
                        spine.Value.gameObject.SetActive(targetSpineType == spine.Key);
                    }
                
                    if (targetSpineType == spine.Key)
                    {
                        //Apply data from DripData_Runtime to dict
                        if (dripData != null && dripData.SpineBlendShapes != null && dripData.SpineBlendShapes.Length > 0)
                        {
                            foreach (var blendShapeData in dripData.SpineBlendShapes)
                            {
                                if (dict.ContainsKey(blendShapeData.BlendShapeType))
                                {
                                    dict[blendShapeData.BlendShapeType] = blendShapeData.BlendShapeValue;
                                }
                            }
                        }
                    
                        // Apply to skinnedMeshRenderer of spine.
                        foreach (var data in dict)
                        {
                            if ((int)data.Key < spine.Value.sharedMesh.blendShapeCount)
                            {
                                spine.Value.SetBlendShapeWeight((int)data.Key, data.Value);
                            }
                            else
                            {
                                Debug.LogWarning($"[CharacterSpineController] BlendShape Weight is out of range. {data.Key}");
                            }
                        }
                    }
                }
            }
        }
        
        public void ReplicateSpine(CharacterSpineController spineController)
        {
            Setup(spineController.DripDataRuntime);
        }
    }
}