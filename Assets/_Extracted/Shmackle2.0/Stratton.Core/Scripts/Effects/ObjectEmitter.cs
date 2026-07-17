using UnityEngine;
using Stratton.Core;

namespace Stratton.Effects
{
    public abstract class ObjectEmitter : MonoBehaviour, IUpdateable
    {
        protected bool _isPaused;
        protected EmitterPool _emitterPool;
        protected Transform _mainCameraTransform;

        private ObjectEmitterData _objectEmitterData;

        public ObjectEmitterData ObjectEmitterData
        {
            set { _objectEmitterData = value; }
            get { return _objectEmitterData; }
        }

        public virtual void Init(EmitterPool emitterPool)
        {
            _emitterPool = emitterPool;
        }

        public abstract void Play();
        public virtual void Play(Transform parent)
        {
            transform.parent = parent;
            Play();
        }
        public abstract void Pause();
        public abstract void Stop();

        public void SetPosition(Vector3 position)
        {         
            transform.position = position;
        }

        public void SetParent(Transform parent)
        {
            transform.parent = parent;
            transform.localPosition = Vector3.zero;
        }

        public void Release()
        {
            if (_emitterPool != null)
            {
                _emitterPool.ReleaseEmitter(this);
            }
        }

        public void SetMainCameraTransform(Transform mainCameraTransform)
        {
            _mainCameraTransform = mainCameraTransform;
        }

        public virtual void OnUpdate()
        {
            if (_objectEmitterData.IsGlobal)
            {
                return;
            }

            if (_mainCameraTransform != null)
            {
                if (!PriorityCalculator.IsWithinMaxDistanceToCamera(_mainCameraTransform, this))
                {
                    Release();
                }
            }
        }
    }
}