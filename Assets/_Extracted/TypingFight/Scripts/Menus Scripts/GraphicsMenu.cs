using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphicsMenu : MonoBehaviour
{
    public Image title;

    public TMP_Dropdown resolutionsDD;
    public TMP_Dropdown windowDD;
    public TMP_Dropdown qualityDD;
    public Toggle vSyncTgg;

    private Resolution[] resolutions;
    private int defaultValue;
    
    private void Start()
    {
        FindObjectOfType<Tweening>().TitlePulsate(title);

        ReadyResolutions();

        ResetPlayerPrefs();
    }

    /*** ---- Graphics Menu ---- ***/

    public void ReadyResolutions()
    {
        resolutions = Screen.resolutions;
        resolutionsDD.ClearOptions();

        List<string> options = new List<string>();

        int currRes = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currRes = i;
            }
        }

        defaultValue = currRes;
        resolutionsDD.AddOptions(options);
        resolutionsDD.value = currRes;
        resolutionsDD.RefreshShownValue();
    }

    public void SetResolution(int res)
    {
        Resolution resolution = resolutions[res];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetWindowMode(int mode)
    {
        Screen.fullScreen = (mode == 0) ? true : false;
    }

    public void SetQuality (int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetVsync(bool vsync)
    {
        QualitySettings.vSyncCount = vsync ? 1 : 0;
    }

    /*** ------------ Actions ------------ ***/

    private void ResetPlayerPrefs()
    {
        // Resolution
        SetResolution(PlayerPrefs.GetInt("Resolution", defaultValue));
        resolutionsDD.value = PlayerPrefs.GetInt("Resolution", defaultValue);
        resolutionsDD.RefreshShownValue();

        // Window Mode
        SetWindowMode(PlayerPrefs.GetInt("Window Mode", 0));
        windowDD.value = PlayerPrefs.GetInt("Window Mode", 0);
        windowDD.RefreshShownValue();

        // Graphic Quality
        SetQuality(PlayerPrefs.GetInt("Graphic Quality", 0));
        qualityDD.value = PlayerPrefs.GetInt("Graphic Quality", 0);
        qualityDD.RefreshShownValue();

        // Vsync
        vSyncTgg.isOn = PlayerPrefs.GetInt("Vsync") == 1 ? true : false;
        SetVsync(vSyncTgg.isOn);
    }

    public void Return()
    {
        // Reset all selections
        ResetPlayerPrefs();

        FindObjectOfType<OptionsMenuController>().ShowMenus(2, false);
    }

    public void OK()
    {
        // Save player preferences
        PlayerPrefs.SetInt("Resolution", resolutionsDD.value);
        PlayerPrefs.SetInt("Window Mode", windowDD.value);
        PlayerPrefs.SetInt("Graphic Quality", qualityDD.value);
        PlayerPrefs.SetInt("Vsync", vSyncTgg.isOn ? 1 : 0);

        FindObjectOfType<OptionsMenuController>().ShowMenus(2, false);
    }
}
