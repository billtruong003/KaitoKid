using Stratton.Core;
using UnityEngine;

namespace Stratton.Audio
{
    public abstract class AudioListenerBase : MonoBehaviour, Core.IInitializable
    {
        #region Fields

        protected AudioSystem _audioSystem;

        #endregion

        #region Properties

        public bool IsReady { get; protected set; }

        #endregion

        #region Public Methods

        public virtual void Init()
        {
            _audioSystem = GameSystemsManager.Instance.Get<AudioSystem>();
            IsReady = true;
        }

        public virtual void DeInit()
        {
            IsReady = false;
        }

        #endregion
    }
}