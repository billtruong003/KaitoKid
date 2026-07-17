using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class StartMenuController : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Image title;

    private GameObject _audioPlayer;
    private AudioManager _audioManager;
    private AudioSource _audioSource;

    private float defaultValue = 0.5f;

    void Start()
    {
        _audioPlayer = GameObject.FindWithTag("Audio");
        _audioManager = _audioPlayer.GetComponent<AudioManager>();
        _audioSource = _audioPlayer.GetComponent<AudioSource>();

        // Initialise audio preferences 
        UpdateAudioPrefs();
        if (!_audioSource.isPlaying)
        {
            _audioManager.PlaySound("MainTheme");
        }


        // Animate title pulsating
        FindObjectOfType<Tweening>().TitlePulsate(title);
    }

    private void UpdateAudioPrefs()
    {
        audioMixer.SetFloat("masterVolume", Mathf.Log10(PlayerPrefs.GetFloat("Master Volume", defaultValue)) * 20);
        audioMixer.SetFloat("sfxVolume", Mathf.Log10(PlayerPrefs.GetFloat("SFX Volume", defaultValue)) * 20);
        audioMixer.SetFloat("musicVolume", Mathf.Log10(PlayerPrefs.GetFloat("Music Volume", defaultValue)) * 20);
    }

    public void PlayButton()
    {
        _audioManager.PauseSound("MainTheme");
        GetComponent<SceneLoader>().LoadScene(1);
    }

    public void OptionsButton()
    {
        GetComponent<SceneLoader>().LoadScene(2);
    }

    public void ExitButton()
    {
        Application.Quit();
    }
}