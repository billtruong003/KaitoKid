using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Audio
{
    [Serializable]
    [CreateAssetMenu(fileName = "New Audio Library", menuName = "Data/Audio/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        #region Fields
        [SerializeField] private int _poolSize = 5;

        [SerializeField] private List<AudioData> _audioData = new List<AudioData>();

        #endregion

        #region Properties

        public int PoolSize => _poolSize;

        public List<AudioData> AudioData => _audioData;

        #endregion

        #region Properties

        public bool TryGetAudioData(string audioKey, out AudioData audioData)
        {
            foreach (var data in _audioData)
            {
                if (data.AudioKey == audioKey)
                {
                    audioData = data;
                    return true;
                }
            }
            audioData = null;
            return false;
        }

        #endregion
    }
}