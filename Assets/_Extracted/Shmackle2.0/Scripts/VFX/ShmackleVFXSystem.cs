using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Stratton.Core;
using UnityEngine;
using Stratton.VFX;

namespace Shmackle.VFX
{
    /// <summary>
    /// This C# script defines the `ShmackleVFXSystem` class, which manages all visual effects (VFX) in the game. 
    /// It handles loading a `VFXLibrary` from Addressable Assets, which contains the data for all the different effects. 
    /// To improve performance, it uses a pooling system, creating a pool of `VFXEmitter` objects for each effect defined in the library.
    /// The class provides several `Play` methods to start a VFX by its key, allowing it to be placed at a specific position or parented to a transform. 
    /// It also includes methods to `Stop` effects and to play them for a set duration. 
    /// The system initializes and manages `VFXListener` instances, which are responsible for triggering these VFX in response to in-game events. 
    /// The `Update` method ensures that all active VFX pools are properly updated each frame.
    /// </summary>
    public class ShmackleVFXSystem : VFXSystem
    {
        #region Serialized Fields
        
        [SerializeField] private List<VFXListener> _listeners;

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            builder.AddMessageBroker<VFXBaseEvent>();
        }
        
        public override async UniTask<InitializationResult> Init()
        {
            var result = await base.Init();
            foreach (var listener in _listeners)
            {
                listener.Init();
            }
            return result;
        }

        public override UniTask<DeinitializationResult> DeInit()
        {
            var result = base.DeInit();
            foreach (var listener in _listeners)
            {
                listener?.DeInit();
            }
            return result;
        }
        
        #endregion
    }
}
