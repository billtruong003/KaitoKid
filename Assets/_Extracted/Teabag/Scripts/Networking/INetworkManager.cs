using System;
using Cysharp.Threading.Tasks;
using Fusion;
using Teabag.GameMode;
using UnityEngine;

namespace Teabag.Networking
{
    /// <summary>
    /// Interface for managing Fusion networking lifecycle, room state, and game-mode transitions.
    /// Replaces direct static access to NetworkManager.
    /// Resolved via <see cref="Squido.JungleXRKit.Core.ServiceLocator"/>.Get&lt;INetworkManager&gt;().
    /// </summary>
    public interface INetworkManager
    {
        // ── Runner & Instance ──

        /// <summary>Whether a NetworkRunner currently exists.</summary>
        bool HasRunner { get; }

        /// <summary>The active Fusion NetworkRunner. May be null when between sessions.</summary>
        NetworkRunner Runner { get; set; }

        // ── Connection State ──

        /// <summary>Current state of the networking FSM (NONE, JOINING, IN_ROOM, LEAVING, GHOST).</summary>
        State NetworkState { get; set; }

        /// <summary>True when the runner is active and running.</summary>
        bool InRoom { get; }

        /// <summary>True when connected in Shared (networked) mode.</summary>
        bool InNetworkedRoom { get; }

        /// <summary>True when a Runner reference exists.</summary>
        bool IsConnected { get; }

        /// <summary>True when this client is the shared-mode master or in single-player.</summary>
        bool IsMaster { get; }

        /// <summary>True when round-trip time exceeds the lag threshold.</summary>
        bool IsLaggyConnection { get; }

        /// <summary>True during room join/leave transitions or scene manager activity.</summary>
        bool IsLoading { get; }

        // ── Metrics / Timestamps ──

        /// <summary>Global player count from the CCU API.</summary>
        int PlayerCount { get; set; }

        /// <summary>UTC time when the current room was joined.</summary>
        DateTime TimeJoinedRoom { get; set; }

        /// <summary>Duration spent in the current room.</summary>
        TimeSpan TimeSpentInRoom { get; }

        /// <summary>Result of the last StartGame call.</summary>
        StartGameResult LastResult { get; set; }

        /// <summary>Parameters of the most recent join request.</summary>
        (string gameMode, string sessionName, bool online) LastJoinRequest { get; }

        // ── Room Info ──

        /// <summary>Current room info, or null if not in a networked room.</summary>
        GorillaRoomInfo CurrentRoom { get; }

        /// <summary>Current room info, never null (returns empty stub if not in a room).</summary>
        GorillaRoomInfo CurrentRoomSafe { get; }

        /// <summary>Game mode identifier of the current room.</summary>
        string CurrentGameMode { get; }

        // ── Game Mode Queries ──

        /// <summary>Looks up a GameModeSo by its identifier.</summary>
        GameModeSo GetGameMode(string gameMode);

        /// <summary>The GameModeSo data for the current game mode.</summary>
        GameModeSo CurrentGameModeData { get; }

        /// <summary>True if the current game mode is Battle Royale.</summary>
        bool IsBattleRoyale { get; }

        /// <summary>True if the current game mode is Bootcamp.</summary>
        bool IsBootcamp { get; }

        /// <summary>True if the current game mode is Shop.</summary>
        bool IsShop { get; }

        /// <summary>Returns the gorilla prefab for the current game mode, falling back to default.</summary>
        NetworkObject GorillaPrefab { get; }

        // ── Scene Helpers ──

        /// <summary>Checks whether a scene with the given build index is loaded.</summary>
        bool IsSceneLoaded(int sceneIndex);

        /// <summary>Checks whether a given scene path is marked to be ignored during global Unload all operations.</summary>
        bool IsSceneIgnoredForUnload(string scenePath);

        // ── Retry State ──

        /// <summary>True while a join retry backoff delay is in progress.</summary>
        bool IsRetrying { get; }

        /// <summary>Current retry attempt index (0 = first attempt).</summary>
        int CurrentRetryAttempt { get; }

        // ── Matchmaking ──

        /// <summary>Match type for the next FreeForAll join (0=FFA, 1=Duo, 2=Squads). Reset after use.</summary>
        int PendingMatchType { get; set; }

        /// <summary>Shuttle index the player boarded (0, 1, 2). Used by GameLoopManager to position the correct shuttle on landing.</summary>
        int PendingShuttleIndex { get; set; }

        /// <summary>Player's local-space offset from the shuttle transform at departure. Restored after transition.</summary>
        Vector3 PendingShuttleLocalOffset { get; set; }
        /// <summary>Source shuttle rotation at departure. Used to compute rotation delta for player orientation.</summary>
        Quaternion PendingShuttleRotation { get; set; }

        /// <summary>Explicit session name for the next JoinGameEx call. When set, all clients join the same room. Cleared after use.</summary>
        string PendingSessionName { get; set; }

        // ── Join / Leave ──
        void JoinGame(GameModeSo gameMode, string sessionName = "");
        /// <summary>Joins a game room (fire-and-forget).</summary>
        void JoinGame(string gameMode, string sessionName = "");

        /// <summary>Joins a game room and waits for the result.</summary>
        UniTask<StartGameResult> JoinGameAsync(string gameMode, string sessionName = "");

        void JoinGameEx(GameModeSo gameMode, string sessionName = "", bool online = true);

        /// <summary>Joins a game room with explicit online/offline control (fire-and-forget).</summary>
        void JoinGameEx(string gameMode, string sessionName = "", bool online = true);

        /// <summary>Joins a game room with explicit online/offline control and waits for the result.</summary>
        UniTask<StartGameResult> JoinGameExAsync(GameModeSo gameMode, string sessionName = "", bool online = true, bool loadScene = true, bool fade = true);

        /// <summary>Joins a game room with explicit online/offline control and waits for the result.</summary>
        UniTask<StartGameResult> JoinGameExAsync(string gameMode, string sessionName = "", bool online = true, bool loadScene = true, bool fade = true);

        /// <summary>Leaves the current room (fire-and-forget).</summary>
        void LeaveGame();

        /// <summary>Leaves the current room and waits for completion.</summary>
        UniTask LeaveGameAsync();

        // ── Utility ──

        /// <summary>Fetches the global player count from the CCU API.</summary>
        UniTask<int> GetPlayerCountAsync();

        /// <summary>Returns a human-readable description of the last join failure, or empty string if OK.</summary>
        string GetCurrentFailReason();

        /// <summary>Generates a random session name that passes the bad-word filter.</summary>
        string GenerateSessionName();

        /// <summary>Clears any message currently shown on the fade screen overlay.</summary>
        void ClearFadeMessage();

        // ── Events ──

        /// <summary>Raised after a room join attempt completes.</summary>
        event Action<StartGameResult> OnJoinRoom;

        // ── Spawn-teleport coordination ──

        /// <summary>
        /// Completion source created at the start of each <see cref="JoinGameExAsync(string, string, bool, bool, bool)"/>
        /// call and signalled by the active game-mode manager (e.g. SpaceStationManager) once the
        /// post-spawn teleport + settle window has finished. NetworkManager awaits this before releasing
        /// the rig hold so the local rig stays kinematic from spawn through final positioning.
        /// Only awaited when <see cref="SpawnTeleportClaimed"/> is true. Null when no join is in flight.
        /// </summary>
        UniTaskCompletionSource SpawnTeleportTcs { get; }

        /// <summary>
        /// True once a game-mode manager has claimed responsibility for signalling
        /// <see cref="SpawnTeleportTcs"/>. JoinGameExAsync only blocks on the TCS when this is set,
        /// so empty-gameMode/offline joins (no spawn-teleport manager) don't waste time on the
        /// safety timeout. Reset per join.
        /// </summary>
        bool SpawnTeleportClaimed { get; }

        /// <summary>
        /// Called by a game-mode manager (e.g. SpaceStationManager.Spawned) to declare that it
        /// will signal post-spawn teleport completion via <see cref="SignalSpawnTeleportComplete"/>.
        /// </summary>
        void ClaimSpawnTeleport();

        /// <summary>
        /// Signals that the active game-mode manager has finished the post-spawn teleport (success or
        /// graceful failure). Idempotent — only the first call resolves the underlying TCS.
        /// </summary>
        void SignalSpawnTeleportComplete();

        // ── Prefabs / Assets (needed by GorillaRunner and scene setup) ──

        /// <summary>Prefab for the UserPropertyManager network object.</summary>
        NetworkObject UserPropertyManagerPrefab { get; }

        /// <summary>The default skybox material used when no game-mode skybox is set.</summary>
        Material DefaultSkybox { get; }

        /// <summary>The GameModeDb asset for game mode lookups.</summary>
        GameModeDb GameModeDatabase { get; }
    }
}
