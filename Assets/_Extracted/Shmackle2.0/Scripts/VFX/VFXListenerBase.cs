using UnityEngine;
using Stratton.Core;

namespace Shmackle.VFX
{
    /// <summary>
    /// This class serves as a base for VFX listeners. It's an abstract class that implements the IInitializable interface.
    /// When initialized, it retrieves an instance of the ShmackleVFXSystem and sets the IsReady flag to true.
    /// The DeInit method sets the IsReady flag to false.
    /// This class provides a basic structure for managing the initialization and de-initialization of VFX listeners.
    /// </summary>
    public abstract class VFXListenerBase : MonoBehaviour, IInitializable
    {
        #region Fields

        protected ShmackleVFXSystem _vfxSystem;

        #endregion
        
        #region Properties
        
        public bool IsReady { get; protected set; }
        
        #endregion
        
        #region Public Methods
        
        public virtual void Init()
        {
            _vfxSystem = GameSystemsManager.Instance.Get<ShmackleVFXSystem>();
            IsReady = true;
        }

        public virtual void DeInit()
        {
            IsReady = false;
        }
        
        #endregion
    }
}
