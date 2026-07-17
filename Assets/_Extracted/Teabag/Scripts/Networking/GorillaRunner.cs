using Fusion;
using Fusion.Sockets;
using Teabag.GameMode;
using Teabag.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GorillaLocomotion;
using Squido.JungleXRKit.Core;
using Teabag.Networking.Extensions;
using Teabag.Core;

using UnityEngine;
using UnityEngine.SceneManagement;
using Teabag.Player;

namespace Teabag.Networking
{
    public class GorillaRunner : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static Material s_currentSkybox;
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
        private NetworkRunner m_Runner;
        private bool m_Unloading = false;
        private string m_LastGameMode;
        private bool m_HasLoaded = false;
        private INetworkManager _networkManager;

        private readonly List<int> m_scenesToUnload = new List<int>();

        void Awake()
        {
            GameLogger.Info(this, "Runner has been spawned");
            m_Runner = GetComponent<NetworkRunner>();
        }

        async void FixedUpdate()
        {
            try
            {
                if (m_Unloading || !m_Runner || !m_Runner.IsRunning || m_Runner.IsSceneManagerBusy || !m_Runner.IsSharedModeMasterClient || string.IsNullOrEmpty(NetworkManager.CurrentGameMode))
                    return;

                // Don't run scene cleanup while a game mode transition is in progress
                if (NetworkManager.NetworkState == State.JOINING)
                    return;

                // Don't clean up scenes until the initial scene load is complete —
                // LoadGameModeAsync handles scene management during first load.
                if (!m_HasLoaded)
                    return;

                m_scenesToUnload.Clear();

                // Skip scene cleanup when GameLoopManager manages additive scenes
                if (GameServices.SuppressSceneCleanup?.Invoke() ?? false)
                    return;

                // get current gamemode scenes
                GameModeSo gameModeData = NetworkManager.GetGameMode(NetworkManager.CurrentGameMode);
                HashSet<int> validScenes = gameModeData != null ? gameModeData.SceneIndices : new HashSet<int>();
                if (gameModeData != null && gameModeData.GameManagerPrefab && validScenes.Any(idx => NetworkManager.IsSceneLoaded(idx)))
                {
                    if (!(GameServices.GorillaGameManagerExists?.Invoke() ?? false) && m_Runner.IsSharedModeMasterClient)
                        SpawnGameManager();
                }

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);

                    // Do not forcefully unload the splash scenes during boot loading
                    if (NetworkManager.IsSceneIgnoredForUnload(scene.path))
                        continue;

                    int sceneIndex = scene.buildIndex;

                    if (sceneIndex > 0 && !validScenes.Contains(sceneIndex))
                        m_scenesToUnload.Add(sceneIndex);
                }

                // if we have scenes to unload, unload them
                if (m_scenesToUnload.Count > 0)
                {
                    GameLogger.Warning(this, $"Scenes to unload: {string.Join(',', m_scenesToUnload)}");
                    foreach (int scene in m_scenesToUnload)
                    {
                        GameLogger.Warning(this, $"Stale scene detected (buildIndex={scene}) — unloading");
                        m_Unloading = true;
                        await m_Runner.SceneManager.UnloadScene(m_Runner.SceneManager.GetSceneRef(SceneManager.GetSceneByBuildIndex(scene).path));
                        m_Unloading = false;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                GameLogger.Error(this, $"FixedUpdate error: {ex.Message}");
                m_Unloading = false;
            }
        }

        // ReSharper disable once Unity.IncorrectMethodSignature
        public void OnConnectedToServer(NetworkRunner runner)
        {

        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            GameLogger.Error(this, $"Connect failed (Reason={reason})");
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {

        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {

        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            GameLogger.Error(this, $"Disconnected from server (Reason={reason})");

            // Don't auto-reconnect during intentional transitions — the calling code handles it.
            // For unexpected disconnects, set NONE so the health check reports Unhealthy and the
            // FSM lifecycle triggers a full Bootstrap recovery.
            if (NetworkManager.NetworkState != State.JOINING && NetworkManager.NetworkState != State.LEAVING)
                NetworkManager.NetworkState = State.NONE;
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {

        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {

        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {

        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {

        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {

        }

        public static void SpawnGorilla()
        {
            var networkManager = ServiceLocator.Get<INetworkManager>();
            GameLogger.Info("Spawning player: " + networkManager.GorillaPrefab.name);
            networkManager.Runner.Spawn(networkManager.GorillaPrefab, Vector3.zero, Quaternion.identity);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log("DISABLED - Restore HandlePlayerLeft()?");
            // GorillaGameManager.instance?.HandlePlayerLeft(player);
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {

        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {

        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            GameLogger.Debug(this, $"Finished scene loading (CurrentGameMode={NetworkManager.CurrentGameMode}, m_HasLoaded={m_HasLoaded})");

            GameModeSo currentModeData = NetworkManager.GetGameMode(NetworkManager.CurrentGameMode);
            HashSet<int> currentModeScenes = currentModeData != null ? currentModeData.SceneIndices : new HashSet<int>();

            foreach (GameObject m in Mark.FindObjectsWithMark("ChestSpawn"))
                m.SetActive(false);
            foreach (GameObject m in Mark.FindObjectsWithMark("ChestFallSpawn"))
                m.SetActive(false);

            ServiceLocator.Get<IMapService>()?.TakeMapPicture?.Invoke();

            var targetSkybox = currentModeData != null && currentModeData.Skybox != null ? currentModeData.Skybox : NetworkManager.DefaultSkybox;
            SetCurrentSkybox(targetSkybox);

            if (currentModeScenes.Count > 0 && !currentModeScenes.All(idx => NetworkManager.IsSceneLoaded(idx)))
            {
                GameLogger.Debug(this, $"OnSceneLoadDone: not all game mode scenes loaded yet, deferring. gameModeScenes=[{string.Join(",", currentModeScenes)}]");
                return;
            }

            // All game-mode scenes are confirmed loaded. Ensure the active scene is a
            // game-mode scene so Fusion-spawned objects (gorilla, game manager) land in
            // the correct scene rather than a stale transition scene (e.g. Main).
            var firstScene = currentModeData?.Scenes.FirstOrDefault();
            if (firstScene != null)
                SceneManager.SetActiveScene(firstScene.LoadedScene);

            NetworkManager.ClearFadeMessage();
            // Do NOT FadeIn here — each game-mode manager owns its post-load sequence
            // (SpaceStationManager, GameLoopManager, WaitingZoneManager). An eager fade-in
            // here would start ramping alpha toward 0 before the manager's Spawned() fires,
            // revealing the world at the wrong pose (flicker) until the manager snaps back
            // to black. If a future mode has no manager, have that mode's own bootstrap
            // trigger the fade-in explicitly.
            if (m_HasLoaded) return;

            m_HasLoaded = true;

            // Guard against stale reference: the old gorilla may have been destroyed
            // when the previous runner shut down, but if UnregisterGorilla didn't fire
            // the service still holds a dead reference.  Unity's == null catches this.
            var gorillaService = ServiceLocator.Get<IGorillaService>();
            bool hasLocal = gorillaService.HasLocalGorilla
                            && gorillaService.LocalGorilla is UnityEngine.Object gorillaObj
                            && gorillaObj != null;
            GameLogger.Debug(this, $"OnSceneLoadDone: first load complete. HasLocalGorilla={hasLocal}");
            if (!hasLocal)
                SpawnGorilla();

            if (!UserPropertyManager.Instance && runner.LocalPlayer.PlayerId == 1 && NetworkManager.UserPropertyManagerPrefab)
                NetworkManager.Runner.Spawn(NetworkManager.UserPropertyManagerPrefab);

            if (runner.IsSceneAuthority)
            {
                GameLogger.Warning(this, "Attempting to spawn game mode after scene load");
                SpawnGameManager();
            }

            // Player teleport is driven by SpawnPointService.HandleSceneLoaded
            // (fires per scene load) and by the game-mode manager's Spawned() hook.
            bool suppressRespawn = GameServices.SuppressRespawnOnSceneLoad?.Invoke() ?? false;
            GameLogger.Info(this, $"OnSceneLoadDone: suppressRespawn={suppressRespawn}");
            if (!suppressRespawn)
            {
                ServiceLocator.Get<IMapService>()?.TakeMapPicture?.Invoke();
            }
            else
            {
                GameLogger.Info(this, "OnSceneLoadDone: TakeMapPicture SUPPRESSED");
            }
        }

        public static void SetCurrentSkybox(Material skybox)
        {
            RenderSettings.skybox = skybox;
            s_currentSkybox = skybox;
        }

        public static void SpawnGameManager()
        {
            var networkManager = ServiceLocator.Get<INetworkManager>();
            if (networkManager.NetworkState == State.LEAVING)
                return;

            NetworkObject gameManagerPrefab = networkManager.GetGameMode(networkManager.CurrentGameMode)?.GameManagerPrefab;
            if (gameManagerPrefab)
            {
                GameLogger.Debug("Spawning game mode");
                networkManager.Runner.Spawn(gameManagerPrefab);
            }
        }

        public async void OnSceneLoadStart(NetworkRunner runner)
        {
            GameLogger.Debug(this, "Starting scene load");
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {

        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            GameLogger.Warning(this, $"Runner shutdown (Reason={shutdownReason})");

            // During intentional transitions (JOINING/LEAVING), LoadGameModeAsync on the
            // new runner will handle scene management. Fire-and-forget unloads here would
            // race with the new runner's NetworkSceneManagerDefault and cause timeouts.
            bool isIntentionalTransition = NetworkManager.NetworkState == State.JOINING
                                        || NetworkManager.NetworkState == State.LEAVING;

            if (isIntentionalTransition)
                return;

            // Unexpected shutdown — unload game scenes so we don't leave stale scenes around.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (NetworkManager.IsSceneIgnoredForUnload(scene.path))
                    continue;

                if (scene.buildIndex > 0) _ = SceneManager.UnloadSceneAsync(scene.buildIndex);
            }

            // Clear bootstrap reload suppression so the FSM can properly reload
            // the bootstrap scene for a full restart during recovery.
            PostBootstrapSceneLoaderService.ShouldSuppressReload = null;

            // Set NONE so the health check reports Unhealthy, which triggers the
            // FSM to transition back and reload the Bootstrap scene for a full restart.
            // Do NOT auto-reconnect here — the FSM lifecycle handles recovery.
            NetworkManager.NetworkState = State.NONE;
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }
    }
}
