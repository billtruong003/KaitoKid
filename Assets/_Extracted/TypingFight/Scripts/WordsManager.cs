using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using TMPro;
using UnityEngine.Networking;
using System.IO;

public class WordsManager : MonoBehaviour
{
    [HideInInspector] public bool IsTyping { get; private set; }

    [SerializeField] GameObject typingPanel;
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] GameObject player;
    [SerializeField] GameObject enemy;

    public List<Word> words;
    private Word activeWord;
    private static string[] wordList;
    private int activeIndex;
    private int charTyped;
    private int timesFailed;
    private float _frequency;

    private AudioManager _audioManager;
    private GameController _gameController;
    private CharacterBehaviour _playerBehaviour;
    private CharacterBehaviour _enemyBehaviour;

    private static readonly string[] complexities = { "EasyWords.txt", "MediumWords.txt", "HardWords.txt" };
    private static readonly float[] frequencyTimes = { 30f, 20f, 10f, 3f };

    // Start is called before the first frame update
    void Start()
    {
        _gameController = FindObjectOfType<GameController>();
        _audioManager = GameObject.FindWithTag("Audio").GetComponent<AudioManager>();

        _frequency = frequencyTimes[PlayerPrefs.GetInt("Attacks Frequency")];

        _playerBehaviour = player.GetComponentInChildren<CharacterBehaviour>();
        _enemyBehaviour = enemy.GetComponentInChildren<CharacterBehaviour>();

        IsTyping = false;
        StartCoroutine(GetWordsfromTXT());
    }

    internal void StartDisplayingWords()
    {
        // Restart letter typing counters
        RestartTypingCounters();

        // Clean current list
        CleanList();

        // Activate typing panel
        typingPanel.SetActive(true);

        IsTyping = true;

        // Add random words to word list
        for (int i = 0; i < 50; i++)
        {
            AddWord();
        }

        // Set Active Word
        activeWord = words[activeIndex = 0];

        // Display Word
        DisplayWord(activeWord);

        // Start enemy timed attacks
        InvokeRepeating("EnemyAttack", _frequency, _frequency);
    }

    private IEnumerator GetWordsfromTXT()
    {
        int complexity = PlayerPrefs.GetInt("Words Complexity", 0);
        string fileName = complexities[complexity];
        
        // Đường dẫn này trên Web sẽ là http://localhost:3000/webgl-games/typing-fight/StreamingAssets/TextFiles/...
        string filepath = System.IO.Path.Combine(Application.streamingAssetsPath, "TextFiles", fileName);

        #if UNITY_WEBGL && !UNITY_EDITOR
            // Dùng WebRequest để "tải" file về từ Server
            using (UnityWebRequest www = UnityWebRequest.Get(filepath))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Lỗi tải file: " + www.error);
                }
                else
                {
                    string content = www.downloadHandler.text;
                    wordList = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    Debug.Log("WebGL: Đã tải " + wordList.Length + " từ.");
                }
            }
        #else
            // Chạy trên Windows/Editor thì dùng File trực tiếp
            if (System.IO.File.Exists(filepath))
            {
                wordList = System.IO.File.ReadAllLines(filepath);
                Debug.Log("Windows: Đã đọc " + wordList.Length + " từ.");
            }
            yield return null;
        #endif
    }
    // Add new random words to the game list
    private void AddWord()
    {
        Word word = new Word(GetRandomWord());
        words.Add(word);
    }

    // Get random word from the wordList
    private static string GetRandomWord()
    {
        int randomIndex = Random.Range(0, wordList.Length);
        string randomWord = wordList[randomIndex];

        return randomWord;
    }

    // Clean current list
    private void CleanList()
    {
        words.Clear();
    }

    // Check if typed letter is part of the active word
    public void TypeLetter(char letter)
    {
        //Check if letter was next
        if (activeWord.GetNextLetter() == letter)
        {
            charTyped++;
            MarkLetter(letter);
            activeWord.TypedLetter();
            _audioManager.PlaySound("KeyPress");
        }
        else
        {
            charTyped++;
            timesFailed++;
            _audioManager.PlaySound("WrongPress");
            EnemyAttack();
        }

        // Check if word is fully typed
        if (activeWord.WordCompleted())
        {
            // TODO : play "ding" sound
            _playerBehaviour.Animate("attack");
            StartCoroutine(DelayedAnimation(0.5f, "enemy"));
            SetActiveWord();
            DisplayWord(activeWord);
        }
    }

    public void SetActiveWord()
    {
        activeIndex++;
        activeWord = activeIndex < words.Count ? words[activeIndex] : null;
    }

    public void DisplayWord(Word w)
    {
        if (IsTyping) text.text = w.word;
    }

    public void MarkLetter(char letter)
    {
        int markIndex = activeWord.GetIndex() != 0 ? text.text.LastIndexOf('>') + 1 : 0;

        string substrA = text.text.Substring(0, markIndex);
        string substrB = text.text.Substring(markIndex, 1);
        string substrC = text.text.Substring(markIndex + 1);

        text.text = substrA + "<color=#ffffff>" + substrB + "</color>" + substrC;
    }

    public bool GetTyping()
    {
        return IsTyping;
    }

    private void EnemyAttack()
    {
        _enemyBehaviour.Animate("attack");
        StartCoroutine(DelayedAnimation(0.5f, "player"));
    }

    private void RestartTypingCounters()
    {
        charTyped = 0;
        timesFailed = 0;
    }

    public void RestartPlayerAnimations()
    {
        // Restart Animations
        _playerBehaviour.RestartAnimation();
        _enemyBehaviour.RestartAnimation();

        // Restart Health Bars
        _playerBehaviour.RestartHealthBar();
        _enemyBehaviour.RestartHealthBar();
    }

    public void StopWordsSpawning(string whoWon)
    {
        // Stop player from continuing to type
        IsTyping = false;
        text.text = "";
        typingPanel.SetActive(false);

        // Stop Stopwatch
        _gameController.StopStopWatch();

        // Stop Enemy timed attacks
        CancelInvoke("EnemyAttack");

        // Show End Game Screen
        _gameController.ShowGameResults(whoWon, charTyped, timesFailed);

    }

    // Creates a co-routine to delay an action
    private IEnumerator DelayedAnimation(float time, string fighter)
    {
        yield return new WaitForSeconds(time);

        if (fighter == "enemy")
        {
            _enemyBehaviour.TakeDamage();
        }
        else if (fighter == "player")
        {
            _playerBehaviour.TakeDamage();
        }
    }
}




