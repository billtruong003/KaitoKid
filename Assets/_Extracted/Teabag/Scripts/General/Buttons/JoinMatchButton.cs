using Teabag.Networking;
using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Player;
using Teabag.UI;

public class JoinMatchButton : GorillaButton
{
    public string gameMode;
    public bool leave;
    public bool changeGameMode;

    [ContextMenu("On Press")]
    public override void OnPress()
    {
        var networkManager = ServiceLocator.Get<INetworkManager>();
        if (!leave)
        {
            /*
            if (NetworkManager.currentRoom != null)
            {
                if (NetworkManager.currentRoom.isPrivate && changeGameMode)
                {
                    Debug.Log("Changing game mode");
                    NetworkManager.JoinGame(gameMode, NetworkManager.currentRoom.name);
                    return;
                }
            }
            */

            networkManager.JoinGame(gameMode);
        }
        else
        {
            networkManager.LeaveGame();
            /*
            if (!BananaBlimp.instance.isInBlimp)
                GorillaServiceUtils.LocalGorillaPlayer.Respawn();
            */
        }
    }
}
