using System.Collections.Generic;
using System.Linq;
using Stratton.Effects;
using Stratton.VFX.Factories;
using UnityEngine;

namespace Stratton.VFX
{
    public class VFXPool : EmitterPool
    {
        #region Fields

        protected VFXEmitterFactory _vfxEmitterFactory;

        private GameObject _vfxTemplate;

        #endregion

        #region Properties

        public List<VFXEmitter> VFXEmitters => _emitters.Cast<VFXEmitter>().ToList();

        #endregion

        #region Public Methods

        public VFXPool(VFXEmitterFactory vfxEmitterFactory, int poolSize, Transform poolParent, GameObject vfxTemplate) : base(poolSize, poolParent)
        {
            _vfxEmitterFactory = vfxEmitterFactory;
            _vfxTemplate = vfxTemplate;
        }

        public void PrefillPool()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var vfxEmitter = CreateVfxEmitter();
                EnqueueVFXEmitter(vfxEmitter);
            }
        }

        public void ReleaseVFXEmitter(VFXEmitter audioEmitter)
        {
            EnqueueVFXEmitter(audioEmitter);
        }

        public VFXEmitter GetVFXEmitter()
        {
            if (_queue.Count > 0)
            {
                return _queue.Dequeue() as VFXEmitter;
            }
            else
            {
                return CreateVfxEmitter();
            }
        }

        #endregion

        #region Private Methods

        private VFXEmitter CreateVfxEmitter()
        {
            var vfxEmitter = _vfxEmitterFactory.Create(_vfxTemplate);
            vfxEmitter.Init(this);
            vfxEmitter.transform.parent = _poolParent;
            _emitters.Add(vfxEmitter);
            return vfxEmitter;
        }

        private void EnqueueVFXEmitter(VFXEmitter vfxEmitter)
        {
            vfxEmitter.gameObject.SetActive(false);
            vfxEmitter.SetParent(_poolParent);
            _queue.Enqueue(vfxEmitter);
        }

        #endregion
    }
}