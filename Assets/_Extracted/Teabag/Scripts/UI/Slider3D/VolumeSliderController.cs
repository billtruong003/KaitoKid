using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public class VolumeSliderController : Slider3DController
{
    [SerializeField] VolumeMixerType mixer;
    AudioMixerGroup _mixerGroup;
    string _volumeParameter;
    protected override void Awake()
    {
        base.Awake();
        _mixerGroup = GetMixerGroup();
        switch (mixer)
        {
            case VolumeMixerType.Music:
                {
                    _volumeParameter = "MusicVol";
                    break;
                }
            case VolumeMixerType.Sfx:
                {
                    _volumeParameter = "SFXVol";
                    break;
                }
            case VolumeMixerType.Voice:
                {
                    _volumeParameter = "VoiceVol";
                    break;
                }
        }
        UpdateSlider(Amount);
    }

    public override void UpdateSlider(float amount)
    {
        if (_mixerGroup == null)
        {
            return;
        }
        progress.text = (amount * 100).ToString();
        amount = Mathf.Clamp(amount, 0.0001f, 1f);
        _mixerGroup.audioMixer.SetFloat(_volumeParameter, Mathf.Log10(amount) * 20);
    }

    private AudioMixerGroup GetMixerGroup()
    {
        switch (mixer)
        {
            case VolumeMixerType.Music:
                {
                    return AudioSettingsAsset.InstanceAsset.Settings.MusicMixerGroup;
                }
            case VolumeMixerType.Sfx:
                {
                    return AudioSettingsAsset.InstanceAsset.Settings.SfxMixerGroup;
                }
            case VolumeMixerType.Voice:
                {
                    return AudioSettingsAsset.InstanceAsset.Settings.VoiceMixerGroup;
                }
            default:
                {
                    return null;
                }
        }
    }
}

public enum VolumeMixerType
{
    Music,
    Sfx,
    Voice
}
