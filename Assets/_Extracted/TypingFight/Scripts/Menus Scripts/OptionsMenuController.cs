using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuController : MonoBehaviour
{
    public Image title;
    public GameObject[] menuOptions;
    
    void Start()
    {
        FindObjectOfType<Tweening>().TitlePulsate(title);
    }

    public void ShowMenus(int menuIndex, bool hide)
    {
        menuOptions[menuIndex].gameObject.SetActive(hide);
        menuOptions[0].gameObject.SetActive(!hide);    
    }

    public void GameplayButton()
    {
        ShowMenus(1, true);
    }

    public void GraphicsButton()
    {
        ShowMenus(2, true);
    }

    public void AudioButton()
    {
        ShowMenus(3, true);
    }

    public void ReturnButton()
    {
        FindObjectOfType<SceneLoader>().LoadScene(0);
    }

}