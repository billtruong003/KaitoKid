using Teabag.Game;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOver : MonoBehaviour
{
    public TMP_Text killerText;
    public TMP_Text valuesText;

    public void OnEnable()
    {
        if (BattleRoyaleManager.instance == null)
            return;

        ShowText(BattleRoyaleManager.instance.killedInfo.killerName, BattleRoyaleManager.instance.kills, BattleRoyaleManager.instance.killedInfo.place);
    }

    public void ShowText(string killer, int kills, int place)
    {
        killerText.text = $"KILLER: {killer}";
        valuesText.text = $"KILLS: {kills}\nPLACE: {place}";
    }
}
