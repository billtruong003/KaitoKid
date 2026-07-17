#nullable enable
// LobbyPanel — UI Toolkit lobby. Renders the server-sent weapon list,
// surfaces pick + ready events for LobbyState to forward to NetClient.
//
// UXML + USS live under Assets/RadiantArena/UI/Resources/ and are loaded via
// Resources.Load. BasePanel.Build is code-first, but CloneTree inflates the
// asset hierarchy under the panel root so all UXML named elements are
// reachable via root.Q<T>("name").
//
// Opponent ready state is polled every 250ms from ArenaContext rather than
// emitted as a per-field event — cheap, decoupled, no extra event surface.

using System;
using System.Collections.Generic;
using BillGameCore;
using RadiantArena.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace RadiantArena.UI
{
    public class LobbyPanel : BasePanel
    {
        // Public surface — LobbyState subscribes to these. Plain C# events
        // (not Bill.Events) because the audience is exactly one state.
        public event Action<string>? OnWeaponPicked;
        public event Action? OnReadyClicked;
        public event Action? OnUnreadyClicked;

        VisualElement? _root;
        ListView? _weaponList;
        Label? _meName;
        Label? _meReady;
        Label? _opponentName;
        Label? _opponentReady;
        Label? _sessionId;
        Button? _readyBtn;
        Button? _unreadyBtn;

        WeaponSnapshot[] _weapons = Array.Empty<WeaponSnapshot>();
        IVisualElementScheduledItem? _opponentPoll;

        protected override void Build(VisualElement root)
        {
            _root = root;

            var tree = Resources.Load<VisualTreeAsset>("LobbyPanel");
            if (tree == null)
            {
                Debug.LogError("[Arena.Lobby] LobbyPanel.uxml not found in Resources/ — panel will be empty.");
                return;
            }
            tree.CloneTree(root);

            var uss = Resources.Load<StyleSheet>("lobby");
            if (uss != null) root.styleSheets.Add(uss);
            else Debug.LogWarning("[Arena.Lobby] lobby.uss not found — panel will use default Unity styles.");

            _weaponList = root.Q<ListView>("weapon-list");
            _meName = root.Q<Label>("me-name");
            _meReady = root.Q<Label>("me-ready");
            _opponentName = root.Q<Label>("opponent-name");
            _opponentReady = root.Q<Label>("opponent-ready");
            _sessionId = root.Q<Label>("session-id");
            _readyBtn = root.Q<Button>("ready-btn");
            _unreadyBtn = root.Q<Button>("unready-btn");

            if (_weaponList != null)
            {
                _weaponList.fixedItemHeight = 36;
                _weaponList.selectionType = SelectionType.Single;
                _weaponList.makeItem = MakeWeaponItem;
                _weaponList.bindItem = BindWeaponItem;
                _weaponList.selectionChanged += OnSelectionChanged;
            }

            if (_readyBtn != null) _readyBtn.clicked += () => OnReadyClicked?.Invoke();
            if (_unreadyBtn != null) _unreadyBtn.clicked += () => OnUnreadyClicked?.Invoke();
        }

        VisualElement MakeWeaponItem()
        {
            var label = new Label();
            label.AddToClassList("weapon-item");
            return label;
        }

        void BindWeaponItem(VisualElement el, int i)
        {
            if (i < 0 || i >= _weapons.Length) return;
            var w = _weapons[i];
            ((Label)el).text = $"{w.DisplayName}  ·  {w.Tier} / {w.Category}";
        }

        void OnSelectionChanged(IEnumerable<object> _)
        {
            if (_weaponList == null) return;
            int idx = _weaponList.selectedIndex;
            if (idx < 0 || idx >= _weapons.Length) return;
            OnWeaponPicked?.Invoke(_weapons[idx].Slug);
        }

        public void SetAvailableWeapons(WeaponSnapshot[] weapons)
        {
            _weapons = weapons ?? Array.Empty<WeaponSnapshot>();
            if (_weaponList != null)
            {
                _weaponList.itemsSource = _weapons;
                _weaponList.Rebuild();
            }
        }

        public void SetSessionId(string sessionId)
        {
            if (_sessionId != null) _sessionId.text = string.IsNullOrEmpty(sessionId) ? "" : $"session {sessionId}";
        }

        public override void OnOpened()
        {
            _opponentPoll = _root?.schedule.Execute(RefreshFromContext).Every(250);
            RefreshFromContext();
        }

        public override void OnClosed()
        {
            _opponentPoll?.Pause();
            _opponentPoll = null;
        }

        void RefreshFromContext()
        {
            if (_meName != null)
            {
                _meName.text = string.IsNullOrEmpty(ArenaContext.MyDiscordId) ? "Me" : ArenaContext.MyDiscordId;
            }
            if (_meReady != null)
            {
                var ready = ArenaContext.MyPlayer != null && ArenaContext.MyPlayer.Ready;
                _meReady.text = ready ? "READY" : "Not ready";
                _meReady.EnableInClassList("ready", ready);
            }
            if (_opponentName != null)
            {
                _opponentName.text = string.IsNullOrEmpty(ArenaContext.OpponentDiscordId)
                    ? "Waiting for opponent…"
                    : ArenaContext.OpponentDiscordId;
            }
            if (_opponentReady != null)
            {
                if (ArenaContext.OpponentPlayer == null)
                {
                    _opponentReady.text = "--";
                    _opponentReady.EnableInClassList("ready", false);
                }
                else
                {
                    var ready = ArenaContext.OpponentPlayer.Ready;
                    _opponentReady.text = ready ? "READY" : "Not ready";
                    _opponentReady.EnableInClassList("ready", ready);
                }
            }
        }
    }
}
