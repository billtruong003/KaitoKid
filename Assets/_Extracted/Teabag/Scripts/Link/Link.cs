using Teabag.Authentication;
using System;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Services;
using TMPro;
using UnityEngine;
using Teabag.UI;

public class Link : MonoBehaviour
{
    public GameObject notJoined;
    public GameObject notLinked;
    public GameObject linked;
    State state;
    DiscordConnection connection;
    public DiscordAccountViewer accountViewer;
    public GorillaButton button;
    public TextMeshPro text;
    public TextMeshPro code;

    private void Awake()
    {
        text.gameObject.SetActive(false);
        code.gameObject.SetActive(false);

        button.onClick.AddListener(LinkAccount);

        LoadState();
        LoadAccountAsync();
    }

    public async UniTask LoadAccountAsync(bool first = true)
    {
        var response = await API.RequestAsync<GorillaResult<DiscordConnection>>($"api/discord/v1/link/{ServiceLocator.Get<IAuthenticationService>().PlayFabId}", HttpMethod.Get);
        if (response.success)
        {
            connection = response.message;
            if (connection.discordId > 0)
            {
                state = State.Linked;
                LoadState();

                await accountViewer.LoadAccountAsync(response.message.discordId);
                if (!first) await AuthenticationUtils.GetCosmeticsAsync();
            }
        }
        else if (first)
        {
            state = State.NotJoined;
            LoadState();
        }
    }

    public async UniTaskVoid JoinDiscord()
    {
        Application.OpenURL("https://discord.gg/gorillaroyale");
        while (GorillaLocomotion.Player.isPaused)
            await UniTask.Yield();
        OpenLinking();
    }

    public void OpenLinking()
    {
        state = State.NotLinked;
        LoadState();
    }

    public async void LinkAccount()
    {
        button.GetComponentInChildren<TextMeshPro>().text = "LOADING";
        button.interactable = false;

        var response = await API.RequestAsync<GorillaResult<string>>("api/discord/v1/link/create", HttpMethod.Post, API.CreateAuthorization());
        ShowCode(response.success, response.message);
    }

    public void ShowCode(bool success, string message)
    {
        text.gameObject.SetActive(true);
        code.gameObject.SetActive(true);
        code.text = success ? message : "ERROR";

        if (success)
        {
            button.GetComponentInChildren<TextMeshPro>().text = "DONE!";
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Done);
            button.interactable = true;
        }
    }

    public async void Done()
    {
        button.interactable = false;
        await LoadAccountAsync(false);
        button.interactable = true;
    }

    public void LoadState()
    {
        notJoined.SetActive(state == State.NotJoined);
        notLinked.SetActive(state == State.NotLinked);
        linked.SetActive(state == State.Linked);
    }

    public enum State
    {
        NotJoined,
        NotLinked,
        Linked
    }

    [Serializable]
    public class DiscordConnection
    {
        public string playFabId;
        public long discordId;
        public string link;
    }
}
