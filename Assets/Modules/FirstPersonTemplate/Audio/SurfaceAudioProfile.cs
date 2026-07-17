using UnityEngine;
using System.Collections.Generic;

namespace GameSystem.Audio
{
    [CreateAssetMenu(fileName = "NewSurfaceProfile", menuName = "Audio/Surface Profile")]
    public class SurfaceAudioProfile : ScriptableObject
    {
        [System.Serializable]
        public struct SurfaceAudioData
        {
            public SurfaceType surfaceType;
            public AudioClip[] clips;
        }

        [SerializeField] private List<SurfaceAudioData> surfaceDataList = new List<SurfaceAudioData>();

        public AudioClip GetRandomClip(SurfaceType type)
        {
            foreach (var data in surfaceDataList)
            {
                if (data.surfaceType == type && data.clips.Length > 0)
                {
                    int randomIndex = Random.Range(0, data.clips.Length);
                    return data.clips[randomIndex];
                }
            }
            return null;
        }
    }
}