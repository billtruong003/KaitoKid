using UnityEngine;
using BillGameCore;
using Mythfall.Inventory;
using Mythfall.Localization;
using Mythfall.States;

namespace Mythfall.Core
{
    /// <summary>
    /// Orchestrates Mythfall's initialization. Lives on [Bootstrap] GameObject
    /// alongside <see cref="BillStartup"/>. Awake() configures BillStartup's
    /// step queue + next-scene before BillStartup.Start() runs the splash sequence.
    ///
    /// Step lambdas execute inside BillStartup's coroutine, which only begins after
    /// <see cref="GameReadyEvent"/> fires — so Bill services are guaranteed available
    /// when steps run.
    /// </summary>
    [RequireComponent(typeof(BillStartup))]
    public class GameBootstrap : MonoBehaviour
    {
        const string FirstScene = "MenuScene";

        [SerializeField] BillStartup startup;

        bool _poolsRegistered;
        bool _gameLayerRegistered;

        void Awake()
        {
            Debug.Log($"[GameBootstrap] Awake fired @ frame={Time.frameCount} BillReady={Bill.IsReady} sceneCount={UnityEngine.SceneManagement.SceneManager.sceneCount} activeScene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

            // Register Localization (no Bill deps — just JSON + PlayerPrefs)
            if (!ServiceLocator.Has<LocalizationService>())
            {
                ServiceLocator.Register(new LocalizationService());
                var loc = ServiceLocator.Get<LocalizationService>();
                Debug.Log($"[GameBootstrap] LocalizationService registered, language: {loc.CurrentLanguage}");
            }

            // Pool + game-layer registration: try now if Bill ready (editor bounce flow), else defer
            TryRegisterPools("Awake");
            TryRegisterGameLayer("Awake");

            // Configure BillStartup splash
            if (startup == null) startup = GetComponent<BillStartup>();
            if (startup == null)
            {
                Debug.LogError("[GameBootstrap] BillStartup component missing.");
                return;
            }

            startup.transition = TransitionType.Fade;
            startup.transitionDuration = 0.5f;

#if UNITY_EDITOR
            // Bill.Phase2 editor-return reads EditorPrefs Bill_ReturnScene, DELETES the key,
            // then calls Bill.Scene.Load(returnScene). By the time Awake runs (after Phase2),
            // the EditorPrefs is gone — but the scene load coroutine is still running.
            // Detect that via Bill.Scene.IsLoading instead.
            bool billLoadingScene = Bill.IsReady && Bill.Scene != null && Bill.Scene.IsLoading;
            startup.nextScene = billLoadingScene ? "" : FirstScene;
            Debug.Log($"[GameBootstrap] billLoadingScene={billLoadingScene} → BillStartup.nextScene='{startup.nextScene}' (empty = let Bill's editor-return win)");
#else
            startup.nextScene = FirstScene;
#endif

            startup.AddStep("Health Check", () =>
            {
                Bill.Trace.HealthCheck();
                return true;
            });
        }

        void OnEnable()
        {
            Debug.Log($"[GameBootstrap] OnEnable @ frame={Time.frameCount}");
            // Subscribe to GameReadyEvent in case Bill wasn't ready in Awake (production flow).
            // Gate on Bill.IsReady (cheap, no error log) — Bill.Events getter logs a
            // SERVICE NOT FOUND error if accessed before Phase2, even when null-checked.
            // If Bill isn't ready yet, Start() picks up registration once it is.
            if (Bill.IsReady)
                Bill.Events.SubscribeOnce<GameReadyEvent>(OnGameReady);
        }

        void Start()
        {
            Debug.Log($"[GameBootstrap] Start @ frame={Time.frameCount} BillReady={Bill.IsReady} poolsRegistered={_poolsRegistered} gameLayerRegistered={_gameLayerRegistered}");
            TryRegisterPools("Start");
            TryRegisterGameLayer("Start");
        }

        void OnDestroy()
        {
            Debug.Log($"[GameBootstrap] OnDestroy @ frame={Time.frameCount}");
        }

        void OnGameReady(GameReadyEvent _)
        {
            Debug.Log($"[GameBootstrap] OnGameReady @ frame={Time.frameCount}");
            TryRegisterPools("GameReadyEvent");
            TryRegisterGameLayer("GameReadyEvent");
        }

        void TryRegisterPools(string source)
        {
            if (_poolsRegistered)
            {
                Debug.Log($"[GameBootstrap] TryRegisterPools({source}) skipped — already registered");
                return;
            }
            if (!Bill.IsReady)
            {
                Debug.Log($"[GameBootstrap] TryRegisterPools({source}) deferred — Bill not ready yet");
                return;
            }
            RegisterPools();
            _poolsRegistered = true;
            Debug.Log($"[GameBootstrap] TryRegisterPools({source}) DONE");
        }

        void TryRegisterGameLayer(string source)
        {
            if (_gameLayerRegistered)
            {
                Debug.Log($"[GameBootstrap] TryRegisterGameLayer({source}) skipped — already registered");
                return;
            }
            if (!Bill.IsReady)
            {
                Debug.Log($"[GameBootstrap] TryRegisterGameLayer({source}) deferred — Bill not ready yet");
                return;
            }
            RegisterGameLayer();
            _gameLayerRegistered = true;
            Debug.Log($"[GameBootstrap] TryRegisterGameLayer({source}) DONE");
        }

        void RegisterGameLayer()
        {
            // InventoryService — owns persisted PlayerData. Initialize() reads Bill.Save,
            // so it must register AFTER Bill ready.
            if (!ServiceLocator.Has<InventoryService>())
            {
                ServiceLocator.Register(new InventoryService());
                Debug.Log("[GameBootstrap] InventoryService registered.");
            }

            // Mythfall game states — Bill.State is a core service registered by Bill itself.
            Bill.State.AddState<MainMenuState>();
            Bill.State.AddState<CharacterSelectState>();
            Bill.State.AddState<InRunState>();
            Bill.State.AddState<DefeatState>();
            Debug.Log("[GameBootstrap] 4 Mythfall states registered (MainMenu, CharacterSelect, InRun, Defeat).");
        }

        bool RegisterPools()
        {
            int registered = 0;
            int attempted = 0;

            attempted++; if (TryRegister("Enemy_Swarmer",    "Prefabs/Enemies/Swarmer",     30)) registered++;
            attempted++; if (TryRegister("Enemy_Brute",      "Prefabs/Enemies/Brute",       12)) registered++;
            attempted++; if (TryRegister("Enemy_Shooter",    "Prefabs/Enemies/Shooter",     12)) registered++;
            attempted++; if (TryRegister("Projectile_Arrow", "Prefabs/Projectiles/Arrow",   20)) registered++;
            attempted++; if (TryRegister("Enemy_Projectile", "Prefabs/Projectiles/EnemyProjectile", 20)) registered++;

            Debug.Log($"[GameBootstrap] {registered}/{attempted} pools registered.");
            return true;
        }

        static bool TryRegister(string poolKey, string resourcePath, int warmCount)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[GameBootstrap] Resources/{resourcePath}.prefab missing — skipping pool '{poolKey}'.");
                return false;
            }
            Bill.Pool.Register(poolKey, prefab, warmCount);
            return true;
        }
    }
}
