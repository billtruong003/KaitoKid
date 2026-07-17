using System;
using System.Collections;
using System.Collections.Generic;
using EasyUI.Modals;
using UnityEngine;
using TMPro;

public class GameController : MonoBehaviour
{
    [HideInInspector] public bool IsRunning { get; private set; }

    public TextMeshProUGUI stopwatchGUI;
    public TextMeshProUGUI countdownGUI;
    public Sprite win, lose;

    private float _stopwatch;
    private int _countdown;

    private GameObject _audioPlayer;
    private AudioManager _audioManager;
    private AudioSource _audioSource;
    private WordsManager _wordsManager;


    // Start is called before the first frame update
    void Start()
    {
        _audioPlayer = GameObject.FindWithTag("Audio");
        _audioManager = _audioPlayer.GetComponent<AudioManager>();
        _audioSource = _audioPlayer.GetComponent<AudioSource>();

        _wordsManager = FindObjectOfType<WordsManager>();

        _audioManager.PlaySound("BattleTheme");

        IsRunning = false;

        // Initialise StopWatch
        RestartStopWatch();

        //3, 2, 1 .... GO!!
        RestartCountdown();
    }

    private void StartGame()
    {
        IsRunning = true;
        _wordsManager.StartDisplayingWords();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsRunning) RunStopWatch();
    }

    public void ShowGameResults(string whoWon, int charTyped, int timesFailed)
    {
        string result = "Your speed was <color=#621D19><b>" + CalculateWPM(charTyped, _stopwatch) + " WPM</b></color> with <color=#621D19><b>" + CalculateAccuracy(charTyped, timesFailed) + "%</b></color> accuracy!";

        ModalUI.instance
            .SetTitle((whoWon == "Player") ? win : lose)
            .SetMessage(result)
            .OnPlayAgain(PlayAgain)
            .OnMainMenu(MainMenu)
            .ShowModal();
    }

    private void PlayAgain()
    {
        // Restart Player animations
        _wordsManager.RestartPlayerAnimations();

        // Restart StopWatch
        RestartStopWatch();

        //Restart Countdown
        RestartCountdown();
    }

    private void MainMenu()
    {
        _audioManager.PauseSound("BattleTheme");
        FindObjectOfType<SceneLoader>().LoadScene(0);
    }

    private int CalculateWPM(int charTyped, float time)
    {
        float WPM = (charTyped / 5) / (time / 60f);

        return (int)Math.Round(WPM);
    }

    private int CalculateAccuracy(int charTyped, int timesFailed)
    {
        float accuracy = 0f;
        int charCorrect = charTyped - timesFailed;

        if (charCorrect > 0) accuracy = (charCorrect / (float)charTyped) * 100f;

        return (int)Math.Round(accuracy);
    }

    /*** ---------- Time Related Methods ---------- ***/

    private void RunStopWatch()
    {
        _stopwatch += Time.deltaTime;
        stopwatchGUI.text = _stopwatch.ToString("#");
    }

    public void StopStopWatch()
    {
        IsRunning = false;
    }

    private void RestartStopWatch()
    {
        _stopwatch = 0f;
        stopwatchGUI.text = "0";
    }

    private void RestartCountdown()
    {
        _countdown = 3;
        StartCoroutine(CountDownStart());
    }

    private IEnumerator CountDownStart()
    {
        countdownGUI.gameObject.SetActive(true);
        _audioManager.PlaySound("Countdown");

        while (_countdown > 0)
        {
            countdownGUI.text = _countdown.ToString();
            yield return new WaitForSeconds(1f);
            _countdown--;
        }

        countdownGUI.text = "GO!";
        StartGame();

        yield return new WaitForSeconds(1f);
        countdownGUI.gameObject.SetActive(false);
    }
}
