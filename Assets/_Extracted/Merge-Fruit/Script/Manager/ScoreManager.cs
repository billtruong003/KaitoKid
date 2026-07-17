using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private int score;
    [SerializeField] private UIManager _uiManager;
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
    private void Start()
    {
        score = 0;
        PlusScore(0);
    }
    public void PlusScore(int addScore)
    {
        score += addScore;
        UIManager.Instance.UpdateScore(score);
    }


}
