using System;
using System.Collections.Generic;
using System.Linq;
using Shmackle.Data;
using Shmackle.Runtime;
using UnityEngine;

namespace _Shmackle.Scripts.Player
{
    [Serializable]
    public class CharacterPart
    {
        #region #----- Fields -----#

        public CharacterPartType PartType;
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        public DripData_Runtime DripData;

        #endregion
    }

    public enum CharacterPartType
    {
        Body,
        Head,
        Hand,
        Tape,
        Finger,
        FaceDecor,
        ArmLeft,
        ArmRight,
        Face,
        Belt,
        Neck,
        Gun,
        Top,
        Badge,
        Back,
        Teeth,
        Beard,
        Hair,
        Ear,
        Helmet,
        BodyCover,
    }

    [Flags]
    public enum CharacterPartFlags
    {
        Body = 1 << 0,
        Head = 1 << 1,
        Hand = 1 << 2,
        Tape = 1 << 3,
        Finger = 1 << 4,
        FaceDecor = 1 << 5,
        ArmLeft = 1 << 6,
        ArmRight = 1 << 7,
        Face = 1 << 8,
        Belt = 1 << 9,
        Neck = 1 << 10,
        Gun = 1 << 11,
        Top = 1 << 12,
        Badge = 1 << 13,
        Back = 1 << 14,
        Teeth = 1 << 15,
        Beard = 1 << 16,
        Hair = 1 << 17,
        Ear = 1 << 18,
        Helmet = 1 << 19,
        BodyCover = 1 << 20,
    }

    public class CharacterPartController : MonoBehaviour
    {
        #region #----- Fields -----#

        [SerializeField]
        private CharacterPart[] _characterParts;
        [SerializeField]
        private Mesh _defaultBodyMesh;
        public Material[] _defaultBodyMaterials;
        [SerializeField]
        public Material[] _defaultBodyMaterialsGert;
        [SerializeField]
        private Material _ghostMaterial;

        public Material _defaultBodyDissolveMaterial;
        public Material _defaultBodyDissolveDrip;
        [Header("Spine")]
        public GameObject _spineGameObject;

        [Header("Monster")]
        [SerializeField] private Mesh _monsterMesh;
        [SerializeField] private Material[] _monsterMaterials;
        [SerializeField] private Material[] _monsterInvisibleMaterials;
        public Dictionary<CharacterPartType, CharacterPart> characterPartMap
        {
            get
            {
                if (_characterPartMap == null)
                {
                    _characterPartMap = new(_characterParts.Length);
                    foreach (var part in _characterParts)
                    {
                        _characterPartMap[part.PartType] = part;
                    }
                }

                return _characterPartMap;
            }
        }

        private Dictionary<CharacterPartType, CharacterPart> _characterPartMap;
        private readonly Material[] _defaultEmptyMaterials = new Material[0];
        private readonly string _gloveTextureString = "_UseGloveTexture";
        private readonly string _gertStateShaderName = "_GertState";
        public BloodJmanTransform bloodJmanTransform;
        #endregion

        #region #----- Public Methods -----#

        public void SetPartLayers(List<(CharacterPartType part, LayerMask layer)> partLayers)
        {
            foreach (var partLayer in partLayers)
            {
                if (TryGetCharacterPart(partLayer.part, out var characterPart))
                {
                    characterPart.SkinnedMeshRenderer.gameObject.layer = partLayer.layer;
                }
            }
        }

        public bool TryEquipCharacterPart(DripData_Runtime data)
        {
            // TODO: Remove specific handle for Hand.
            if (false) //(data.CharacterPartType == CharacterPartType.Hand)
            {
                if (characterPartMap.TryGetValue(CharacterPartType.Body, out var bodyPart))
                {
                    bodyPart.SkinnedMeshRenderer.material.SetFloat(_gloveTextureString, 1.0f);
                    return true;
                }
                else
                {
                    Debug.LogError($"[CharacterPartController] TryEquipCharacterPart body part is null.");
                    return false;
                }
            }
            else
            {
                if (data == null)
                {
                    Debug.LogError($"[CharacterPartController] TryEquipCharacterPart data is null.");
                    return false;
                }

                if (data.CharacterPartMesh == null ||
                    data.CharacterPartMaterial == null)
                {
                    Debug.LogError($"[CharacterPartController] DripManager TryEquipCharacterPart data mesh or material is null.");
                    return false;
                }

                if (TryGetCharacterPart(data.CharacterPartType, out var characterPart))
                {
                    characterPart.SkinnedMeshRenderer.sharedMesh = data.CharacterPartMesh;
                    characterPart.SkinnedMeshRenderer.materials = new[] { data.CharacterPartMaterial };
                    characterPart.DripData = data;

                    Debug.Log($"[CharacterPartController] DripManager Equip character part: {data.CharacterPartType} characterPart.SkinnedMeshRenderer.sharedMesh {characterPart.SkinnedMeshRenderer.sharedMesh}");

                    characterPartMap[data.CharacterPartType] = characterPart;
                    
                    characterPart.SkinnedMeshRenderer.gameObject.SetActive(true);

                    return true;
                }
            }

            return false;
        }

        public bool TryUnequipCharacterPart(DripData_Runtime data)
        {
            return TryUnequipCharacterPart(data.CharacterPartType);
        }

        public bool TryUnequipCharacterPart(CharacterPartType partType)
        {
            // TODO: Remove specific handle for Hand.
            if (false)//(partType == CharacterPartType.Hand)
            {
                if (characterPartMap.TryGetValue(CharacterPartType.Body, out var bodyPart))
                {
                    bodyPart.SkinnedMeshRenderer.material.SetFloat(_gloveTextureString, 0.0f);
                    return true;
                }
                else
                {
                    Debug.LogError($"[CharacterPartController] DripManager TryUnequipCharacterPart body part is null.");
                    return false;
                }
            }
            else
            {
                if (TryGetCharacterPart(partType, out var characterPart))
                {
                    characterPart.SkinnedMeshRenderer.sharedMesh  = null;
                    characterPart.SkinnedMeshRenderer.materials   = _defaultEmptyMaterials;

                    if (characterPart.DripData != null)
                    {
                        characterPart.DripData.DripDataTransformation = null;
                        characterPart.DripData                        = null;
                    }

                    // Debug.Log($"[CharacterPartController] DripManager Unequip character part: {partType}");

                    var isVisible = false;

                    if (partType == CharacterPartType.Body)
                    {
                        characterPart.SkinnedMeshRenderer.sharedMesh = _defaultBodyMesh;
                        characterPart.SkinnedMeshRenderer.materials = _defaultBodyMaterials;

                        isVisible = true;
                    }
                    
                    characterPart.SkinnedMeshRenderer.gameObject.SetActive(isVisible);

                    characterPartMap[partType] = characterPart;

                    return true;
                }
            }

            return false;
        }

        public bool IsCharacterPartEquipped(CharacterPartType partType)
        {
            return GetCharacterPartData(partType) != null;
        }

        public DripData_Runtime GetCharacterPartData(CharacterPartType characterPartType)
        {
            if (TryGetCharacterPart(characterPartType, out var characterPart))
            {
                return characterPart.DripData;
            }

            return null;
        }

        public void SetBlendShapeWeight(
            Dictionary<BlendShapeType, int> blendShapeWeights,
            Dictionary<BlendShapeNeckType, int> blendShapeNeckWeights,
            Dictionary<BlendShapeBeltType, int> blendShapeBeltWeights,
            Dictionary<BlendShapeBadgeType, int> blendShapeBadgeWeights)
        {
            //Body
            if (TryGetCharacterPart(CharacterPartType.Body, out var characterPart))
            {
                foreach (var kvp in blendShapeWeights)
                {
                    if ((int)kvp.Key < characterPart.SkinnedMeshRenderer.sharedMesh.blendShapeCount)
                    {
                        characterPart.SkinnedMeshRenderer.SetBlendShapeWeight((int)kvp.Key, kvp.Value);
                    }
                    else
                    {
                        Debug.LogWarning($"BlendShape Weight is out of range. {kvp.Key}");
                    }

                    if (TryGetCharacterPart(CharacterPartType.BodyCover, out var bodyCoverPart))
                    {
                        if (bodyCoverPart.SkinnedMeshRenderer.sharedMesh != null
                            && (int)kvp.Key < bodyCoverPart.SkinnedMeshRenderer.sharedMesh.blendShapeCount)
                        {
                            bodyCoverPart.SkinnedMeshRenderer.SetBlendShapeWeight((int)kvp.Key, kvp.Value);
                        }
                    }

                    if (kvp.Key == BlendShapeType.TeethShow)
                    {
                        if (TryGetCharacterPart(CharacterPartType.Beard, out var beardPart))
                        {
                            if (beardPart.SkinnedMeshRenderer.sharedMesh != null
                                && (int)kvp.Key < beardPart.SkinnedMeshRenderer.sharedMesh.blendShapeCount)
                            {
                                beardPart.SkinnedMeshRenderer.SetBlendShapeWeight((int)kvp.Key, kvp.Value);
                            }
                        }

                        if (TryGetCharacterPart(CharacterPartType.FaceDecor, out var faceDecorPart))
                        {
                            if (faceDecorPart.SkinnedMeshRenderer.sharedMesh != null
                                && (int)kvp.Key < faceDecorPart.SkinnedMeshRenderer.sharedMesh.blendShapeCount)
                            {
                                faceDecorPart.SkinnedMeshRenderer.SetBlendShapeWeight((int)kvp.Key, kvp.Value);
                            }
                        }
                        
                        if (TryGetCharacterPart(CharacterPartType.Head, out var head))
                        {
                            if (head.SkinnedMeshRenderer.sharedMesh != null
                                && (int)kvp.Key < head.SkinnedMeshRenderer.sharedMesh.blendShapeCount)
                            {
                                head.SkinnedMeshRenderer.SetBlendShapeWeight((int)kvp.Key, kvp.Value);
                            }
                        }
                    }

                    if (kvp.Key == BlendShapeType.NerdFace)
                    {
                        if (TryGetCharacterPart(CharacterPartType.FaceDecor, out var faceDecorPart))
                        {
                            if (faceDecorPart.SkinnedMeshRenderer.sharedMesh != null
                                && (int)kvp.Key < faceDecorPart.SkinnedMeshRenderer.sharedMesh.blendShapeCount)
                            {
                                faceDecorPart.SkinnedMeshRenderer.SetBlendShapeWeight((int)kvp.Key, kvp.Value);
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"[CharacterPartController] SetBlendShapeWeight body part is null.");
            }

            //Neck
            if (TryGetCharacterPart(CharacterPartType.Neck, out var characterPartNeck) && characterPartNeck.SkinnedMeshRenderer.sharedMesh != null)
            {
                foreach (var kvp in blendShapeNeckWeights)
                {
                    if ((int)kvp.Key < characterPartNeck.SkinnedMeshRenderer.sharedMesh.blendShapeCount)
                    {
                        characterPartNeck.SkinnedMeshRenderer.SetBlendShapeWeight((int)kvp.Key, kvp.Value);
                    }
                    else
                    {
                        Debug.LogWarning($"BlendShape Weight is out of range. {kvp.Key}");
                    }
                }
            }

            //Belt
            if (TryGetCharacterPart(CharacterPartType.Belt, out var characterPartbelt) && characterPartbelt.SkinnedMeshRenderer.sharedMesh != null)
            {
                foreach (var kvp in blendShapeBeltWeights)
                {
                    if ((int)kvp.Key < characterPartbelt.SkinnedMeshRenderer.sharedMesh.blendShapeCount)
                    {
                        characterPartbelt.SkinnedMeshRenderer.SetBlendShapeWeight((int)kvp.Key, kvp.Value);
                    }
                    else
                    {
                        Debug.LogWarning($"BlendShape Weight is out of range. {kvp.Key}");
                    }
                }
            }

            //Badge
            if (TryGetCharacterPart(CharacterPartType.Badge, out var characterPartBadge) && characterPartBadge.SkinnedMeshRenderer.sharedMesh != null)
            {
                foreach (var kvp in blendShapeBadgeWeights)
                {
                    if ((int)kvp.Key < characterPartBadge.SkinnedMeshRenderer.sharedMesh.blendShapeCount)
                    {
                        characterPartBadge.SkinnedMeshRenderer.SetBlendShapeWeight((int)kvp.Key, kvp.Value);
                    }
                    else
                    {
                        Debug.LogWarning($"BlendShape Weight is out of range. {kvp.Key}");
                    }
                }
            }
        }

        public void SetSpineActiveState(bool isActive)
        {
            if (_spineGameObject != null)
            {
                _spineGameObject.SetActive(isActive);
            }
        }

        public void SetGertMaterial(DripData_Runtime dripDataBody, bool isGert)
        {
            if (TryGetCharacterPart(CharacterPartType.Body, out var bodyPart))
            {
                var mesh = dripDataBody != null ? dripDataBody.CharacterPartMesh : _defaultBodyMesh;
                bodyPart.SkinnedMeshRenderer.sharedMesh = mesh;

                if (dripDataBody != null)
                {
                    bodyPart.SkinnedMeshRenderer.material = isGert ? dripDataBody.CharacterGertMaterial : dripDataBody.CharacterPartMaterial;
                }
                else
                {
                    bodyPart.SkinnedMeshRenderer.materials = isGert ? _defaultBodyMaterialsGert : _defaultBodyMaterials;
                }
            }
        }

        public void SetGertMaterialProperty(float percentage)
        {

            foreach (var kvp in characterPartMap)
            {
                var material = kvp.Value.SkinnedMeshRenderer.material;
                if (material != null &&
                    material.HasProperty(_gertStateShaderName))
                {
                    material.SetFloat(_gertStateShaderName, percentage);
                }
            }
        }

        public void ResetPart()
        {
            foreach (var key in characterPartMap.Keys.ToList())
            {
                TryUnequipCharacterPart(key);
            }
        }

        public void SetDissolveMaterial(CharacterPart part, DripData_Runtime dripRuntime)
        {
            part.SkinnedMeshRenderer.material = dripRuntime.CharacterDissolveMaterial;
        }

        public void SetDefaultMaterial(CharacterPart part, DripData_Runtime dripRuntime)
        {
            if (part.SkinnedMeshRenderer.materials != null && part.SkinnedMeshRenderer.materials.Length > 1)
            {
                part.SkinnedMeshRenderer.materials = new[] { dripRuntime.CharacterPartMaterial };
            }
            else
            {
                part.SkinnedMeshRenderer.material = dripRuntime.CharacterPartMaterial;
            }
            dripRuntime.CharacterPartMaterial.SetFloat("_DissolveThreshhold", -1.5f);
        }

        public void SetPartMaterial(CharacterPart part, Material ghostMaterial)
        {
            part.SkinnedMeshRenderer.material = ghostMaterial;
        }

        public void SetPartsMaterial(CharacterPart part, Material addMaterial)
        {
            var materials = part.SkinnedMeshRenderer.materials;
            if (materials == null || materials.Length == 0)
            {
                return;
            }

            part.SkinnedMeshRenderer.materials = new Material[] { materials[0], addMaterial };
        }

        public void SetMonsterPart(bool isVisible)
        {
            if (TryGetCharacterPart(CharacterPartType.Body, out var bodyPart))
            {
                bodyPart.SkinnedMeshRenderer.sharedMesh = _monsterMesh;
                bodyPart.SkinnedMeshRenderer.materials = isVisible ? _monsterMaterials : _monsterInvisibleMaterials;
            }
        }

        public void ReplicatePart(CharacterPart[] parts, bool replicateOriginalMaterialData = false)
        {
            ResetPart();

            foreach (var part in parts)
            {
                if (TryGetCharacterPart(part.PartType, out var characterPart))
                {
                    characterPart.SkinnedMeshRenderer.sharedMesh = part.SkinnedMeshRenderer.sharedMesh;

                    if (characterPart.SkinnedMeshRenderer.sharedMesh != null && characterPart.SkinnedMeshRenderer.sharedMesh.blendShapeCount > 0)
                    {
                        for (int i = 0; i < characterPart.SkinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
                        {
                            characterPart.SkinnedMeshRenderer.SetBlendShapeWeight(i, part.SkinnedMeshRenderer.GetBlendShapeWeight(i));
                        }
                    }
                    
                    if (!replicateOriginalMaterialData)
                    {
                        characterPart.SkinnedMeshRenderer.materials  = part.SkinnedMeshRenderer.sharedMaterials;
                    }
                    else
                    {
                        if (part.DripData != null)
                        {
                            characterPart.SkinnedMeshRenderer.materials  = new[] { part.DripData.CharacterPartMaterial };
                        }
                        else if (part.PartType == CharacterPartType.Body)
                        {
                            characterPart.SkinnedMeshRenderer.materials  = _defaultBodyMaterials;
                        }
                        else
                        {
                            Debug.LogWarning($"[CharacterPartController] ReplicatePart DripData is null and PartType is body, so we reuse other part.");
                            characterPart.SkinnedMeshRenderer.materials  = part.SkinnedMeshRenderer.sharedMaterials;
                        }
                    }
                    
                    characterPart.SkinnedMeshRenderer.gameObject.SetActive(part.SkinnedMeshRenderer.gameObject.activeSelf);

                    // TODO: Remove specific handle for Hand.
                    if (false) //(part.PartType == CharacterPartType.Hand)
                    {
                        if (characterPart.SkinnedMeshRenderer.material != null &&
                            part.SkinnedMeshRenderer.material != null &&
                            characterPart.SkinnedMeshRenderer.material.HasProperty(_gloveTextureString) &&
                            part.SkinnedMeshRenderer.material.HasProperty(_gloveTextureString))
                        {
                            characterPart.SkinnedMeshRenderer.material.SetFloat(_gloveTextureString, part.SkinnedMeshRenderer.material.GetFloat(_gloveTextureString));
                        }
                    }
                }
            }
        }

        public CharacterPart[] GetCharacterParts()
        {
            return characterPartMap.Values.ToArray();
        }
        
        public bool TryGetCharacterPart(CharacterPartType partType, out CharacterPart characterPart)
        {
            if (characterPartMap.TryGetValue(partType, out var part))
            {
                characterPart = part;
                return true;
            }

            Debug.LogError($"[CharacterPartController] No part found for {partType}");
            characterPart = default;
            return false;
        }
        #endregion

        #region #----- Private Methods -----#
        public bool IsTypeAllowedByFlags(CharacterPartFlags allowedFlags, CharacterPartType partType)
        {
            return (allowedFlags & (CharacterPartFlags)(1 << (int)partType)) != 0;
        }

        public bool IsTypeFobbidenByFlags(CharacterPartFlags fobbidenFlags, CharacterPartType partType)
        {
            return (fobbidenFlags & (CharacterPartFlags)(1 << (int)partType)) != 0;
        }
        #endregion
    }
}