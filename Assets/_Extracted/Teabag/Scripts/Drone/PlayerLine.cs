using Teabag.Authentication;
using Teabag.Networking;
using Teabag.Player.Rig;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Teabag.Player;

public class PlayerLine : MonoBehaviour
{
    public Gorilla gorilla;

    public GameObject buttons;
    //public GameObject reportButtons;
    //public TextMeshPro reportedText;

    private async UniTaskVoid Awake()
    {
        buttons.SetActive(false);
        while (gorilla == null)
            await UniTask.Yield();

        if (gorilla.HasStateAuthority)
            Destroy(gameObject);

        while (string.IsNullOrEmpty(gorilla.id))
            await UniTask.Yield();
        buttons.SetActive(true);

        //Reported();
    }
    
    /*
    public void Reported()
    {
        bool reported = ModerationUtils.reportedPlayers.Contains(controller.playerId);
        reportButtons.SetActive(!reported);
        reportedText.gameObject.SetActive(reported);
    }
    */
}
