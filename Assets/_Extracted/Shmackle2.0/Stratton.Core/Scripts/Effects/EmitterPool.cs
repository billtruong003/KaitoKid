using System.Collections.Generic;
using UnityEngine;
using Stratton.Core;

namespace Stratton.Effects
{
    public class EmitterPool : IUpdateable
    {
        protected int _poolSize;
        protected Transform _poolParent;
        protected Queue<ObjectEmitter> _queue = new Queue<ObjectEmitter>();
        protected List<ObjectEmitter> _emitters = new List<ObjectEmitter>();
        protected List<ObjectEmitter> _emittersToRemove = new List<ObjectEmitter>();

        public EmitterPool(int poolSize, Transform poolParent)
        {
            _poolSize = poolSize;
            _poolParent = poolParent;
        }

        public void OnUpdate()
        {
            foreach (ObjectEmitter emitter in _emitters)
            {
                // Could've been destroyed because of destroyed parent
                if (emitter == null || emitter.gameObject == null)
                {
                    _emittersToRemove.Add(emitter);
                    continue;
                }
                if (emitter.gameObject.activeSelf)
                {
                    emitter.OnUpdate();
                }
            }
            RemoveBrokenEmitters();
        }

        public void ReleaseEmitter(ObjectEmitter emitter)
        {
            EnqueueEmitter(emitter);
        }       

        protected void EnqueueEmitter(ObjectEmitter emitter)
        {
            emitter.gameObject.SetActive(false);
            emitter.SetParent(_poolParent);
            _queue.Enqueue(emitter);
        }

        protected void RemoveBrokenEmitters()
        {
            if (_emittersToRemove.Count == 0)
            {
                return;
            }
            foreach (var emitter in _emittersToRemove)
            {
                _emitters.Remove(emitter);
            }
        }
    }
}