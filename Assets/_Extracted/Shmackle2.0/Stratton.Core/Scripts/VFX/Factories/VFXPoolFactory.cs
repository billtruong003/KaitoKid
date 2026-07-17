using UnityEngine;

namespace Stratton.VFX.Factories
{
    public class VFXPoolFactory
    {
        public VFXPool Create(VFXEmitterFactory vfxEmitterFactory, int poolSize, Transform poolParent, GameObject vfxTemplate)
        {
            var vfxPool = new VFXPool(vfxEmitterFactory, poolSize, poolParent, vfxTemplate);
            return vfxPool;
        }
    }
}