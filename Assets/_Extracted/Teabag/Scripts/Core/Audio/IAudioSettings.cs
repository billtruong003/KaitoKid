using Squido.JungleXRKit.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace Teabag.Core
{
    /// <summary>
    /// Defines an interface for configuring and managing audio settings,
    /// including audio mixer groups for different audio categories such as music, sound effects, voice, and ambient sounds.
    /// </summary>
    public interface IAudioSettings : ISettings
    {
        /// <summary>
        /// Gets the prefab used to instantiate the AudioManager component.
        /// </summary>
        GameObject AudioManagerPrefab { get; }

        /// <summary>
        /// Gets the master audio mixer used to control overall audio settings.
        /// This property provides access to the main audio mixer that governs global
        /// audio levels and effects across all audio channels in the application.
        /// </summary>
        AudioMixer MasterMixer { get; }

        /// <summary>
        /// Gets the audio mixer group used for managing music audio settings.
        /// This property provides access to the mixer group that controls
        /// the audio levels for music in the application.
        /// </summary>
        AudioMixerGroup MusicMixerGroup { get; }

        /// <summary>
        /// Gets the audio mixer group dedicated to sound effects (SFX).
        /// This property provides access to the mixer group that controls
        /// the audio levels for sound effects in the application.
        /// </summary>
        AudioMixerGroup SfxMixerGroup { get; }

        /// <summary>
        /// Gets the audio mixer group dedicated to handling voice-related audio settings.
        /// This property provides access to the mixer group that controls
        /// the audio levels for voice-related audio in the application.
        /// </summary>
        AudioMixerGroup VoiceMixerGroup { get; }

        /// <summary>
        /// Gets the audio mixer group designated for ambient sounds and environmental audio.
        /// This property provides access to the mixer group that controls
        /// the audio levels for ambient in the application.
        /// </summary>
        AudioMixerGroup AmbientMixerGroup { get; }

        /// <summary>
        /// Gets the audio mixer group specifically used for managing UI-related audio settings.
        /// This property provides access to the mixer group that controls
        /// the audio levels for ui-related audio in the application.
        /// </summary>
        AudioMixerGroup UiMixerGroup { get; }

        /// <summary>
        /// Retrieves the AudioMixerGroup associated with the specified AudioGroup category.
        /// </summary>
        /// <param name="group">The AudioGroup category for which the AudioMixerGroup is requested.
        /// Possible values: Master, Music, Sfx, Voice, Ambient, UI.</param>
        /// <returns>The corresponding AudioMixerGroup if the AudioGroup exists; otherwise, null.</returns>
        AudioMixerGroup GetMixerGroup(AudioGroup group);
    }
}
