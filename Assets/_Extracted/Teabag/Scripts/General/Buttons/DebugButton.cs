using Teabag.Authentication;
using Teabag.BattlePass;
using Teabag.BattlePass.UI;
using Teabag.Core;
using Teabag.Networking;
using Teabag.Player.Rig;
using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Player;
using Teabag.Services;
using Teabag.UI;

public class DebugButton : GorillaButton
{
    public int kills = 5;
    //public ChallengeType type;
    //int i = 0;
    public override void OnPress()
    {
        /*
        VRRig rig = VRRigAssignment.AssignVRRig();
        rig.transform.position = new Vector3(0, 42, i);
        i++;
        */
        /*
        if (NetworkManager.runner == null)
        {
            PopupManager.Display("Not in a room", transform.position, Color.red, 1);
            return;
        }

        NetworkObjectsManager.Spawn(15, transform.position + Vector3.up, Quaternion.identity, Vector3.one);
        */

        //DailyChallenges.Score(type);

        /*
        PopupManager.Display(kills + " kills", transform.position, Color.white, 1);
        AuthenticationUtils.SubmitKills(kills, true);
        */
#if !UNITY_EDITOR
        var authManager = ServiceLocator.Get<IAuthenticationService>();
        if (authManager.LoggedIn)
            ModerationUtils.BanAsync(0, "CHEATING", authManager.PlayFabId);
#else
        //AuthenticationUtils.SubmitKillsAsync(15, true);
        //BattlePassManager.Xp += 20;
        //GameLogger.Info("Current XP: " + BattlePassManager.XP);
#endif
    }
}
