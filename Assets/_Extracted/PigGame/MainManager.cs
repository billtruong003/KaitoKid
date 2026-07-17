using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

public class MainManager : MonoBehaviour
{

    [SerializeField] private GameObject coinAssign;
    [SerializeField] private Transform CoinContainer;
    [SerializeField] private TextMeshProUGUI sign;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI coinTargetLabel;
    [SerializeField] private float money = 0;
    [SerializeField] private bool isAdd;
    [SerializeField] private int coinTarget;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        money = 0;
        GenCoinTarget();
    }

    public void CreateCoinImage(GameObject coinImage)
    {
        if (isAdd)
        {
            GameObject coin = Instantiate(coinImage, CoinContainer);
            coin.name = coinImage.gameObject.name;
        }
    }
    public void ChangeSign()
    {
        if (sign.text == "+")
        {
            isAdd = false;
            sign.text = "-";
        }
        else
        {
            isAdd = true;
            sign.text = "+";
        }
    }
    private void DisplayMoney()
    {
        moneyText.text = money.ToString();
    }
    public void ChangeMoney(int money)
    {
        if (isAdd)
        {
            this.money += money;
        }
        else
        {
            RemoveCoin(money.ToString(), money);
        }
    }
    private void RemoveCoin(string coinName, int money)
    {
        if (CoinContainer.childCount < 1)
            return;

        foreach (Transform coin in CoinContainer)
        {
            Debug.Log("Child name: " + coin.name + " CoinName: " + coinName);
            if (coin.name == coinName)
            {
                coinAssign = coin.gameObject;
            }
        }

        if (coinAssign != null)
        {
            Destroy(coinAssign);
            this.money -= money;
        }
    }
    [Button]
    private void RemoveAllCoin()
    {
        // Loop through each child of the parent
        foreach (Transform child in CoinContainer)
        {
            // Destroy the child object
            Destroy(child.gameObject);
        }

        // After all children are destroyed, clear the children list
        money = 0;
        CoinContainer.DetachChildren();
    }
    private void GenCoinTarget()
    {
        coinTarget = Random.Range(0, 1000);
        coinTargetLabel.text = coinTarget.ToString();
    }
    public void CheckCoinTrue()
    {
        if (coinTarget == money)
        {
            Debug.Log("True");
            if (ScoreManager.instance != null)
            {
                Debug.Log("Nice");
                ScoreManager.instance.MinusRound();
            }
            else
            {
                Debug.Log("NotNice");
            }
            ScoreManager.instance.PlusCorrectRound();
            RemoveAllCoin();
            GenCoinTarget();
        }
    }
    public void BackToMenu()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.BackToMenu();
        }
    }
    public void ReloadScene()
    {
        if (GameController.Instance != null)
        {
            GameController.Instance.Reload();
        }
    }
}
