using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayMenu : MonoBehaviour
{
    public Image title;

    public TMP_Dropdown wordsComplxDD;
    public TMP_Dropdown damageDD;
    public TMP_Dropdown frequencyDD;

    private int defaultValue = 0;

    // Start is called before the first frame update
    void Start()
    {
        FindObjectOfType<Tweening>().TitlePulsate(title);
        ResetPlayerPrefs();
    }

    /*** -------- Actions -------- ***/

    private void ResetPlayerPrefs()
    {
        // Words Complexity
        wordsComplxDD.value = PlayerPrefs.GetInt("Words Complexity", defaultValue);

        // Attacks DMG
        damageDD.value = PlayerPrefs.GetInt("Enemy DMG", defaultValue);

        // Attacks Frequency
        frequencyDD.value = PlayerPrefs.GetInt("Attacks Frequency", defaultValue);
    }

    public void Return()
    {
        // Reset all selections
        ResetPlayerPrefs();

        FindObjectOfType<OptionsMenuController>().ShowMenus(1, false);
    }

    public void OK()
    {
        // TODO: Save player preferences
        PlayerPrefs.SetInt("Words Complexity", wordsComplxDD.value);
        PlayerPrefs.SetInt("Enemy DMG", damageDD.value);
        PlayerPrefs.SetInt("Attacks Frequency", frequencyDD.value);

        FindObjectOfType<OptionsMenuController>().ShowMenus(1, false);
    }
}
