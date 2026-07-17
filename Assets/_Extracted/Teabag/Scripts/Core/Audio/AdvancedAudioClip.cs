using System;
using UnityEngine;

namespace Teabag.Core
{
    /// <summary>
    /// Represents an advanced audio clip configuration that encapsulates additional parameters for playback
    /// such as volume, spatial blend, pitch, routing, and priority. This class is used for customizing audio
    /// playback behavior in a more detailed and controlled manner.
    /// </summary>
    [Serializable]
    public class AdvancedAudioClip
    {
        [SerializeField, Tooltip("The audio clip to play.")]
        private AudioClip _clip;

        [SerializeField, Tooltip("The individual volume of the audio clip.")]
        private float _volume = 1f;

        [SerializeField, Tooltip("0 = fully 2D, 1 = fully 3D spatial audio.")]
        private float _spatialBlend = 0f;

        [SerializeField, Tooltip("The pitch of the audio clip.")]
        private float _rollOff = 1f;

        [SerializeField, Tooltip("Which AudioMixer group this clip routes to.")]
        private AudioGroup _group = AudioGroup.Sfx;

        [SerializeField, Tooltip("Higher priority sounds steal pool slots from lower priority sounds when the pool is full.")]
        private AudioPriority _priority = AudioPriority.Sfx;


        /// <summary>
        /// Gets the audio clip to be played.
        /// </summary>
        public AudioClip Clip => _clip;

        /// <summary>
        /// Gets the individual volume of the audio clip.
        /// </summary>
        public float Volume => _volume;

        /// <summary>
        /// Gets the spatial blend of the audio clip, determining the balance between 2D and 3D sound.
        /// A value of 0 represents fully 2D audio, while a value of 1 represents fully 3D spatialized audio.
        /// </summary>
        public float SpatialBlend => _spatialBlend;

        /// <summary>
        /// Gets the roll-off factor determining how the audio volume decreases
        /// over distance in 3D space.
        /// </summary>
        public float RollOff => _rollOff;

        /// <summary>
        /// Gets the associated audio mixer group for the audio clip.
        /// </summary>
        public AudioGroup Group => _group;

        /// <summary>
        /// Gets the priority level assigned to this audio clip.
        /// Higher priority clips will override lower priority ones in cases where playback resources are limited.
        /// </summary>
        public AudioPriority Priority => _priority;
    }
}
