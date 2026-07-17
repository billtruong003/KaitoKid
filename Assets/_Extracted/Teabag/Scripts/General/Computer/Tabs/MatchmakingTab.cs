using System.Collections;
using System.Collections.Generic;
using System.Text;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Core;

public class MatchmakingTab : IComputerTab
{
    public static bool joinGamesInProgress
    {
        get => ServiceLocator.Get<IDataPersistenceService>()?.LoadData<int>("JoinBattleRoyaleInProgress", 1) == 1;
        set => ServiceLocator.Get<IDataPersistenceService>()?.TrySaveData("JoinBattleRoyaleInProgress", value ? 1 : 0);
    }

    public override void OnClose()
    {

    }

    public override void OnOpen()
    {
        Refresh();
    }

    public override void Press(KeyCode key)
    {
        if (key == KeyCode.Return)
            joinGamesInProgress = !joinGamesInProgress;
        Refresh();
    }

    public void Refresh()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("This is the matchmaking tab for Battle Royale, please use the settings below to customise your experience");
        builder.AppendLine();
        builder.AppendLine("Join games in progress: " + joinGamesInProgress);
        builder.AppendLine();
        builder.AppendLine("Use the enter key to toggle");
        computer.RenderText(builder.ToString());
    }
}
