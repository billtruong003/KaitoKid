using System;
using System.Linq;
using Shmackle.Data;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Shmackle.Scripts.Player
{
    public enum SpineSimpleType
    {
        Normal,
        Bulky,
        Samurai,
        SpaceSuit,
        KnightArmor_Thrill,
        Mecha,
        Gold,
        UniBro,
    }
    
    public class CharacterSpineSimpleController : CharacterSpineController
    {
        [SerializeField]
        [DictionaryDrawerSettings(DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
        private SerializedDictionary<SpineSimpleType, GameObject> _spineSimpleMaps;
        
        public override void Setup(DripData_Runtime dripData)
        {
            _dripDataRuntime = dripData;

            var targetSpineType = ParseSpineType(dripData);
            foreach (var spine in _spineSimpleMaps)
            {
                spine.Value.SetActive(spine.Key == targetSpineType);
            }
        }

        private SpineSimpleType ParseSpineType(DripData_Runtime runtime)
        {
            if (runtime == null)
            {
                return SpineSimpleType.Normal;
            }

            switch (runtime.SpineType)
            {
                case SpineType.Pack1:
                    var spineBlendshapeKey = runtime.SpineBlendShapes.FirstOrDefault(x => x.BlendShapeValue > 0);
                    return spineBlendshapeKey.BlendShapeValue <= 0 ? SpineSimpleType.Normal : 
                               (spineBlendshapeKey.BlendShapeType switch
                                {
                                    SpineBlendShapeKey.Bulky => SpineSimpleType.Bulky,
                                    SpineBlendShapeKey.Samurai => SpineSimpleType.Samurai,
                                    SpineBlendShapeKey.SpaceSuit => SpineSimpleType.SpaceSuit,
                                    SpineBlendShapeKey.KnightArmorAndTheThrill => SpineSimpleType.KnightArmor_Thrill,
                                    _ => throw new NotImplementedException(spineBlendshapeKey.BlendShapeType.ToString()),
                                });
                case SpineType.Pack2:
                    return SpineSimpleType.Mecha;
                case SpineType.Pack3:
                    return SpineSimpleType.Gold;
                case SpineType.Pack7:
                    return SpineSimpleType.UniBro;
                default: throw new NotImplementedException(runtime.SpineType.ToString());
            }
        }
    }
}