#nullable enable
using BillGameCore;
using RadiantArena.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace RadiantArena.UI
{
    /// <summary>
    /// Terminal modal — winner banner + outcome label + final HPs + 2 stub buttons.
    /// EndState opens this; calls Render(...) to fill content. Buttons log only
    /// (replay → D.U10/D.U11, lobby → D.U11).
    /// </summary>
    public class ResultPanel : BasePanel
    {
        VisualElement? _root;
        Label? _banner;
        Label? _outcome;
        Label? _hpMe;
        Label? _hpOpp;
        Button? _replayBtn;
        Button? _lobbyBtn;

        protected override void Build(VisualElement root)
        {
            _root = root;

            var tree = Resources.Load<VisualTreeAsset>("ResultPanel");
            if (tree == null)
            {
                Debug.LogError("[Arena.Result] ResultPanel.uxml not found in Resources/");
                return;
            }
            tree.CloneTree(root);

            // Load result.uss (own styles) + lobby.uss (.btn / .btn-primary / .btn-secondary inheritance).
            var resultUss = Resources.Load<StyleSheet>("result");
            if (resultUss != null) root.styleSheets.Add(resultUss);
            var lobbyUss = Resources.Load<StyleSheet>("lobby");
            if (lobbyUss != null) root.styleSheets.Add(lobbyUss);

            _banner    = root.Q<Label>("result-banner");
            _outcome   = root.Q<Label>("result-outcome");
            _hpMe      = root.Q<Label>("result-hp-me");
            _hpOpp     = root.Q<Label>("result-hp-opp");
            _replayBtn = root.Q<Button>("result-replay-btn");
            _lobbyBtn  = root.Q<Button>("result-lobby-btn");

            if (_replayBtn != null) _replayBtn.clicked += () => Debug.Log("[Arena.Result] replay clicked (stub — D.U10/D.U11)");
            if (_lobbyBtn  != null) _lobbyBtn.clicked  += () => Debug.Log("[Arena.Result] back-to-lobby clicked (stub — D.U11)");
        }

        public void Render(string winnerId, string outcome,
            System.Collections.Generic.Dictionary<string, int>? finalHp)
        {
            var meId  = ArenaContext.MyDiscordId ?? "";
            var oppId = ArenaContext.OpponentDiscordId ?? "";

            string verdictClass;
            string verdictText;
            if (string.IsNullOrEmpty(winnerId))
            {
                verdictText = "Trận đấu kết thúc";
                verdictClass = "draw";
            }
            else if (winnerId == meId)
            {
                verdictText = "Trận đấu THẮNG";
                verdictClass = "win";
            }
            else
            {
                verdictText = "Trận đấu THUA";
                verdictClass = "lose";
            }

            if (_banner != null)
            {
                _banner.text = verdictText;
                _banner.EnableInClassList("win",  verdictClass == "win");
                _banner.EnableInClassList("lose", verdictClass == "lose");
                _banner.EnableInClassList("draw", verdictClass == "draw");
            }
            if (_outcome != null)
            {
                _outcome.text = string.IsNullOrEmpty(outcome) ? "" : $"({outcome})";
            }

            int meHp = 0, oppHp = 0;
            if (finalHp != null)
            {
                if (!string.IsNullOrEmpty(meId))  finalHp.TryGetValue(meId,  out meHp);
                if (!string.IsNullOrEmpty(oppId)) finalHp.TryGetValue(oppId, out oppHp);
            }
            if (_hpMe  != null) _hpMe.text  = $"{(string.IsNullOrEmpty(meId)  ? "Me" : meId)}: {meHp} HP";
            if (_hpOpp != null) _hpOpp.text = $"{(string.IsNullOrEmpty(oppId) ? "—" : oppId)}: {oppHp} HP";
        }
    }
}
