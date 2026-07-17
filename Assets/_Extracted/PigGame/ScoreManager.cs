using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;
    [SerializeField] private TextMeshProUGUI correctRound;
    [SerializeField] private TextMeshProUGUI advice;
    [SerializeField] private TextMeshProUGUI roundCount;
    [SerializeField] private GameObject gameOverLabel;
    [SerializeField] private int round;
    [SerializeField] private int totalRound;
    [SerializeField] private int correctRoundCount;

    private void Awake()
    {
        instance = this;
    }
    private void OnDestroy()
    {
        instance = null;
    }
    // Start is called before the first frame update
    void Start()
    {
        RoundDecide();
    }

    public void MinusRound()
    {

        round -= 1;
        if (round >= 0)
            roundCount.text = round.ToString();

        CheckGameOver();
    }
    public void PlusCorrectRound()
    {
        correctRoundCount++;
    }
    public void RoundDecide()
    {
        if (GameController.Instance == null)
            return;
        if (GameController.Instance.difficult == 1)
        {
            round = 8;
            totalRound = 8;
            roundCount.text = round.ToString();
        }
        if (GameController.Instance.difficult == 2)
        {
            round = 10;
            totalRound = 10;
            roundCount.text = round.ToString();
        }
        if (GameController.Instance.difficult == 3)
        {
            round = 13;
            totalRound = 13;
            roundCount.text = round.ToString();
        }
    }

    public void CheckGameOver()
    {
        if (round < 0 && gameOverLabel != null)
        {
            gameOverLabel.SetActive(true);
            correctRound.text = "Correct Round: " + correctRoundCount.ToString() + " / " + totalRound.ToString();
            CheckAdvice();
        }
    }
    public void CheckAdvice()
    {
        float CheckPercent = totalRound / correctRoundCount;
        if (CheckPercent > 0.8)
        {
            advice.text = "You did a very good job!";
        }
        else if (CheckPercent > 0.6 && CheckPercent < 0.8)
        {
            advice.text = "Good job, but should do more than just this";
        }
        else
        {
            advice.text = "You doing fine but should training more";
        }
    }
}
