using System;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Core;
using IAudioService = Teabag.Core.IAudioService;

public class GorillaGameAudio : MonoBehaviour
{
    GorillaGameManager manager;

    public AudioSource backgroundMusic;
    public AudioSource countdownMusic;

    [Header("Countdown Clips")]
    public AdvancedAudioClip countdownClip;
    public AdvancedAudioClip goClip;

    [Header("Gameplay Clips")]
    public AdvancedAudioClip dieClip;
    public AdvancedAudioClip loseClip;
    public AdvancedAudioClip winClip;

    private IAudioService _audioService;

    int lastSecond;
    GameState lastGameState;

    private void Awake()
    {
        manager = GetComponentInParent<GorillaGameManager>();
        _audioService = ServiceLocator.Get<IAudioService>();
    }

    private void Update()
    {
        if (manager == null)
            manager = GorillaGameManager.instance;
    }

    private void FixedUpdate()
    {
        if (manager == null || manager.Object == null)
            return;

        switch (manager.gameState)
        {
            case GameState.Starting:
                HandleStartingAudio();
                break;
            case GameState.Running:
                HandleRunningAudio();
                break;
            case GameState.Ended:
                HandleEndedAudio();
                break;
        }

        lastGameState = manager.gameState;
    }

    private void HandleStartingAudio()
    {
        var span = manager.StartTime - SyncedTime.now;

        if (countdownMusic != null && !countdownMusic.isPlaying)
        {
            GameServices.BlimpButtonHandle?.Invoke(false);
            countdownMusic.Play();
        }

        if ((int)span.TotalSeconds > 3 || lastSecond == (int)span.TotalSeconds)
            return;

        if ((int)span.TotalSeconds > 0)
            _audioService.Play(countdownClip);
        else
            _audioService.Play(goClip);

        lastSecond = (int)span.TotalSeconds;
    }

    private void HandleRunningAudio()
    {
        if (countdownMusic != null)
            countdownMusic.Stop();

        if (backgroundMusic != null && !backgroundMusic.isPlaying)
            backgroundMusic.Play();
    }

    private void HandleEndedAudio()
    {
        if (countdownMusic != null)
            countdownMusic.Stop();
        if (backgroundMusic != null)
            backgroundMusic.Stop();

        if (lastGameState != manager.gameState)
        {
            GameServices.BlimpButtonHandle?.Invoke(true);
            var clip = manager.Won() ? winClip : loseClip;
            _audioService.Play(clip);
        }
    }
}
