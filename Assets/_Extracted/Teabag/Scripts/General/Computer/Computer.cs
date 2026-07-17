using Teabag.Authentication;
using Teabag.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using TMPro;
using UnityEngine;
using Teabag.Player;
using Teabag.Services;

public class Computer : MonoBehaviour
{
    private static readonly List<Computer> _allComputers = new List<Computer>();
    private static string _pendingError;

    public static bool loading
    {
        get
        {
            if (!ServiceLocator.Get<IAuthenticationService>().FullyLoggedIn)
                return true;

            if (CameraFade.IsFading)
                return true;

            return ServiceLocator.Get<INetworkManager>().IsLoading;
        }
    }
    public TextMeshPro sidePanel;
    public TextMeshPro screen;

    public List<IComputerTab> tabs;
    public int currentTab;
    IComputerTab openTab
    {
        get
        {
            return tabs[currentTab];
        }
    }

    ComputerState state = ComputerState.Loading;

    private void Awake()
    {
        _allComputers.Add(this);

        GameServices.IsComputerLoading = () => loading;
        RenderTab();
        StartCoroutine(StartupSequence());

        if (!string.IsNullOrEmpty(_pendingError))
            InternalDisplayError(_pendingError);
    }

    private void OnDestroy()
    {
        _allComputers.Remove(this);
    }

    public static void DisplayError(string error)
    {
        _pendingError = error;
        GameLogger.Error("Computer displaying error: " + error);

        if (_allComputers.Count < 1)
            return;

        for (int i = 0; i < _allComputers.Count; i++)
        {
            _allComputers[i].StopAllCoroutines();
            _allComputers[i].InternalDisplayError(error);
        }
    }

    private void InternalDisplayError(string error)
    {
        screen.color = Color.red;
        sidePanel.color = Color.red;
        RenderText(error);
        RenderTab();
        state = ComputerState.Error;
    }

    IEnumerator StartupSequence()
    {
        if (state == ComputerState.Error)
        {
            GameLogger.Error("Invalid state while loading computer");
            yield break;
        }

        RenderText("Loading...");

        state = ComputerState.Loading;
        while (loading && state != ComputerState.Error)
            yield return null;

        if (state != ComputerState.Error)
        {
            state = ComputerState.ReadyScreen;
            var networkManager = ServiceLocator.Get<INetworkManager>();

            while (state == ComputerState.ReadyScreen)
            {
                RenderText("Welcome to the tactical computer\n\n" +
                           $"Players online: {networkManager.PlayerCount}\n\n" +
                           "Press any key to begin...");
                /*
                if (loading)
                    state = ComputerState.Use;
                */
                yield return null;
            }
        }
    }

    public void Update()
    {
        if (state != ComputerState.Error)
        {
            if (loading)
            {
                RenderText("Loading...");
                if (tabs[currentTab].IsOpen)
                {
                    // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                    tabs[currentTab].Close();
                }
            }
            else if (!tabs[currentTab].IsOpen)
            {
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                tabs[currentTab].Open();
                state = ComputerState.Use;
            }
        }
    }

    public void ButtonPress(KeyCode key)
    {
        GameLogger.Info("Computer button pressed: " + key);
        if (state == ComputerState.Loading || state == ComputerState.Error)
        {
            GameLogger.Info("Computer is loading or erroring: " + state);
            return;
        }

        if (state == ComputerState.ReadyScreen)
        {
            GameLogger.Info("Computer is on the ready screen - switching to use mode");
            state = ComputerState.Use;
            OpenTab(0);
            return;
        }

        int i = currentTab;
        switch (key)
        {
            case KeyCode.UpArrow:
                i--;
                if (i < 0) i = 0;
                OpenTab(i);
                GameLogger.Info("Up");
                break;
            case KeyCode.DownArrow:
                i++;
                if (i >= tabs.Count) i = tabs.Count - 1;
                OpenTab(i);
                GameLogger.Info("Down");
                break;
            default:
                openTab.Press(key);
                GameLogger.Info("Press");
                break;
        }
    }

    public void OpenTab(int i)
    {
        tabs[currentTab].Close();
        currentTab = i;
        tabs[currentTab].Open();
        RenderTab();
    }

    public void RenderTab()
    {
        string text = string.Empty;
        for (int i = 0; i < tabs.Count; i++)
        {
            if (state != ComputerState.Error)
                text += tabs[i].TabName + (currentTab == i ? "<--" : string.Empty) + "\n";
            else
                text += "ERR\n";
        }

        sidePanel.text = text;
    }

    public void RenderText(string t)
    {
        screen.text = t.ToUpper();
    }

    private enum ComputerState
    {
        Loading,
        ReadyScreen,
        Error,
        Use
    }
}
