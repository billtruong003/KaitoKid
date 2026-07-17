using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Player.Rig;
using System.Text;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Game;
using TMPro;
using Teabag.Networking;
using Teabag.Player;
using Teabag.UI;

public class Drone : MonoBehaviour
{
    public TextMeshPro playerText;
    public TextMeshPro roomText;
    public float spacing = 0.1f;
    float lastSpacing;
    public GameObject playerLine;
    public GorillaButton button;
    public List<GameObject> lines;
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
    private IGorillaService _gorillaService;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    private bool _isFirstRefresh = true;
    // Starting: 0.8
    // Distance between: 0.095

    // private void OnEnable() => Refresh();

    private void Update()
    {
        if (NetworkManager == null)
        {
            return;
        }

        if (_isFirstRefresh)
        {
            _isFirstRefresh = false;
            Refresh();
        }

        if (NetworkManager.Runner == null)
            Refresh();

        if (!Mathf.Approximately(spacing, lastSpacing))
        {
            Refresh();
            lastSpacing = spacing;
        }

        var rig = LocalHardwareRig;
        if (rig != null && Vector3.Distance(transform.position, rig.Headset.Position) > 2)
            gameObject.SetActive(false);
    }

    private void Awake()
    {
        _gorillaService = ServiceLocator.Get<IGorillaService>();
        if (_gorillaService != null) _gorillaService.OnGorillaSpawned += OnPlayerJoined;
        TeamManager.onTeamSwitched += OnTeamSwitched;
    }

    private void OnDestroy()
    {
        if (_gorillaService != null) _gorillaService.OnGorillaSpawned -= OnPlayerJoined;
        TeamManager.onTeamSwitched -= OnTeamSwitched;
    }

    public void OnPlayerJoined(IGorilla gorilla)
    {
        try { Refresh(); }
        catch (System.Exception e)
        {
            GameLogger.Error(this, $"OnPlayerJoined(): Failed to refresh -- {e}");
        }
    }

    public void OnTeamSwitched(Gorilla controller)
    {
        try { Refresh(); }
        catch (System.Exception e)
        {
            GameLogger.Error(this, $"OnTeamSwitched(): Failed to refresh -- {e}");
        }
    }

    public void Refresh()
    {
        if (NetworkManager == null)
        {
            return;
        }

        while (lines.Count > 0)
        {
            GameObject obj = lines[0];
            lines.RemoveAt(0);
            Destroy(obj);
        }

        if (!NetworkManager.Runner)
        {
            playerText.text = "Not in a room";
            roomText.text = "";
            button.gameObject.SetActive(false);
            return;
        }

        if (NetworkManager.CurrentRoom != null)
        {
            //button.gameObject.SetActive(NetworkManager.currentRoom.gameMode.Contains("BattleRoyale"));
            button.gameObject.SetActive(true);
            roomText.text = $"ROOM: {NetworkManager.CurrentRoom.FriendlyName}\nGAME MODE: {NetworkManager.CurrentRoom.GameMode.ToUpper()}";
        }

        StringBuilder builder = new StringBuilder();
        var droneGorillas = _gorillaService?.Gorillas;
        if (droneGorillas == null) return;
        for (int i = 0; i < droneGorillas.Count; i++)
        {
            Gorilla gorilla = (Gorilla)droneGorillas[i];

            string gorillaPlayerName = gorilla.playerName;
            Color teamColour = gorilla.team != null && TeamManager.instance != null
                ? TeamManager.GetTeam(gorilla.team.team).colour
                : Color.white;
            string teamColorHexString = ColorUtility.ToHtmlStringRGB(teamColour);

            gorillaPlayerName = $"<color=#{teamColorHexString}>{gorillaPlayerName}</color>";
            if (gorilla.health != null)
            {
                if (gorilla.health.isDead)
                    gorillaPlayerName = $"<s>{gorillaPlayerName}</s>";
            }

            builder.AppendLine(gorillaPlayerName);
            if (!gorilla.HasStateAuthority)
                SpawnLine(i, gorilla);
        }
        playerText.text = builder.ToString();
    }

    public void SpawnLine(int i, Gorilla gorilla)
    {
        float y = 0.8f - spacing * i;

        GameObject line = Instantiate(playerLine, playerText.transform.parent);
        Vector3 position = line.transform.localPosition;
        position.y = y;
        line.transform.localPosition = position;
        lines.Add(line);

        line.GetComponent<PlayerLine>().gorilla = gorilla;
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < 16; i++)
        {
            float y = 0.8f - spacing * i;
            Gizmos.DrawSphere(transform.position + Vector3.up * y * transform.localScale.magnitude, 0.1f);
        }
    }
}
