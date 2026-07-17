#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Photon.Realtime;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Networking.Extensions;
using Teabag.Player;
using UnityEngine;

namespace Teabag.Networking
{
    /// <summary>
    /// Editor-only debug GUI window for the NetworkManager.
    /// Displays connection info, regions, players, world/scene data, errors, network logs, and stats.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkManagerDebugGUI : MonoBehaviour
    {
        private NetworkManager _networkManager;
        private IGorillaService _gorillaService;

        private bool _debugWindowInitialized = false;
        private Rect _debugWindowRect = new Rect(10, 10, 600, 400);
        private int _selectedTab = 0;
        private Vector2 _scrollPosition = Vector2.zero;
        private bool _showDebugWindow = false;
        private readonly List<string> _errorLog = new List<string>();
        private readonly List<string> _networkLog = new List<string>();
        private string _customRegion = "";
        private readonly string[] _tabNames = { "Debug Info", "Regions", "Players", "World", "Errors", "Network", "Stats", "Connection" };

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
        }

        private void OnGUI()
        {
            // Always draw a small toggle button in the top-left corner.
            if (GUI.Button(new Rect(10, 10, 80, 20), _showDebugWindow ? "CLOSE" : "OPEN"))
                _showDebugWindow = !_showDebugWindow;

            if (!_showDebugWindow)
                return;

            if (!_debugWindowInitialized)
                InitializeDebugWindow();

            _debugWindowRect = GUILayout.Window(0, _debugWindowRect, DrawDebugWindow, "Fusion Debug Console", GUILayout.MinWidth(400), GUILayout.MinHeight(300));
        }

        private void InitializeDebugWindow()
        {
            _debugWindowInitialized = true;
            Application.logMessageReceived += OnFusionLogMessageReceived;
        }

        private void OnFusionLogMessageReceived(string logString, string stackTrace, UnityEngine.LogType type)
        {
            if (type == UnityEngine.LogType.Error || type == UnityEngine.LogType.Exception)
            {
                _errorLog.Add($"[{DateTime.Now:HH:mm:ss}] {logString}");
                if (_errorLog.Count > 100) _errorLog.RemoveAt(0);
            }

            if (logString.Contains("Fusion") || logString.Contains("Network"))
            {
                _networkLog.Add($"[{DateTime.Now:HH:mm:ss}] {logString}");
                if (_networkLog.Count > 100) _networkLog.RemoveAt(0);
            }
        }

        private void DrawDebugWindow(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close", GUILayout.Width(60)))
                _showDebugWindow = false;

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear Logs", GUILayout.Width(80)))
            {
                _errorLog.Clear();
                _networkLog.Clear();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _tabNames.Length; i++)
            {
                GUI.backgroundColor = _selectedTab == i ? Color.cyan : Color.white;
                if (GUILayout.Button(_tabNames[i], GUILayout.Height(25)))
                    _selectedTab = i;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0: DrawDebugInfoTab(); break;
                case 1: DrawRegionsTab(); break;
                case 2: DrawPlayersTab(); break;
                case 3: DrawWorldTab(); break;
                case 4: DrawErrorsTab(); break;
                case 5: DrawNetworkTab(); break;
                case 6: DrawStatsTab(); break;
                case 7: DrawConnectionTab(); break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawDebugInfoTab()
        {
            GUILayout.Label("=== CONNECTION INFO ===", UnityEditor.EditorStyles.boldLabel);
            GUILayout.Label($"State: {_networkManager.NetworkState}");
            GUILayout.Label($"Room: {_networkManager.CurrentRoomSafe.FriendlyName}");
            GUILayout.Label($"Game Mode: {_networkManager.CurrentGameMode}");
            GUILayout.Label($"Is Connected: {_networkManager.IsConnected}");
            GUILayout.Label($"Is Master: {_networkManager.IsMaster}");
            GUILayout.Label($"In Networked Room: {_networkManager.InNetworkedRoom}");
            GUILayout.Label($"Is Loading: {_networkManager.IsLoading}");
            GUILayout.Label($"Time in Room: {_networkManager.TimeSpentInRoom:mm\\:ss}");

            GUILayout.Space(10);
            GUILayout.Label("=== QUICK ACTIONS ===", UnityEditor.EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (_networkManager.GameModeDatabase)
            {
                for (int i = 0; i < _networkManager.GameModeDatabase.gameModes.Count; i++)
                {
                    var gm = _networkManager.GameModeDatabase.gameModes[i];
                    if (!gm) continue;
                    if (GUILayout.Button(gm.Id, GUILayout.Height(30)))
                        _networkManager.JoinGame(gm.Id, string.Empty);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Leave Game", GUILayout.Height(25)))
                _networkManager.LeaveGame();
            if (GUILayout.Button("Refresh", GUILayout.Height(25)))
                _ = _networkManager.GetPlayerCountAsync();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("=== SYSTEM INFO ===", UnityEditor.EditorStyles.boldLabel);
            GUILayout.Label($"Unity Version: {Application.unityVersion}");
            GUILayout.Label($"App Version: {Application.version}");
            GUILayout.Label($"Platform: {Application.platform}");
            GUILayout.Label($"FPS: {(1f / Time.unscaledDeltaTime):F1}");
            GUILayout.Label($"Frame Count: {Time.frameCount}");
        }

        private void DrawRegionsTab()
        {
            GUILayout.Label("=== PHOTON REGIONS ===", UnityEditor.EditorStyles.boldLabel);

            string currentRegion = PhotonAppSettings.Global.AppSettings.FixedRegion;
            string connectedRegion = _networkManager.Runner?.SessionInfo?.Region;
            bool isAuto = string.IsNullOrEmpty(currentRegion);
            string regionDisplay;
            if (isAuto)
            {
                regionDisplay = string.IsNullOrEmpty(connectedRegion)
                    ? "Auto (not in session)"
                    : $"Auto (connected: {connectedRegion})";
            }
            else
            {
                regionDisplay = string.IsNullOrEmpty(connectedRegion)
                    ? currentRegion
                    : $"{currentRegion} (connected: {connectedRegion})";
            }
            GUILayout.Label($"Current Region: {regionDisplay}");

            GUILayout.Space(10);

            string[] regions = { "eu", "us", "asia", "jp", "au", "usw", "sa", "cae", "in", "cn" };
            string[] regionNames = { "Europe", "US East", "Asia", "Japan", "Australia", "US West", "South America", "Canada East", "India", "China" };
            for (int i = 0; i < regions.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = currentRegion == regions[i] ? Color.green : Color.white;
                if (GUILayout.Button($"{regionNames[i]} ({regions[i]})", GUILayout.Height(25)))
                {
                    PhotonAppSettings.Global.AppSettings.FixedRegion = regions[i];
                    GameLogger.Debug($"changed region to: {regions[i]}");
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            GUILayout.Label("Custom Region:");
            GUILayout.BeginHorizontal();
            _customRegion = GUILayout.TextField(_customRegion);
            if (GUILayout.Button("Set", GUILayout.Width(50)))
            {
                if (!string.IsNullOrEmpty(_customRegion))
                {
                    PhotonAppSettings.Global.AppSettings.FixedRegion = _customRegion;
                    GameLogger.Debug($"changed region to: {_customRegion}");
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (GUILayout.Button("Auto Region", GUILayout.Height(30)))
            {
                PhotonAppSettings.Global.AppSettings.FixedRegion = string.Empty;
                GameLogger.Debug("set to auto region selection");
            }

            GUILayout.Space(10);
            GUILayout.Label("Note: Region changes require reconnection to take effect.", UnityEditor.EditorStyles.helpBox);
        }

        private void DrawPlayersTab()
        {
            GUILayout.Label("=== PLAYER INFO ===", UnityEditor.EditorStyles.boldLabel);

            if (_networkManager.CurrentRoom != null)
            {
                GUILayout.Label($"Players: {_networkManager.CurrentRoom.PlayerCount}/{_networkManager.CurrentRoom.MaxPlayers}");
                GUILayout.Label($"Room: {_networkManager.CurrentRoom.Name}");
                GUILayout.Label($"Is Private: {_networkManager.CurrentRoom.IsPrivate}");
                GUILayout.Label($"Is Running: {_networkManager.CurrentRoom.IsRunning}");
                GUILayout.Label($"Is Modded: {_networkManager.CurrentRoom.IsModded}");
            }
            else
                GUILayout.Label("Not in a networked room");

            GUILayout.Space(10);

            if (ServiceLocator.TryGet(out _gorillaService) && _networkManager.Runner && _networkManager.Runner.ActivePlayers != null)
            {
                GUILayout.Label("=== ACTIVE PLAYERS ===", UnityEditor.EditorStyles.boldLabel);
                foreach (var player in _networkManager.Runner.ActivePlayers)
                {
                    Gorilla gorilla = null;
                    if (_gorillaService != null)
                    {
                        foreach (var gorillaEntry in _gorillaService.Gorillas)
                        {
                            if (gorillaEntry is Gorilla candidate && candidate.Object && candidate.Object.StateAuthority == player)
                            {
                                gorilla = candidate;
                                break;
                            }
                        }
                    }

                    if (!gorilla)
                        continue;

                    GUILayout.BeginHorizontal();

                    player.TryGetUserProperty("ping", out int expectedPing);
                    GUILayout.Label($"{gorilla.playerName} ({player.PlayerId}, " +
                                    $"{(player.IsRealPlayer && _networkManager.Runner.LocalPlayer == player ? "Local" : "Remote")}" +
                                    $", ±{expectedPing}ms)");
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10);
            GUILayout.Label($"Global Player Count: {_networkManager.PlayerCount}");
            if (GUILayout.Button("Refresh Player Count", GUILayout.Height(25)))
                _ = _networkManager.GetPlayerCountAsync();
        }

        private void DrawWorldTab()
        {
            GUILayout.Label("=== WORLD & SCENES ===", UnityEditor.EditorStyles.boldLabel);

            GUILayout.Label($"Scene Count: {UnityEngine.SceneManagement.SceneManager.sceneCount}");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Scene {i}: {scene.name}");
                GUILayout.Label($"Build Index: {scene.buildIndex}");
                GUILayout.Label($"Loaded: {scene.isLoaded}");
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label("=== AVAILABLE GAME MODES ===", UnityEditor.EditorStyles.boldLabel);

            if (_networkManager.GameModeDatabase)
            {
                foreach (var gm in _networkManager.GameModeDatabase.gameModes)
                {
                    if (!gm) continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Mode: {gm.Id}");
                    GUILayout.Label($"Scenes: {string.Join(", ", gm.SceneIndices)}");
                    GUILayout.Label($"Max Players: {gm.MaxPlayers}");
                    if (GUILayout.Button("Load", GUILayout.Width(50)))
                        _networkManager.JoinGame(gm.Id);
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10);

            if (_networkManager.Runner)
            {
                GUILayout.Label("=== FUSION WORLD ===", UnityEditor.EditorStyles.boldLabel);
                GUILayout.Label($"Scene Authority: {_networkManager.Runner.IsSceneAuthority}");
                GUILayout.Label($"Scene Manager Busy: {_networkManager.Runner.IsSceneManagerBusy}");
                GUILayout.Label($"Simulation Time: {_networkManager.Runner.SimulationTime:F2}");
                GUILayout.Label($"Render Time: {_networkManager.Runner.RemoteRenderTime:F2}");
            }
        }

        private void DrawErrorsTab()
        {
            GUILayout.Label("=== ERROR LOG ===", UnityEditor.EditorStyles.boldLabel);

            if (_errorLog.Count == 0)
                GUILayout.Label("No errors logged.");
            else
            {
                GUILayout.Label($"Total Errors: {_errorLog.Count}");
                GUILayout.Space(5);
                for (int i = _errorLog.Count - 1; i >= 0; i--)
                    GUILayout.Label(_errorLog[i], UnityEditor.EditorStyles.helpBox);
            }

            GUILayout.Space(10);
            if (_networkManager.LastResult != null && !_networkManager.LastResult.Ok)
            {
                GUILayout.Label("=== LAST CONNECTION ERROR ===", UnityEditor.EditorStyles.boldLabel);
                GUILayout.Label($"Reason: {_networkManager.LastResult.ShutdownReason}");
                GUILayout.Label($"Message: {_networkManager.LastResult.ErrorMessage}");
                GUILayout.Label($"Friendly: {_networkManager.GetCurrentFailReason()}");
            }
        }

        private void DrawNetworkTab()
        {
            GUILayout.Label("=== NETWORK LOG ===", UnityEditor.EditorStyles.boldLabel);

            if (_networkLog.Count == 0)
                GUILayout.Label("No network events logged.");
            else
            {
                GUILayout.Label($"Total Network Events: {_networkLog.Count}");
                GUILayout.Space(5);
                for (int i = _networkLog.Count - 1; i >= 0; i--)
                    GUILayout.Label(_networkLog[i], UnityEditor.EditorStyles.helpBox);
            }
        }

        private void DrawStatsTab()
        {
            GUILayout.Label("=== NETWORK STATISTICS ===", UnityEditor.EditorStyles.boldLabel);

            if (_networkManager.Runner)
            {
                var stats = _networkManager.Runner.GetNetworkStats();

                GUILayout.Label($"RTT: {stats.currentRTT:F3}s ({stats.currentRTT * 1000:F0}ms)");
                GUILayout.Label($"Is Laggy: {_networkManager.IsLaggyConnection}");
                GUILayout.Label($"Packets Sent: {stats.sentPacketsPerSec}");
                GUILayout.Label($"Packets Received: {stats.receivedPacketsPerSec}");

                GUILayout.Space(10);
                GUILayout.Label("=== FUSION RUNNER ===", UnityEditor.EditorStyles.boldLabel);
                GUILayout.Label($"Tick: {_networkManager.Runner.Tick}");
                GUILayout.Label($"Delta Time: {_networkManager.Runner.DeltaTime:F4}");
                GUILayout.Label($"Input Buffer Size: {_networkManager.Runner.Config.Simulation.InputTransferMode}");

                if (_networkManager.Runner.GameMode == Fusion.GameMode.Shared)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("=== SHARED MODE ===", UnityEditor.EditorStyles.boldLabel);
                    GUILayout.Label($"Is Master Client: {_networkManager.Runner.IsSharedModeMasterClient}");
                }
            }
            else
                GUILayout.Label("No active NetworkRunner");

            GUILayout.Space(10);

            // memory usage
            GUILayout.Label("=== MEMORY ===", UnityEditor.EditorStyles.boldLabel);
            GUILayout.Label($"Total Memory: {(System.GC.GetTotalMemory(false) / 1024f / 1024f):F1} MB");
            GUILayout.Label($"Heap Size: {(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f):F1} MB");

            if (GUILayout.Button("Force GC", GUILayout.Height(25)))
                System.GC.Collect();
        }

        private void DrawConnectionTab()
        {
            GUILayout.Label("=== NETWORK CONFIG ===", UnityEditor.EditorStyles.boldLabel);

            if (_networkManager._networkConfig)
            {
                var cfg = _networkManager._networkConfig;
                GUILayout.Label($"Connection Timeout: {cfg.ConnectionTimeoutSeconds}s");
                GUILayout.Label($"Max Retries: {cfg.MaxRetries}");
                GUILayout.Label($"Base Retry Delay: {cfg.BaseRetryDelaySeconds}s");
                GUILayout.Label($"Max Retry Delay: {cfg.MaxRetryDelaySeconds}s");

                GUILayout.Space(5);
                GUILayout.Label("Computed backoff delays:");
                if (cfg.MaxRetries == 0)
                    GUILayout.Label("  No retries configured (MaxRetries = 0)", UnityEditor.EditorStyles.helpBox);
                else
                {
                    for (int i = 0; i < cfg.MaxRetries; i++)
                        GUILayout.Label($"  Retry {i + 1}: {cfg.GetRetryDelay(i)}s");
                }
            }
            else
            {
                GUILayout.Label("No NetworkConfig assigned — using fallback defaults (30s timeout, 4 retries, 10/60s delays)",
                    UnityEditor.EditorStyles.helpBox);
            }

            GUILayout.Space(10);
            GUILayout.Label("=== RETRY STATE ===", UnityEditor.EditorStyles.boldLabel);
            GUILayout.Label($"Is Retrying: {_networkManager.IsRetrying}");
            GUILayout.Label($"Current Retry Attempt: {_networkManager.CurrentRetryAttempt}");
            GUILayout.Label($"Join In Progress: {_networkManager.JoinInProgress}");
            GUILayout.Label($"State: {_networkManager.NetworkState}");

            GUILayout.Space(10);
            GUILayout.Label("=== SIMULATION TOGGLES ===", UnityEditor.EditorStyles.boldLabel);

            GUI.backgroundColor = _networkManager.SimulateTimeout ? Color.yellow : Color.white;
            if (GUILayout.Button($"Simulate Timeout: {(_networkManager.SimulateTimeout ? "ON" : "OFF")}", GUILayout.Height(30)))
                _networkManager.SimulateTimeout = !_networkManager.SimulateTimeout;
            GUI.backgroundColor = Color.white;
            GUILayout.Label("Fires once — hangs the next join past the timeout, then auto-clears.", UnityEditor.EditorStyles.helpBox);

            GUILayout.Space(5);
            GUI.backgroundColor = _networkManager.SimulateFailure ? Color.yellow : Color.white;
            if (GUILayout.Button($"Simulate Failure: {(_networkManager.SimulateFailure ? "ON" : "OFF")}", GUILayout.Height(30)))
                _networkManager.SimulateFailure = !_networkManager.SimulateFailure;
            GUI.backgroundColor = Color.white;
            GUILayout.Label("Returns ConnectionRefused N times (see Fail Count below), then auto-clears.", UnityEditor.EditorStyles.helpBox);

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label($"Failure Fail Count: {_networkManager.SimulateFailCount}", GUILayout.Width(150));
            if (GUILayout.Button("-", GUILayout.Width(30)) && _networkManager.SimulateFailCount > 0)
                _networkManager.SimulateFailCount--;
            if (GUILayout.Button("+", GUILayout.Width(30)))
                _networkManager.SimulateFailCount++;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("=== TEST ACTIONS ===", UnityEditor.EditorStyles.boldLabel);
            if (GUILayout.Button("Test: Join Online (BattleRoyale)", GUILayout.Height(30)))
                _networkManager.JoinGameEx(Teabag.GameMode.NetworkGameModeIds.BattleRoyale, string.Empty, true);
            if (GUILayout.Button("Test: Join Online (TestWeapons)", GUILayout.Height(30)))
                _networkManager.JoinGameEx(Teabag.GameMode.NetworkGameModeIds.TestWeapons, string.Empty, true);
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnFusionLogMessageReceived;
        }
    }
}
#endif
