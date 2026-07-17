using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _moneyText;
    [SerializeField] private TextMeshProUGUI _gameOverScoreText;
    [SerializeField] private TextMeshProUGUI _GameOverMoneyText;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void UpdateScore(int score)
    {
        _scoreText.text = score.ToString();
    }
    public void AppearGameOverPanel()
    {
        _GameOverMoneyText.text = "50";
        _gameOverScoreText.text = _scoreText.text;
        gameOverPanel.SetActive(true);
    }
}
