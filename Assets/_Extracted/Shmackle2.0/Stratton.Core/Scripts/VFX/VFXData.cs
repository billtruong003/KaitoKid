using UnityEngine;
using System;
using Stratton.Effects;

namespace Stratton.VFX
{
    [Serializable]
    public class VFXData: ObjectEmitterData
    {
        public string VFXKey = "";
        public GameObject VFXObject;
        public int PoolSize = 1;
    }
}
