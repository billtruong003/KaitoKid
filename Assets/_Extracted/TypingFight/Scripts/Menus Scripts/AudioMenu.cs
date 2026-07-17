using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioMenu : MonoBehaviour
{
    private float defaultValue = 0.5f;

    public AudioMixer audioMixer;
    public Image title;
    public Slider masterSlider;
    public Slider sfxSlider;
    public Slider musicSlider;

    // Start is called before the first frame update
    void Start()
    {
        FindObjectOfType<Tweening>().TitlePulsate(title);

        // Reset all selections
        ResetPlayerPrefs();
    }

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("masterVolume", Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("sfxVolume", Mathf.Log10(volume) * 20);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("musicVolume", Mathf.Log10(volume) * 20);
    }

    /*** -------- Actions -------- ***/

    private void ResetPlayerPrefs()
    {
        // Master Volume
        SetMasterVolume(PlayerPrefs.GetFloat("Master Volume", defaultValue));
        masterSlider.value = PlayerPrefs.GetFloat("Master Volume", defaultValue);

        // SFX Volume
        SetSFXVolume(PlayerPrefs.GetFloat("SFX Volume", defaultValue));
        sfxSlider.value = PlayerPrefs.GetFloat("SFX Volume", defaultValue);

        // Music Volume
        SetSFXVolume(PlayerPrefs.GetFloat("Music Volume", defaultValue));
        musicSlider.value = PlayerPrefs.GetFloat("Music Volume", defaultValue);  
    }

    public void Return()
    {
        // Reset all selections
        ResetPlayerPrefs();

        FindObjectOfType<OptionsMenuController>().ShowMenus(3, false);
    }

    public void OK()
    {
        // Save player preferences
        PlayerPrefs.SetFloat("Master Volume", masterSlider.value);
        PlayerPrefs.SetFloat("SFX Volume", sfxSlider.value);
        PlayerPrefs.SetFloat("Music Volume", musicSlider.value);

        FindObjectOfType<OptionsMenuController>().ShowMenus(3, false);
    }
}
