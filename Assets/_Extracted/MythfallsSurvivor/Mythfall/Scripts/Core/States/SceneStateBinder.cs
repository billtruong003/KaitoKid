using UnityEngine;
using BillGameCore;

namespace Mythfall.States
{
    /// <summary>
    /// Drop on a scene-resident GameObject (e.g. "[SceneBootstrap]") and pick the
    /// state this scene should enter on load. Runs in Start, defers until Bill is
    /// ready, then calls Bill.State.GoTo&lt;TargetState&gt;.
    ///
    /// Use cases:
    ///   - MenuScene → Initial = MainMenu (ensures cold boot opens Main Menu).
    ///   - GameplayScene → Initial = InRun (lets editor-direct-play of GameplayScene
    ///     show HUD without going through CharacterSelect).
    ///
    /// Idempotent: if the state is already current (e.g. CharacterSelectPanel set
    /// InRunState before triggering scene load), GoTo is a no-op.
    /// </summary>
    public class SceneStateBinder : MonoBehaviour
    {
        public enum InitialState { MainMenu, CharacterSelect, InRun, Defeat }

        [SerializeField] InitialState initialState = InitialState.MainMenu;

        void Start()
        {
            if (Bill.IsReady) Apply();
            else Bill.Events?.SubscribeOnce<GameReadyEvent>(_ => Apply());
        }

        void Apply()
        {
            if (Bill.State == null)
            {
                Debug.LogWarning("[SceneStateBinder] Bill.State unavailable — cannot enter " + initialState);
                return;
            }

            switch (initialState)
            {
                case InitialState.MainMenu:        Bill.State.GoTo<MainMenuState>();        break;
                case InitialState.CharacterSelect: Bill.State.GoTo<CharacterSelectState>(); break;
                case InitialState.InRun:           Bill.State.GoTo<InRunState>();           break;
                case InitialState.Defeat:          Bill.State.GoTo<DefeatState>();          break;
            }
            Debug.Log($"[SceneStateBinder] {gameObject.scene.name} → GoTo<{initialState}State>");
        }
    }
}
