using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Authentication;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Teabag.Core
{
    /// <summary>
    /// Static delegate bridges for Assembly-CSharp services.
    /// Named assemblies call these; Assembly-CSharp wires them up at startup.
    /// </summary>
    [Obsolete("This class need complete refactor")]
    public static class GameServices
    {
        // ModMenu bridge — set by ModMenu on Awake
        public static Func<string, bool> IsModEnabled;

        // Debug/God mode — set by UI.DebugScreen
        public static bool GodModeEnabled;

        // VR Input — set by VRInputHandler (for CameraFade haptic override)
        public static float HapticMultiplier = 1f;

        // BananaBlimp bridges — set by BananaBlimp on Awake
        public static Action CloseBlimpDoors;
        public static bool JoinModded => ServiceLocator.Get<IDataPersistenceService>()?.LoadData<int>("JoinModded", 0) == 1;

        // ModerationUtils bridge — set by Assembly-CSharp startup
        public static Func<string, bool> CheckBadWord;

        // API bridge — set by Assembly-CSharp startup
        public static Func<UniTask<int>> GetPlayerCountAsync;

        // BananaBlimp location check — set by BananaBlimp on Awake
        public static Func<bool> IsInBlimp;

        // PopupManager bridge — set by PopupManager on startup
        public static Action<string, Vector3, float> DisplayPopup;
        public static Action<string, Vector3, Color, float> DisplayPopupColored;

        // DailyChallenges bridge — set by Assembly-CSharp startup
        public static Func<int, UniTask> ScoreChallengeAsync;

        // ScopeManager bridges — set by ScopeManager on startup
        public static Action<object> CreateScope;
        public static Action<object> RemoveScope;

        // BattleRoyaleManager bridges — set by BattleRoyaleManager on startup
        public static Func<int> GetKillCount;
        public static Func<int> GetTeabagRipCount;
        // Player-stats bridges used because BattleRoyaleManager / GorillaHealth can hit
        public static Action IncrementTeabagRipCount;
        public static Action<int, int, int> RecordMatchResult;
        public static Func<DateTime> GetMatchStartTime;
        public static Func<Transform> GetPlayerCamera;
        // Returns the number of alive players remaining (updated every fixed tick)
        public static Func<int> GetAlivePlayerCount;


        // LootArea bridge — set by Assembly-CSharp startup (wraps LootArea.GetAreaRarities)
        public static Func<Vector3, List<int>> GetLootAreaRarities;

        // SyncedTime bridge — set by SyncedTime on startup
        public static Func<DateTime> GetSyncedTime;

        // GorillaGameManager.OnDeath bridge — set by GorillaGameManager on startup
        // Parameters: (deadGorilla, killerGorilla) as objects to avoid type dependency
        public static Action<object, object> OnPlayerDeath;

        // ---- TeamManager bridges — set by TeamManager on Awake ----

        // Returns true if TeamManager.instance != null
        public static Func<bool> TeamManagerExists;

        // SharesTeam(Gorilla) — wraps TeamManager.SharesTeam(gorilla)
        public static Func<object, bool> SharesTeam;

        // GetTeamColour(int teamIndex) — returns Team.colour or Color.white
        public static Func<int, Color> GetTeamColour;

        // Team switched event — mirrors TeamManager.onTeamSwitched
        // Player assembly subscribes; TeamManager invokes through this bridge
        public static Action<object> OnTeamSwitched;

        // ---- Scope/FirearmDisplay bridges — set by ScopeManager on Awake ----

        // Fires the scope animation for a given firearm (calls Scope.GetScope(f).Fire())
        public static Action<object> ScopeFire;

        // Creates a FirearmDisplay on the firearm transform; returns the display as object
        public static Func<object, Transform, object> CreateFirearmDisplay;

        // Destroys FirearmDisplay child of a given Transform (called on weapon release)
        public static Action<Transform> DestroyFirearmDisplay;

        // ---- NetworkObjectsManager bridges — set by Assembly-CSharp on startup ----

        // Returns true if BattleRoyaleManager.instance exists
        public static Func<bool> BattleRoyaleManagerExists;

        // Calls Spawn() on all SpawnAtPosition objects in scene
        public static Action SpawnAllAtPosition;

        // ---- GorillaGameManager bridge — set by GorillaGameManager on Spawned ----

        // Returns true if GorillaGameManager.instance exists
        public static Func<bool> GorillaGameManagerExists;



        // ---- NetworkManager bridges — set by NetworkManager on startup ----

        // Returns the active NetworkRunner instance
        public static Func<NetworkRunner> GetRunner;

        // Returns current game mode name (e.g. "Horror")
        public static Func<string> GetCurrentGameMode;

        // Returns true if connected to a Photon room
        public static Func<bool> IsInRoom;

        // Returns true when connected in a networked (Shared mode) room
        public static Func<bool> IsInNetworkedRoom;

        // Returns true while a join/leave transition is in progress
        public static Func<bool> IsLoading;

        // Fire-and-forget join by game mode and session name — set by NetworkManager on startup
        public static Action<string, string> JoinGameWithCode;

        // ---- Phase 2 bridges — for Authentication/ moving to Core ----

        // Network state (AuthenticationUtils.cosmetics, SubmitKillsAsync)
        public static Func<bool> HasNetworkRunner;
        public static Func<bool> IsSceneManagerBusy;
        public static Func<bool> IsRoomModded;

        // Local gorilla (AuthenticationUtils.SetCosmetic, SetDisplayNameAsync, GetCosmeticsAsync)
        public static Action LoadLocalCosmetics;
        public static Action<string> SetLocalPlayerName;

        // UI refresh (AuthenticationUtils.GetCatalogAsync, GetCosmeticsAsync)
        public static Action RefreshCosmeticSelectors;
        public static Action RefreshPurchaseStands;

        // Pack bridge (AuthenticationUtils.GetCosmeticsAsync)
        public static Func<string, List<InventoryItem>> GetPackInventoryItems;

        // CosmeticUtils bridge (AuthenticationUtils.AddToInventory)
        public static Func<Cosmetic, bool> CosmeticExists;

        // LevelManager bridge (AuthenticationUtils.SubmitKillsAsync)
        public static Action<int, int, int> SetLevel;
        public static Func<int> GetObsoleteLevel;
        public static Func<int> GetObsoleteXp;

        // ---- Phase 3 bridges — for BattlePass/ moving to UI ----

        // Computer bridge (UIBattlePass loading state)
        public static Func<bool> IsComputerLoading;

        // ---- Phase 4 bridges — for Economy/ moving to Gameplay ----

        // IAPManager bridge (PackBuy, ValueCalculator)
        public static Func<string, UniTask<(bool isError, string formattedPrice)>> GetIAPProductAsync;
        public static Func<string, UniTask<bool>> PurchaseIAPAsync;

        // Platform bridge (PackBuy — Oculus.Platform.Core.IsInitialized)
        public static Func<bool> IsPlatformInitialized;

        // ---- Phase 5 bridges — for 0New/GameModes/ moving to Networking ----

        // BananaBlimp bridges (additional — OpenAllDoors, button.Handle, transform, ejecting)
        public static Action OpenBlimpDoors;
        public static Func<bool> BlimpExists;
        public static Func<Transform> GetBlimpTransform;
        public static Action<bool> BlimpButtonHandle;
        public static Action<bool> SetBlimpEjecting;
        public static Func<bool> GetBlimpIsInBlimp;


        // SettingsMenu bridges (for BattleRoyaleManager.OnDeath)
        public static Action EnableDrone;
        public static Action<string> OpenSettingsScreen;

        // Type-check bridges for Grabbable subclasses (for BattleRoyaleManager.CleanUp)
        public static Func<object, bool> IsBackpackType;
        public static Func<object, bool> IsMapType;
        public static Func<object, bool> IsLCKGrabbableType;

        // Chest bridges (for BattleRoyaleManager.SpawnChests + CleanUp)
        public static Action<NetworkRunner> DestroyAllChests;
        public static Action<NetworkObject, Transform> SetupSpawnedChest;

        // TeamManager bridge (for GorillaGameManager.StartEndLoop)
        public static Func<int> GetActiveTeamCount;

        // ---- WeaponManager bridges — set by WeaponManager on startup ----
        public static Func<(string, Rarity, Vector3, Vector3), bool> SpawnWeaponData;
        public static Func<(string, Vector3, Vector3), bool> SpawnObjectData;
        public static Func<(string, int, Vector3, Vector3), bool> SpawnAmmoForFirearm;
        public static Func<(string, Rarity), GameObject[]> GetWeaponData;
        public static Func<string, GameObject> GetAmmoFromType;
        public static Func<string, GameObject> GetObjectData;
        public static Func<IReadOnlyCollection<string>> GetSpawnableIds;

        // ---- Game Loop bridges (cross-assembly) ----
        // Used by Networking assembly (GorillaRunner, NetworkManager) which cannot reference IGameLoopService.
        // Set by GameLoopManager/SpaceStationManager on Spawned.
        public static Func<bool> SuppressSceneCleanup;
        public static Func<bool> SuppressRespawnOnSceneLoad;

        // ---- Debug / fly mode ----
        public static bool FlyModeEnabled;

        // ---- Game reset event ----
        public static Action OnGameReset;

        // ---- Backpack bridges ----
        public static Action<string, int> AddBackpackAmmo;
        public static Action BackpackDie;

        // ---- BR Feedback HUD bridges ----

        // Fired on every client when the local player receives damage.
        // Carries the attacker's world-space position so the direction indicator can orient itself.
        // Set by GorillaHealth; consumed by DamageDirectionIndicator.
        public static Action<Vector3> OnDamageTaken;

        // Fired on every client when any gorilla is eliminated.
        // (victimName, killerName) — killerName is "Environment" for zone deaths.
        // Set by GorillaHealth; consumed by KillFeedHUD.
        public static Action<string, string> OnEliminationEvent;

        // Set by PlayerPerkController; consumed by PerkNodeController.
        public static Action OnUpdatePerkEquipEvent;
    }
}
