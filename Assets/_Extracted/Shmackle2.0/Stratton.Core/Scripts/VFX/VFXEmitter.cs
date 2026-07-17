using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Stratton.Effects;

namespace Stratton.VFX
{
    public class VFXEmitter : ObjectEmitter
    {
        #region Fields

        private ParticleSystem _particleSystem;
        private List<ParticleSystem> _childrenEmitters;

        #endregion

        #region Properties

        public ParticleSystem ParticleSystem => _particleSystem;

        public bool IsPlaying
        {
            get
            {
                return _particleSystem != null && _particleSystem.isPlaying || _childrenEmitters.TrueForAll(x => x.isPlaying);
            }
        }

        #endregion

        #region Public Methods 

        public override void Init(EmitterPool emitterPool)
        {
            base.Init(emitterPool);
            _emitterPool = emitterPool as VFXPool;
            _particleSystem = GetComponent<ParticleSystem>();
            _childrenEmitters = GetComponentsInChildren<ParticleSystem>().ToList();
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void ResetLocalRotation()
        {
            transform.localRotation = Quaternion.identity;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (_particleSystem != null && !_particleSystem.isPlaying && !_isPaused && !_particleSystem.main.loop)
            {
                Release();
            }
        }

        public override void Play()
        {
            gameObject.SetActive(true);
            if (_particleSystem != null && !IsPlaying)
            {
                _particleSystem.Play(true);
            }
        }

        public override void Pause()
        {
            if (_particleSystem != null)
            {
                _particleSystem.Pause(true);
            }
            _isPaused = true;
        }

        public override void Stop()
        {
            if (_particleSystem != null)
            {
                _particleSystem.Stop(true);
            }
            Release();
        }

        #endregion
    }
}