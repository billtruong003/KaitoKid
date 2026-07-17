using System;
using Cysharp.Threading.Tasks;
using Teabag.Authentication;
using Teabag.Player.Rig;
using Teabag.Core;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Squido.JungleXRKit.Core;
using Teabag.Services;
using UnityEngine;

public class NameTab : IComputerTab
{
    private const int NAME_UPDATE_TIMEOUT_MS = 10000;

    private enum NameTabState
    {
        Editing,
        Loading,
        Success,
        Error
    }

    ComputerTextField currentName = new ComputerTextField(12);
    private NameTabState _state = NameTabState.Editing;
    PlayFabAsyncResult<UpdateUserTitleDisplayNameResult> result;
    string errorMessage = string.Empty;

    public override void OnOpen()
    {
        Debug.Log("Opened name tab");
        currentName.Set(PlayerData.displayName);

        // this seems kinda useless
        if (string.IsNullOrEmpty(currentName.ToString()))
            currentName.Set(string.Empty);

        Render();
    }

    public override void OnClose()
    {
        //Debug.Log("Close");
    }

    public override async void Press(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Return:
                if (_state == NameTabState.Loading)
                    return;

                _state = NameTabState.Loading;
                Render();

                try
                {
                    var (completed, updateResult) = await UniTask.WhenAny(
                        AuthenticationUtils.SetDisplayNameAsync(currentName.ToString()),
                        UniTask.Delay(NAME_UPDATE_TIMEOUT_MS));

                    if (!completed)
                    {
                        errorMessage = "NAME UPDATE TIMED OUT. TRY AGAIN.";
                        _state = NameTabState.Error;
                        Render();
                        return;
                    }

                    result = updateResult;
                    if (result.IsError)
                    {
                        errorMessage = result.Error?.ErrorMessage ?? "UNKNOWN ERROR";
                        _state = NameTabState.Error;
                    }
                    else
                    {
                        errorMessage = string.Empty;
                        currentName.Set(result.Result.DisplayName);
                        _state = NameTabState.Success;
                    }
                }
                catch (Exception ex)
                {
                    GameLogger.Error($"Failed to update display name: {ex.Message}");
                    errorMessage = "FAILED TO SET NAME. TRY AGAIN.";
                    _state = NameTabState.Error;
                }

                Render();
                break;
                /*
            case KeyCode.Backspace:
                /
                if (currentName.Length > 0)
                {
                    currentName = currentName.Substring(0, currentName.Length - 1);
                }
                /
                Render();
                break;
                */
            default:
                //Debug.Log(key.ToString());
                //Debug.Log(_state);
                if (_state != NameTabState.Loading)
                {
                    _state = NameTabState.Editing;
                    /*
                    char c = key.ToString().Last();
                    currentName += c;
                    if (currentName.Length > 12)
                        currentName = currentName.Substring(0, 12);
                    */
                    currentName.AddKey(key);
                    Render();
                }
                break;
        }
    }

    void Render()
    {
        string screenText = "";
        switch (_state)
        {
            case NameTabState.Editing:
                screenText = "CHOOSE A NICE NAME, GORILLA!\nPRESS ENTER TO SUBMIT NAME";
                screenText += $"\n\nCURRENT NAME: {currentName}";
                break;
            case NameTabState.Loading:
                screenText = "Loading...";
                break;
            case NameTabState.Success:
                screenText = "CHOOSE A NICE NAME, GORILLA!\nPRESS ENTER TO SUBMIT NAME";
                screenText += $"\n\nCURRENT NAME: {currentName}";
                screenText += "\n\nSET NAME";
                break;
            case NameTabState.Error:
                screenText = "CHOOSE A NICE NAME, GORILLA!\nPRESS ENTER TO SUBMIT NAME";
                screenText += $"\n\nCURRENT NAME: {currentName}";
                screenText += "\n\nFAILED TO SET NAME: " + errorMessage.ToUpper();
                break;
        }
        computer.RenderText(screenText);
    }
}
