using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject Play;
    [SerializeField] private GameObject Practice;
    [SerializeField] private GameObject Exit;
    [SerializeField] private List<GameObject> PlayButton;
    [SerializeField] private List<GameObject> PracticeButton;
    public void GamePlayLoad(int num)
    {
        SoundManager.Instance.PlayMusic(num);
        SceneLoad.Instance.LoadGameplayScene();
    }
    public void GamePracticeLoad(int num)
    {
        SoundManager.Instance.PlayMusic(num);
        SceneLoad.Instance.LoadPracticeScene();
    }
    public void InitButtonGamePlay()
    {
        SetFalseButton();
        for (int i = 0; i < PlayButton.Count; i++)
        {
            PlayButton[i].SetActive(true);
        }
    }
    public void InitButtonPractice()
    {
        SetFalseButton();
        for (int i = 0; i < PracticeButton.Count; i++)
        {
            PracticeButton[i].SetActive(true);
        }
    }
    public void SetFalseButton()
    {
        Play.SetActive(false);
        Practice.SetActive(false);
        Exit.SetActive(false);
    }
    public void MoveToTutoScene()
    {
        SceneLoad.Instance.LoadTutoScene();
    }
    public void QuitGame()
    {
        Application.Quit();
    }

}
