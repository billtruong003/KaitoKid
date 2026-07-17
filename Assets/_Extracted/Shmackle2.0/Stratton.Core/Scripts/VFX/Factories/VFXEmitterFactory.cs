using UnityEngine;

namespace Stratton.VFX.Factories
{
    public class VFXEmitterFactory
    {
        public VFXEmitter Create(GameObject template)
        {
            var vfxEmitterGameObject = GameObject.Instantiate(template);
            var vfxEmitter = vfxEmitterGameObject.AddComponent<VFXEmitter>();
            return vfxEmitter;
        }
    }
}