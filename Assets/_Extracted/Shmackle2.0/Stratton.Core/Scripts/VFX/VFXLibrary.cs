using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.VFX
{
    [Serializable]
    [CreateAssetMenu(fileName = "New VFX Library", menuName = "Data/VFX/VFX Library")]
    public class VFXLibrary : ScriptableObject
    {
        [SerializeField] private List<VFXData> _vfxData = new List<VFXData>();

        public List<VFXData> VFXData => _vfxData;

        public bool TryGetVFXData(string vfxKey, out VFXData vfxData)
        {
            foreach (var data in _vfxData)
            {
                if (data.VFXKey == vfxKey)
                {
                    vfxData = data;
                    return true;
                }
            }
            vfxData = null;
            return false;
        }
    }
}