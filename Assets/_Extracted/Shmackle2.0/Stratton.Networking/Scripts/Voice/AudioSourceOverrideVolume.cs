using UnityEngine;

namespace Stratton.Networking.Voice
{
    /// <summary>
    /// Store fixed volume of an audio source and used as a reference value.
    /// Useful for changing direct audio source volume (animation, fades, mute, etc)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourceOverrideVolume : MonoBehaviour
    {
        #region Properties

        public float Volume 
        {
            get
            {
                return _volume;
            }
            set
            {
                _volume = value;
                _audioSource.volume = _volume;
            }
        }

        #endregion
        #region Private Fields

        private AudioSource _audioSource;
        private float _volume = 1.0f;

        #endregion

        #region Private Methods

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _volume = _audioSource.volume;
        }

        #endregion
    }
}
