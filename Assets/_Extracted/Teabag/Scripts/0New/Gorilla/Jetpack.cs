using System;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;
using IAudioService = Teabag.Core.IAudioService;

public class Jetpack : NetworkBehaviour
{
    [SerializeField] private AdvancedAudioClip sfx;
    [SerializeField] private ParticleSystem particle;
    private IAudioService _audioService;

    [Networked, OnChangedRender(nameof(OnJetpackEnable))]
    public NetworkBool IsJetpackEnabled { get; set; }

    public override void Spawned()
    {
        base.Spawned();
        _audioService = ServiceLocator.Get<IAudioService>();
        IsJetpackEnabled = false;
    }

    private void Start()
    {
        IsJetpackEnabled = false;
    }

    private void OnJetpackEnable()
    {
        if (IsJetpackEnabled)
        {
            if (sfx != null && _audioService != null)
                _audioService.Play(sfx, true);

            if(particle != null)
                particle.Play();
        }
        else
        {
            if (sfx != null && _audioService != null)
                _audioService.Stop(sfx.Clip.name);

            if (particle != null)
                particle.Stop();
        }
    }
}




