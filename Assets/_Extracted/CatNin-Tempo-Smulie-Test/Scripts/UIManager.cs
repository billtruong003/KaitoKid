using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private TextMeshProUGUI word;
    [SerializeField] private TextMeshProUGUI score;
    [SerializeField] private List<GameObject> Heart;
    [SerializeField] private GameObject GameOverPanel;
    [SerializeField] private WordSO wordList;
    [SerializeField] private GameObject pausePanel;
    public string CurrentWord;
    public int TimeGen;
    public int Score;
    public int Life;
    private bool isPaused = false;


    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void Start()
    {
        InitWord();
        Score = 0;
        score.text = Score.ToString();
        Life = 3;
    }
    public void InitWord()
    {
        CurrentWord = GetWord();
        word.text = CurrentWord;
    }
    public void BackToMenu()
    {
        SceneLoad.Instance.LoadMenuScene();
    }
    public void ReloadScene()
    {
        SceneLoad.Instance.ReloadScene();
    }
    public string GetWord()
    {
        return wordList.words[Random.Range(0, 3)].word;
    }
    public void PlusScore()
    {
        Score += 1;
        score.text = Score.ToString();
    }
    public void MinusLife()
    {
        Life -= 1;
        Heart[Life].SetActive(false);
    }
    public void GameOver()
    {
        Debug.Log("GameOver");
        GameOverPanel.SetActive(true);
        Time.timeScale = 0;
    }
    public void Pause()
    {
        isPaused = true;


        if (isPaused)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
    public void Continue()
    {
        isPaused = false;


        if (isPaused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }


}
