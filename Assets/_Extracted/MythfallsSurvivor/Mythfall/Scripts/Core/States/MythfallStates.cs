using UnityEngine;
using BillGameCore;
using Mythfall.Input;
using Mythfall.UI;
using Mythfall.UI.Panels;

namespace Mythfall.States
{
    /// <summary>Active when the user is on the Main Menu (MenuScene).</summary>
    public class MainMenuState : GameState
    {
        public override void Enter() => MythfallPanelRegistry.Show<MainMenuPanel>();
        public override void Exit()
        {
            MythfallPanelRegistry.Hide<MainMenuPanel>();
            // Defensive: if user left settings open and triggered a state-change shortcut,
            // close it so it doesn't bleed into the next screen.
            MythfallPanelRegistry.Hide<SettingsOverlay>();
        }
    }

    /// <summary>Character pick screen — still in MenuScene.</summary>
    public class CharacterSelectState : GameState
    {
        public override void Enter() => MythfallPanelRegistry.Show<CharacterSelectPanel>();
        public override void Exit() => MythfallPanelRegistry.Hide<CharacterSelectPanel>();
    }

    /// <summary>Active during gameplay (GameplayScene) — HUD visible, joystick driving input.</summary>
    public class InRunState : GameState
    {
        public override void Enter()
        {
            MobileInputManager.Reset();
            MythfallPanelRegistry.Show<HudPanel>();
        }
        public override void Exit()
        {
            MythfallPanelRegistry.Hide<HudPanel>();
            MobileInputManager.Reset();
        }
    }

    /// <summary>Defeat screen — slow-mo, GameOverPanel visible. Retry/Hub buttons drive next state.</summary>
    public class DefeatState : GameState
    {
        public override void Enter()
        {
            Time.timeScale = 0.3f;
            MythfallPanelRegistry.Show<GameOverPanel>();
        }
        public override void Exit()
        {
            Time.timeScale = 1f;
            MythfallPanelRegistry.Hide<GameOverPanel>();
        }
    }
}
