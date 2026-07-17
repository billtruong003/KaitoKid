#nullable enable
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.States;
using UnityEngine;
using UnityEngine.UIElements;

namespace RadiantArena.Bootstrap {
    public class ArenaBootstrap : MonoBehaviour {
        void Start() {
            if (Bill.IsReady) InitArena();
            else Bill.Events.SubscribeOnce<GameReadyEvent>(_ => InitArena());
        }

        void InitArena() {
            Debug.Log($"[Arena] bootstrap ready (Bill.IsReady={Bill.IsReady})");
            Bill.Events.Fire(new ArenaBootstrapReadyEvent());

            ApplyArenaRuntimeTheme();

            ArenaStates.Register();

            // Spawn JuicePresenter once — Awake guards duplicates.
            if (RadiantArena.Juice.JuicePresenter.Instance == null)
            {
                var juiceGo = new GameObject("[JuicePresenter]");
                juiceGo.AddComponent<RadiantArena.Juice.JuicePresenter>();
            }

            // Spawn ArenaSceneBuilder — configures camera + builds map/players at runtime.
            if (RadiantArena.Arena.ArenaSceneBuilder.Instance == null)
            {
                var sceneGo = new GameObject("[ArenaSceneBuilder]");
                sceneGo.AddComponent<RadiantArena.Arena.ArenaSceneBuilder>();
            }

            Bill.State.GoTo<RadiantArena.States.BootState>();
        }

        // BillGameCore.UIService creates its PanelSettings without a Theme
        // Style Sheet, which causes Unity 6 UI Toolkit to skip text rendering
        // and clip pointer events. Until BillGameCore ships its own default
        // theme, Arena reaches into Bill.UI's PanelSettings via reflection
        // and assigns ArenaRuntimeTheme.tss from Resources.
        static void ApplyArenaRuntimeTheme() {
            var tss = Resources.Load<ThemeStyleSheet>("ArenaRuntimeTheme");
            if (tss == null) {
                Debug.LogWarning("[Arena] ArenaRuntimeTheme.tss not found in Resources/ — Bill.UI panel may not render text.");
                return;
            }
            var uiSvc = (object?)Bill.UI;
            if (uiSvc == null) {
                Debug.LogWarning("[Arena] Bill.UI not available; skipping theme assignment.");
                return;
            }
            var docField = uiSvc.GetType().GetField("_doc",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var doc = docField?.GetValue(uiSvc) as UIDocument;
            if (doc == null || doc.panelSettings == null) {
                Debug.LogWarning("[Arena] Bill.UI._doc / panelSettings unavailable; skipping theme.");
                return;
            }
            doc.panelSettings.themeStyleSheet = tss;
            var rootField = uiSvc.GetType().GetField("_uiRoot",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (rootField?.GetValue(uiSvc) is VisualElement uiRoot) {
                // Enable hit-testing on Bill.UI root so child Buttons receive clicks.
                uiRoot.pickingMode = PickingMode.Position;
            }
            Debug.Log("[Arena] Applied ArenaRuntimeTheme.tss + pickingMode to Bill.UI");
        }
    }
}
