using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace Teabag.Core
{
    /// <summary>
    /// Provides spatial audio playback capabilities.
    /// Adapter interface over the AudioManager static class.
    /// </summary>
    public interface IAudioService : IService
    {
        /// <summary>
        /// Retrieves the audio mixer group corresponding to the specified audio group.
        /// </summary>
        /// <param name="group">The audio group for which to retrieve the mixer group.</param>
        /// <returns>An AudioMixerGroup associated with the specified audio group.</returns>
        AudioMixerGroup GetMixerGroup(AudioGroup group);

        /// <summary>
        /// Sets the volume for the specified audio group using a normalized value.
        /// </summary>
        /// <param name="group">The audio group whose volume is to be adjusted.</param>
        /// <param name="normalised">The normalized volume level, where 0 is silence and 1 is the maximum volume.</param>
        void SetGroupVolume(AudioGroup group, float normalised);

        /// <summary>
        /// Retrieves the current normalized volume level for the specified audio group.
        /// </summary>
        /// <param name="group">The audio group whose volume level is to be retrieved.</param>
        /// <returns>A float representing the normalized volume level of the specified audio group, where 1 represents full volume and 0 represents silence.</returns>
        float GetGroupVolume(AudioGroup group);

        /// <summary>
        /// Plays the specified audio clip.
        /// </summary>
        /// <param name="clip">The audio clip to be played.</param>
        void Play(AudioClip clip);

        /// <summary>
        /// Plays the specified audio clip.
        /// </summary>
        /// <param name="clip">The audio clip to be played.</param>
        /// <param name="volume">The volume at which the clip will be played.</param>
        void Play(AudioClip clip, float volume);

        /// <summary>
        /// Plays the specified audio clip at the given position.
        /// </summary>
        /// <param name="clip">The audio clip to be played.</param>
        /// <param name="position">The 3D position where the audio should be played.</param>
        void Play(AudioClip clip, Vector3 position);

        /// <summary>
        /// Plays the specified audio clip at the given volume and position.
        /// </summary>
        /// <param name="clip">The audio clip to be played.</param>
        /// <param name="volume">The volume level at which the audio clip should be played.</param>
        /// <param name="position">The position in the 3D space from which the audio should be emitted.</param>
        void Play(AudioClip clip, float volume, Vector3 position);

        /// <summary>
        /// Plays an audio clip at the specified volume, position, and spatial blend.
        /// </summary>
        /// <param name="clip">The audio clip to be played.</param>
        /// <param name="volume">The volume level at which the clip should be played, normalized between 0 and 1.</param>
        /// <param name="position">The 3D position in the world where the audio should be played.</param>
        /// <param name="spatialBlend">
        /// Determines how much the audio should be spatially blended:
        /// 0 represents fully 2D audio, and 1 represents fully 3D audio.
        /// </param>
        void Play(AudioClip clip, float volume, Vector3 position, float spatialBlend);

        /// <summary>
        /// Plays a random audio clip from the provided list at the specified position with the given volume.
        /// </summary>
        /// <param name="clips">A read-only list of audio clips to choose from.</param>
        /// <param name="volume">The volume level to play the selected audio clip at, ranging from 0.0 to 1.0.</param>
        /// <param name="position">The 3D world position where the audio clip will be played.</param>
        void Play(IReadOnlyList<AudioClip> clips, float volume, Vector3 position);

        /// <summary>
        /// Plays an audio clip from a collection of audio clips at the specified volume, position, and spatial blend.
        /// </summary>
        /// <param name="clips">A read-only list of audio clips to choose from.</param>
        /// <param name="volume">The volume level at which the audio clip will be played.</param>
        /// <param name="position">The 3D position where the audio clip will be played.</param>
        /// <param name="spatialBlend">The spatial blend value determining the balance between 3D and 2D audio rendering.</param>
        void Play(IReadOnlyList<AudioClip> clips, float volume, Vector3 position, float spatialBlend);

        /// <summary>
        /// Plays the specified advanced audio clip using its predefined settings.
        /// </summary>
        /// <param name="clip">The advanced audio clip to be played, containing specific playback configuration such as volume, spatial blend, routing, and priority.</param>
        void Play(AdvancedAudioClip clip);

        /// <summary>
        /// Plays the specified advanced audio clip with the option to loop the playback.
        /// </summary>
        /// <param name="clip">The advanced audio clip to be played, encapsulating various playback configurations.</param>
        /// <param name="loop">Indicates whether the clip should loop continuously during playback.</param>
        void Play(AdvancedAudioClip clip, bool loop);

        /// <summary>
        /// Plays the specified audio clip at the given volume, pitch and position.
        /// </summary>
        /// <param name="clip">The audio clip to be played.</param>
        /// <param name="volume">The volume level at which the audio clip should be played.</param>
        /// <param name="pitch">The pitch at which to play the audio clip.</param>
        /// <param name="position">The position in the 3D space from which the audio should be emitted.</param>
        void Play(AdvancedAudioClip clip, float volume, float pitch, Vector3 position);

        /// <summary>
        /// Plays the specified advanced audio clip at the given position.
        /// </summary>
        /// <param name="clip">The advanced audio clip containing detailed playback parameters.</param>
        /// <param name="position">The world position where the audio should be played.</param>
        void Play(AdvancedAudioClip clip, Vector3 position);

        /// <summary>
        /// Plays the specified advanced audio clip with the provided position and pitch.
        /// </summary>
        /// <param name="clip">The advanced audio clip to be played.</param>
        /// <param name="position">The world position where the audio should be played.</param>
        /// <param name="pitch">The pitch at which to play the audio clip.</param>
        void Play(AdvancedAudioClip clip, Vector3 position, float pitch);

        /// <summary>
        /// Plays a collection of advanced audio clips at the specified position.
        /// </summary>
        /// <param name="clips">The collection of advanced audio clips to be played.</param>
        /// <param name="position">The position in 3D space where the audio should be played.</param>
        void Play(IReadOnlyList<AdvancedAudioClip> clips, Vector3 position);

        /// <summary>
        /// Plays a list of advanced audio clips at the specified position with the given pitch.
        /// </summary>
        /// <param name="clips">A read-only list of advanced audio clips to be played.</param>
        /// <param name="position">The 3D position where the audio clips will be played.</param>
        /// <param name="pitch">The pitch adjustment to be applied to the audio clips.</param>
        void Play(IReadOnlyList<AdvancedAudioClip> clips, Vector3 position, float pitch);

        /// <summary>
        /// Crossfades the currently playing music to a new audio track over a specified duration.
        /// </summary>
        /// <param name="clip">The new audio clip to be played as music.</param>
        /// <param name="fadeTime">The duration, in seconds, over which to crossfade to the new music track. Defaults to 1 second if not specified.</param>
        void CrossfadeMusic(AudioClip clip, float fadeTime = 1f);

        /// <summary>
        /// Stops the playback of an audio clip with the specified name.
        /// </summary>
        /// <param name="clipName">The name of the audio clip to stop.</param>
        void Stop(string clipName);

        /// <summary>
        /// Stops the currently playing music with an optional fade-out effect.
        /// </summary>
        /// <param name="fadeTime">The duration of the fade-out effect, in seconds. Defaults to 1 second.</param>
        void StopMusic(float fadeTime = 1f);

        /// <summary>
        /// Stops the playback of all currently playing audio clips.
        /// </summary>
        void StopAll();
    }
}
