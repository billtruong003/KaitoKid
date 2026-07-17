using Teabag.Networking;
using System.Collections.Generic;
using System.Text;
using Squido.JungleXRKit.Core;
using Teabag.Authentication;
using Teabag.Core;
using UnityEngine;

public class RoomTab : IComputerTab
{
    public List<string> gameModes = new List<string>();
    public List<Date> dates = new List<Date>();

    public List<string> filtered
    {
        get
        {
            List<string> final = new List<string>();
            for (int i = 0; i < gameModes.Count && i < dates.Count; i++)
            {
                if (dates[i] != null)
                {
                    if (SyncedTime.ServerTime < dates[i].dateTime)
                        continue;
                }

                final.Add(gameModes[i]);
            }
            return final;
        }
    }
    public INetworkManager NetworkManager
    {
        get
        {
            if (_networkManager == null)
            {
                _networkManager = ServiceLocator.Get<INetworkManager>();
            }
            return _networkManager;
        }
    }
    private INetworkManager _networkManager;
    int gameMode;
    ComputerTextField targetCode = new ComputerTextField(8);

    public override void OnClose() { }

    public override void OnOpen() { }

    private void Update()
    {
        if (IsOpen)
        {
            StringBuilder builder = new StringBuilder();
            switch (NetworkManager.NetworkState)
            {
                case State.JOINING:
                    builder.AppendLine("Joining");
                    break;
                case State.LEAVING:
                    builder.AppendLine("Leaving");
                    break;
                default:
                    //builder.AppendLine("Use this tab to join public or private codes. If a code does not exist, it will be private.");
                    builder.AppendLine("Use this tab to join public or private codes. If a code does not exist, it will be private. Use number keys to select game mode.");
                    builder.AppendLine();
                    builder.AppendLine("Game to join: " + gameModes[gameMode]);
                    builder.AppendLine();
                    builder.AppendLine($"Room to join: {targetCode}");
                    builder.AppendLine();
                    builder.AppendLine($"Current room: {NetworkManager.CurrentRoomSafe.Name}");
                    if (NetworkManager.LastResult != null)
                    {
                        if (!NetworkManager.LastResult.Ok)
                            builder.Append($"Failed to join room! {NetworkManager.GetCurrentFailReason()}");
                    }
                    builder.AppendLine();
                    builder.AppendLine();
                    if (CanJoinRoom(targetCode.ToString()))
                        builder.AppendLine("Press enter to join!");
                    break;
            }
            computer.RenderText(builder.ToString());
        }
    }

    public bool CanJoinRoom(string name)
    {
        if (ModerationUtils.CheckBadWordEx(targetCode.ToString()) != null)
            return false;

        return targetCode.Length >= 3 && targetCode.ToString() != NetworkManager.CurrentRoomSafe.Name;
    }

    public override void Press(KeyCode key)
    {
        if (!NetworkManager.IsLoading)
        {
            if (key == KeyCode.Return)
            {
                if (CanJoinRoom(targetCode.ToString()))
                {
                    NetworkManager.JoinGame(gameModes[gameMode], targetCode.ToString());
                }
            }
            else if (IsNumKey(key))
            {
                gameMode = KeyToNum(key) - 1;
                if (gameMode < 0)
                    gameMode = 0;
                if (gameMode >= filtered.Count - 1)
                    gameMode = filtered.Count - 1;
            }
            else
            {
                targetCode.AddKey(key);
            }
        }
    }
}
