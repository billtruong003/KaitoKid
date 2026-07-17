using System;
using MessagePipe;
using UnityEngine;

namespace Shmackle.VFX
{
    /// <summary>
    /// This class is responsible for handling visual effects (VFX) in response to game events. 
    /// It subscribes to VFXEvent messages using a message pipe. When a VFXEvent is received, 
    /// the OnPlayExplodeEvent method is triggered, which then plays a VFX at the specified target's position. 
    /// The key for the VFX to be played is stored in the _vfxExplodeKey field. 
    /// The class also manages the lifecycle of its event subscriptions, 
    /// ensuring they are properly created and disposed of during the Init and DeInit phases.
    /// </summary>
    public class VFXListener : VFXListenerBase
    {
        #region Serialized Fields
        
        [SerializeField] private string _vfxExplodeKey;
        
        #endregion
        
        #region Fields
        
        private IDisposable _eventsBagDisposable;
        private ISubscriber<VFXBaseEvent> _explodeEventSubscriber;
        
        #endregion
        
        #region Public Methods
        
        public override void Init()
        {
            base.Init();
            _explodeEventSubscriber = GlobalMessagePipe.GetSubscriber<VFXBaseEvent>();
            var bag = DisposableBag.CreateBuilder();
            _explodeEventSubscriber.Subscribe(e => OnPlayExplodeEvent(_vfxExplodeKey, e.Target)).AddTo(bag);
            _eventsBagDisposable = bag.Build();
        }

        public override void DeInit()
        {
            _eventsBagDisposable?.Dispose();
            base.DeInit();
        }

        private void OnPlayExplodeEvent(string vfxKey, Transform target)
        {
            _vfxSystem.Play(vfxKey, target.position, target);
        }
        
        #endregion
    }
}
