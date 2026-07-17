#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace RadiantArena.UI
{
    /// <summary>
    /// Combat-phase HUD — 2 HP bars + canonical turn timer + round/turn indicator.
    /// Opens at CountdownState.Enter, closes at EndState.Enter, persists through
    /// MyTurn/OpponentTurn/Animating. HP bars animate via BillTween on HpChangedEvent.
    /// </summary>
    public class HudPanel : BasePanel
    {
        VisualElement? _root;
        Label? _round;
        Label? _timer;
        Label? _turnIndicator;

        Label? _meName;
        Label? _meWeapon;
        Label? _meHp;
        VisualElement? _meFill;

        Label? _oppName;
        Label? _oppWeapon;
        Label? _oppHp;
        VisualElement? _oppFill;

        Action<HpChangedEvent>? _onHp;
        IVisualElementScheduledItem? _tick;

        protected override void Build(VisualElement root)
        {
            _root = root;

            var tree = Resources.Load<VisualTreeAsset>("HudPanel");
            if (tree == null)
            {
                Debug.LogError("[Arena.HUD] HudPanel.uxml not found in Resources/");
                return;
            }
            tree.CloneTree(root);

            var uss = Resources.Load<StyleSheet>("hud");
            if (uss != null) root.styleSheets.Add(uss);

            _round         = root.Q<Label>("hud-round");
            _timer         = root.Q<Label>("hud-timer");
            _turnIndicator = root.Q<Label>("hud-turn");

            _meName   = root.Q<Label>("me-name");
            _meWeapon = root.Q<Label>("me-weapon");
            _meHp     = root.Q<Label>("me-hp");
            _meFill   = root.Q<VisualElement>("me-bar-fill");

            _oppName   = root.Q<Label>("opp-name");
            _oppWeapon = root.Q<Label>("opp-weapon");
            _oppHp     = root.Q<Label>("opp-hp");
            _oppFill   = root.Q<VisualElement>("opp-bar-fill");
        }

        public override void OnOpened()
        {
            _onHp = OnHpChanged;
            Bill.Events.Subscribe(_onHp);
            _tick = _root?.schedule.Execute(RefreshHeader).Every(250);
            SnapAllBars();
            RefreshHeader();
            Debug.Log($"[Arena.HUD] opened, snapping bars me={MyHp()}/{MyMax()} opp={OppHp()}/{OppMax()}");
        }

        public override void OnClosed()
        {
            if (_onHp != null) Bill.Events.Unsubscribe(_onHp);
            _onHp = null;
            _tick?.Pause();
            _tick = null;
            if (_meFill  != null) BillTween.KillTarget(_meFill);
            if (_oppFill != null) BillTween.KillTarget(_oppFill);
        }

        void OnHpChanged(HpChangedEvent e)
        {
            bool isMine = e.playerId == ArenaContext.MyDiscordId;
            var fill = isMine ? _meFill : _oppFill;
            var hpLabel = isMine ? _meHp : _oppHp;
            if (fill == null) return;

            int max = e.hpMax > 0 ? e.hpMax : 100;

            Debug.Log($"[Arena.HUD] HP {(isMine ? "me" : "opp")} {e.oldHp}→{e.newHp}/{max}");

            BillTween.KillTarget(fill);
            BillTween.Float((float)e.oldHp, (float)e.newHp, 0.40f, v =>
            {
                if (fill == null) return;
                float pct = Mathf.Clamp01(v / max) * 100f;
                fill.style.width = new StyleLength(Length.Percent(pct));
                fill.style.backgroundColor = HpColor(pct);
                if (hpLabel != null) hpLabel.text = $"{Mathf.RoundToInt(v)} / {max}";
            })?.SetTarget(fill);
        }

        void SnapAllBars()
        {
            SnapBar(_meFill, _meHp,  MyHp(),  MyMax());
            SnapBar(_oppFill, _oppHp, OppHp(), OppMax());
            if (_meName  != null) _meName.text  = !string.IsNullOrEmpty(ArenaContext.MyDiscordId)       ? ArenaContext.MyDiscordId       : "Me";
            if (_oppName != null) _oppName.text = !string.IsNullOrEmpty(ArenaContext.OpponentDiscordId) ? ArenaContext.OpponentDiscordId : "—";
            if (_meWeapon  != null) _meWeapon.text  = ArenaContext.MyPlayer?.LockedWeapon?.DisplayName
                                                  ?? ArenaContext.MyPlayer?.SelectedWeaponSlug ?? "—";
            if (_oppWeapon != null) _oppWeapon.text = ArenaContext.OpponentPlayer?.LockedWeapon?.DisplayName
                                                  ?? ArenaContext.OpponentPlayer?.SelectedWeaponSlug ?? "—";
        }

        void SnapBar(VisualElement? fill, Label? hpLabel, int hp, int max)
        {
            if (fill == null) return;
            if (max <= 0) max = 100;
            float pct = Mathf.Clamp01((float)hp / max) * 100f;
            fill.style.width = new StyleLength(Length.Percent(pct));
            fill.style.backgroundColor = HpColor(pct);
            if (hpLabel != null) hpLabel.text = $"{hp} / {max}";
        }

        void RefreshHeader()
        {
            if (_round != null) _round.text = ArenaContext.CurrentRound > 0
                ? $"Hiệp {ArenaContext.CurrentRound}"
                : "—";

            if (_timer != null)
            {
                var deadline = ArenaContext.TurnDeadlineAt;
                if (deadline <= 0)
                {
                    _timer.text = "—";
                    _timer.EnableInClassList("timer-urgent", false);
                }
                else
                {
                    var nowMs = (long)(System.DateTime.UtcNow
                        - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalMilliseconds;
                    var remainMs = System.Math.Max(0L, deadline - nowMs);
                    var seconds  = (int)(remainMs / 1000);
                    _timer.text = $"{seconds}s";
                    _timer.EnableInClassList("timer-urgent", seconds <= 5 && seconds > 0);
                }
            }

            if (_turnIndicator != null)
            {
                var tp = ArenaContext.TurnPlayerId;
                if (string.IsNullOrEmpty(tp)) _turnIndicator.text = "";
                else if (tp == ArenaContext.MyDiscordId) _turnIndicator.text = "Lượt của bạn";
                else _turnIndicator.text = "Lượt đối thủ";
            }
        }

        static Color HpColor(float pct)
        {
            if (pct > 50f) return new Color(0.30f, 0.86f, 0.55f);
            if (pct > 25f) return new Color(0.95f, 0.85f, 0.30f);
            return new Color(0.95f, 0.35f, 0.35f);
        }

        int MyHp()   => ArenaContext.MyPlayer?.Hp    ?? 100;
        int MyMax()  => ArenaContext.MyPlayer?.HpMax ?? 100;
        int OppHp()  => ArenaContext.OpponentPlayer?.Hp    ?? 100;
        int OppMax() => ArenaContext.OpponentPlayer?.HpMax ?? 100;
    }
}
