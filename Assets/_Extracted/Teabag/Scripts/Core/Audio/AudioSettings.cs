using System;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace Teabag.Core
{
    /// <summary>
    /// Encapsulates configurations for audio settings, including references to the master audio mixer
    /// and individual mixer groups for various audio categories such as music, sound effects, voice,
    /// ambient sounds, and user interface audio.
    /// </summary>
    /// <remarks>
    /// This class provides access to audio mixers and mixer groups, allowing for the management
    /// of audio levels and grouping within the application. It implements the <see cref="IAudioSettings"/> interface.
    /// </remarks>
    [Serializable]
    public class AudioSettings : IAudioSettings
    {
        [SerializeField]
        private AudioManagerInstance _audioManagerInstance;

        [SerializeField]
        private AudioMixer _masterMixer;

        [SerializeField]
        private AudioMixerGroup _musicMixerGroup;

        [SerializeField]
        private AudioMixerGroup _sfxMixerGroup;

        [SerializeField]
        private AudioMixerGroup _voiceMixerGroup;

        [SerializeField]
        private AudioMixerGroup _ambientMixerGroup;

        [SerializeField]
        private AudioMixerGroup _uiMixerGroup;


        /// <inheritdoc/>
        public GameObject AudioManagerPrefab => _audioManagerInstance.gameObject;

        /// <inheritdoc/>
        public AudioMixer MasterMixer => _masterMixer;

        /// <inheritdoc/>
        public AudioMixerGroup MusicMixerGroup => _musicMixerGroup;

        /// <inheritdoc/>
        public AudioMixerGroup SfxMixerGroup => _sfxMixerGroup;

        /// <inheritdoc/>
        public AudioMixerGroup VoiceMixerGroup => _voiceMixerGroup;

        /// <inheritdoc/>
        public AudioMixerGroup AmbientMixerGroup => _ambientMixerGroup;

        /// <inheritdoc/>
        public AudioMixerGroup UiMixerGroup => _uiMixerGroup;


        /// <summary>
        /// Retrieves the specified audio mixer group based on the provided audio group enumeration.
        /// </summary>
        /// <param name="group">The audio group for which the corresponding mixer group should be retrieved.
        /// This value determines which mixer group (e.g., Master, Music, SFX, Voice, Ambient, UI) is returned.</param>
        /// <returns>The <see cref="AudioMixerGroup"/> associated with the specified <paramref name="group"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the provided <paramref name="group"/>
        /// does not match any recognized audio group.</exception>
        public AudioMixerGroup GetMixerGroup(AudioGroup group) => group switch
        {
            AudioGroup.Master => MasterMixer.outputAudioMixerGroup,
            AudioGroup.Music => MusicMixerGroup,
            AudioGroup.Sfx => SfxMixerGroup,
            AudioGroup.Voice => VoiceMixerGroup,
            AudioGroup.Ambient => AmbientMixerGroup,
            AudioGroup.UI => UiMixerGroup,
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
        };

#if UNITY_EDITOR
        private const string AudioSettingsHeader = "Audio Settings";
        private const string AudioSettingsDescription = "Provides configuration settings for audio playback within the application.";
        private const string AudioManagerPrefabLabel = "Audio Manager Prefab";
        private const string AudioManagerPrefabDescription = "Specifies the prefab used to instantiate the AudioManager instance.";
        private const string MasterMixerLabel = "Master Mixer";
        private const string MasterMixerDescription = "Specifies the master audio mixer used to control overall audio settings.";
        private const string MusicMixerGroupLabel = "Music Mixer Group";
        private const string MusicMixerGroupDescription = "Specifies the mixer group used for music playback.";
        private const string SfxMixerGroupLabel = "SFX Mixer Group";
        private const string SfxMixerGroupDescription = "Specifies the mixer group used for sound effects playback.";
        private const string VoiceMixerGroupLabel = "Voice Mixer Group";
        private const string VoiceMixerGroupDescription = "Specifies the mixer group used for voice playback.";
        private const string AmbientMixerGroupLabel = "Ambient Mixer Group";
        private const string AmbientMixerGroupDescription = "Specifies the mixer group used for ambient sounds playback.";
        private const string UiMixerGroupLabel = "UI Mixer Group";
        private const string UiMixerGroupDescription = "Specifies the mixer group used for UI audio playback.";


        private static readonly GUIContent audioSettingsHeaderGUIContent = new (AudioSettingsHeader);
        private static readonly GUIContent audioSettingsDescriptionGUIContent = new (AudioSettingsDescription);
        private static readonly GUIContent audioManagerPrefabGUIContent = new (AudioManagerPrefabLabel, AudioManagerPrefabDescription);
        private static readonly GUIContent masterMixerGUIContent = new (MasterMixerLabel, MasterMixerDescription);
        private static readonly GUIContent musicMixerGroupGUIContent = new (MusicMixerGroupLabel, MusicMixerGroupDescription);
        private static readonly GUIContent sfxMixerGroupGUIContent = new (SfxMixerGroupLabel, SfxMixerGroupDescription);
        private static readonly GUIContent voiceMixerGroupGUIContent = new (VoiceMixerGroupLabel, VoiceMixerGroupDescription);
        private static readonly GUIContent ambientMixerGroupGUIContent = new (AmbientMixerGroupLabel, AmbientMixerGroupDescription);
        private static readonly GUIContent uiMixerGroupGUIContent = new (UiMixerGroupLabel, UiMixerGroupDescription);


        /// <inheritdoc/>
        public void Editor_OnGUI(Object target)
        {
            using (new UnityEditor.EditorGUILayout.VerticalScope())
            {
                UnityEditor.EditorGUILayout.LabelField(audioSettingsHeaderGUIContent, UnityEditor.EditorStyles.boldLabel);
                UnityEditor.EditorGUILayout.LabelField(audioSettingsDescriptionGUIContent, UnityEditor.EditorStyles.wordWrappedLabel);
                UnityEditor.EditorGUILayout.Space();
            }

            var serializedObject = new UnityEditor.SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();

            var audioManagerInstanceSerializedProperty = serializedObject.FindProperty(nameof(AudioSettingsAsset._settings)).FindPropertyRelative(nameof(_audioManagerInstance));
            UnityEditor.EditorGUILayout.PropertyField(audioManagerInstanceSerializedProperty, audioManagerPrefabGUIContent);

            var masterMixerSerializedProperty = serializedObject.FindProperty(nameof(AudioSettingsAsset._settings)).FindPropertyRelative(nameof(_masterMixer));
            UnityEditor.EditorGUILayout.PropertyField(masterMixerSerializedProperty, masterMixerGUIContent);

            var musicMixerGroupSerializedProperty = serializedObject.FindProperty(nameof(AudioSettingsAsset._settings)).FindPropertyRelative(nameof(_musicMixerGroup));
            UnityEditor.EditorGUILayout.PropertyField(musicMixerGroupSerializedProperty, musicMixerGroupGUIContent);

            var sfxMixerGroupSerializedProperty = serializedObject.FindProperty(nameof(AudioSettingsAsset._settings)).FindPropertyRelative(nameof(_sfxMixerGroup));
            UnityEditor.EditorGUILayout.PropertyField(sfxMixerGroupSerializedProperty, sfxMixerGroupGUIContent);

            var voiceMixerGroupSerializedProperty = serializedObject.FindProperty(nameof(AudioSettingsAsset._settings)).FindPropertyRelative(nameof(_voiceMixerGroup));
            UnityEditor.EditorGUILayout.PropertyField(voiceMixerGroupSerializedProperty, voiceMixerGroupGUIContent);

            var ambientMixerGroupSerializedProperty = serializedObject.FindProperty(nameof(AudioSettingsAsset._settings)).FindPropertyRelative(nameof(_ambientMixerGroup));
            UnityEditor.EditorGUILayout.PropertyField(ambientMixerGroupSerializedProperty, ambientMixerGroupGUIContent);

            var uiMixerGroupSerializedProperty = serializedObject.FindProperty(nameof(AudioSettingsAsset._settings)).FindPropertyRelative(nameof(_uiMixerGroup));
            UnityEditor.EditorGUILayout.PropertyField(uiMixerGroupSerializedProperty, uiMixerGroupGUIContent);

            serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
