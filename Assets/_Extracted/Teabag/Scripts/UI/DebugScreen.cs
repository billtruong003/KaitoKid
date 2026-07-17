using System.Linq;
using Fusion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Authentication;
using Teabag.Networking;
using Teabag.Player;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using Teabag.Core;
using Teabag.Gameplay;

namespace Teabag.UI
{
    public sealed class DebugScreen : MonoBehaviour
    {
        private static DebugScreen s_Instance;

        public static bool GodModeEnabled =>
#if UNITY_EDITOR
            s_Instance && s_Instance.m_GodModeToggle.isOn;
#else
            false;
#endif

        private const float ADD_TO_BUFFER_COOLDOWN = 1f;

        public bool IsDeveloper => PlayerData.inventory.InventoryContains("DEVBADGE");
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
        [SerializeField] private TextMeshPro m_PrivateOrPublicRoomText;

        [Header("Weapons")]
        [SerializeField] private NetworkObject m_PistolObject;
        [SerializeField] private NetworkObject m_RifleObject;
        [SerializeField] private NetworkObject m_BananaGunObject;
        [SerializeField] private NetworkObject m_C4ControllerObject;
        [SerializeField] private NetworkObject m_C4Object;
        [SerializeField] private NetworkObject m_ShotgunObject;
        [SerializeField] private NetworkObject m_GrenadeObject;
        [SerializeField] private NetworkObject m_ShieldObject;
        [SerializeField] private NetworkObject m_FryingPanObject;
        [SerializeField] private NetworkObject m_PearlObject;
        [SerializeField] private NetworkObject m_ShieldPotionObject;
        [SerializeField] private NetworkObject m_MissileObject;

        [Header("Ammo")]
        [SerializeField] private NetworkObject m_PistolAmmo;
        [SerializeField] private NetworkObject m_RifleAmmo;
        [SerializeField] private NetworkObject m_ShotgunAmmo;
        [SerializeField] private NetworkObject m_SniperAmmo;

        [Header("Graphs")]
        [SerializeField] private GorillaToggle[] m_GraphButtons;
        [SerializeField] private GraphUI m_Graph;

        [Header("Pages")]
        [SerializeField] private GameObject[] m_Pages;
        [SerializeField] private TextMeshPro m_PageText;

        [Header("UI")]
        [SerializeField] private GorillaToggle m_GodModeToggle;
        [SerializeField] private GorillaToggle m_FlyModeToggle;
        [SerializeField] private GorillaToggle m_SaveToInvToggle;
        [SerializeField] private TextMeshPro m_CurrAmountText;
        [SerializeField] private TextMeshPro m_StatsText;

        private readonly RollingFloatBuffer m_FpsBuffer = new RollingFloatBuffer(25);
        private readonly RollingFloatBuffer m_PingBuffer = new RollingFloatBuffer(25);
        private readonly RollingFloatBuffer m_GCTimeBuffer = new RollingFloatBuffer(25);
        private readonly RollingFloatBuffer m_DrawCallsBuffer = new RollingFloatBuffer(25);
        private readonly RollingFloatBuffer m_PhysicsTimeBuffer = new RollingFloatBuffer(25);

        private ProfilerRecorder m_GCRecorder;
        private ProfilerRecorder m_DrawCallsRecorder;

        private float m_LastAddToBufferTime;
        private int m_CurrentPage;
        private int m_AddAmmoAmount = 1;
        private int m_CurrentDataSet = -1;
        private INetworkManager _networkManager;
        private IGorillaService _gorillaService;

        private IHardwareRig LocalHardwareRig
        {
            get
            {
                if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                    return rigInfo.HardwareRig;
                return null;
            }
        }

        private void Awake()
        {
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
        }

        public void DataSet_ViewPressed(int index)
        {
#if UNITY_EDITOR
            bool toggleOff = (m_CurrentDataSet == index);
            m_CurrentDataSet = toggleOff ? -1 : index;
            m_Graph.ViewDataSet(m_CurrentDataSet);

            for (int i = 0; i < m_GraphButtons.Length; i++)
            {
                bool shouldBeOn = m_CurrentDataSet == i;
                m_GraphButtons[i].isOn = shouldBeOn;
                m_GraphButtons[i].OnChanged();
            }
#endif
        }

        private void Start()
        {
#if UNITY_EDITOR
            s_Instance = this;
            m_GodModeToggle.onOn.AddListener(() => GameServices.GodModeEnabled = true);
            m_GodModeToggle.onOff.AddListener(() => GameServices.GodModeEnabled = false);
            m_FlyModeToggle.onOn.AddListener(() => GameServices.FlyModeEnabled = true);
            m_FlyModeToggle.onOff.AddListener(() => GameServices.FlyModeEnabled = false);

            m_GCRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            m_DrawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            foreach (var toggle in m_GraphButtons)
                toggle.externallyControlled = true;

            m_Graph.AddDataSet(new GraphData(
                label: "FPS",
                getter: m_FpsBuffer.ToArray,
                color: Color.red));

            m_Graph.AddDataSet(new GraphData(
                label: "Ping",
                getter: m_PingBuffer.ToArray,
                color: Color.green));

            m_Graph.AddDataSet(new GraphData(
                label: "GC Time (ms)",
                getter: m_GCTimeBuffer.ToArray,
                color: new Color(0.5f, 0f, 0.5f))); // deep purple

            m_Graph.AddDataSet(new GraphData(
                label: "Draw Calls",
                getter: m_DrawCallsBuffer.ToArray,
                color: Color.yellow));

            m_Graph.AddDataSet(new GraphData(
                label: "Physics Time (ms)",
                getter: m_PhysicsTimeBuffer.ToArray,
                color: new Color(1f, 0.5f, 0f))); // orange

            m_Graph.ShowGraph();
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            if ((Time.time - m_LastAddToBufferTime) > ADD_TO_BUFFER_COOLDOWN)
            {
                m_LastAddToBufferTime = Time.time;

                // --- FPS ---
                m_FpsBuffer.Add(1f / Time.unscaledDeltaTime);

                // --- GC Time (bytes allocated this frame in KB) ---
                m_GCTimeBuffer.Add(m_GCRecorder.LastValue / 1024f);

                // --- Draw Calls ---
                m_DrawCallsBuffer.Add(m_DrawCallsRecorder.LastValue);

                // --- Physics Time ---
                m_PhysicsTimeBuffer.Add(Time.fixedDeltaTime * 1000f);

                // --- Ping ---
                if (NetworkManager.Runner && NetworkManager.InRoom)
                {
                    // record in ms
                    m_PingBuffer.Add((float)NetworkManager.Runner.GetPlayerRtt(
                        NetworkManager.Runner.LocalPlayer) * 1000f);
                }

                m_StatsText.text = GetStatisticsText();
                m_Graph.RefreshGraph();
            }
#endif
        }

        private string GetStatisticsText()
        {
#if UNITY_EDITOR
            return "STATS:\n\n" +
                   $"FPS: {m_FpsBuffer.Current}, avg: {m_FpsBuffer.Average} (min: {m_FpsBuffer.Min()}, max: {m_FpsBuffer.Max()})\n" +
                   $"GC: {m_GCTimeBuffer.Current}, avg: {m_GCTimeBuffer.Average} (min: {m_FpsBuffer.Min()}, max: {m_GCTimeBuffer.Max()})\n" +
                   $"DC: {m_DrawCallsBuffer.Current}, avg: {m_DrawCallsBuffer.Average} (min: {m_DrawCallsBuffer.Min()}, max: {m_DrawCallsBuffer.Max()})\n" +
                   $"PHY: {m_PhysicsTimeBuffer.Current}, avg: {m_PhysicsTimeBuffer.Average} (min: {m_PhysicsTimeBuffer.Min()}, max: {m_PhysicsTimeBuffer.Max()})\n" +
                   $"LAT: {m_PingBuffer.Current}, avg: {m_PingBuffer.Average} (min: {m_PingBuffer.Min()}, max: {m_PingBuffer.Max()})\n\n" +
                   $"RM: {NetworkManager.CurrentRoom},\n" +
                   $"NS: {NetworkManager.NetworkState}\n" +
                   $"PC: {NetworkManager.PlayerCount}\n" +
                   $"TJ: {NetworkManager.TimeJoinedRoom}";
#endif
            return string.Empty;
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            m_GCRecorder.Dispose();
            m_DrawCallsRecorder.Dispose();
#endif
        }

        public void RoomOptions_OnPrivateOrPublicRoomPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            NetworkManager.CurrentRoom.SetIsVisible(
                !NetworkManager.CurrentRoom.IsPrivate);
            m_PrivateOrPublicRoomText.text = NetworkManager.CurrentRoom.IsPrivate ? "PUBLIC ROOM" : "PRIVATE ROOM";
#endif
        }

        public void RoomOptions_OnReloadMapPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            var sceneRef = NetworkManager.Runner.GetSceneRef(NetworkManager.CurrentGameMode);
            LocalHardwareRig.LocomotionController.PlayerRigidbody.isKinematic = true;

            NetworkManager.Runner.UnloadScene(sceneRef).AddOnCompleted((t) =>
            {
                NetworkManager.Runner.LoadScene(
                    sceneRef,
                    // ReSharper disable once VariableHidesOuterVariable
                    LoadSceneMode.Additive).AddOnCompleted((t) =>
                    {
                        LocalHardwareRig.LocomotionController.PlayerRigidbody.isKinematic = false;
                    });
            });
#endif
        }

        public void RoomOptions_OnKickAllPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            if (!NetworkManager.Runner.IsServer)
            {
                GameLogger.Error("You cannot use KickAll because you are not server.");
                return;
            }

            foreach (var player in NetworkManager.Runner.ActivePlayers)
            {
                if (player.PlayerId != NetworkManager.Runner.LocalPlayer.PlayerId)
                    NetworkManager.Runner.Disconnect(player);
            }
#endif
        }

        public void RoomOptions_OnMeAsStateAuthPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            if (NetworkManager.Runner == null || NetworkManager.Runner.LocalPlayer.IsRealPlayer)
            {
                GameLogger.Error("Runner or local player is not initialized.");
                return;
            }

            if (!NetworkManager.Runner.IsSharedModeMasterClient)
            {
                GameLogger.Warning("State authority can only be reassigned in shared mode as the master client.");
                return;
            }

            // TODO Request rig authority through rigs register
#endif
        }

        public void RoomOptions_OnPrintRoomInfoPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            if (NetworkManager.Runner == null)
            {
                GameLogger.Error("Network Runner is not initialized.");
                return;
            }

            var runner = NetworkManager.Runner;
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("===== ROOM INFO =====");
            sb.AppendLine($"Game Mode: {runner.GameMode}");
            sb.AppendLine($"Session Name: {runner.SessionInfo?.Name ?? "N/A"}");
            sb.AppendLine($"Is Running: {runner.IsRunning}");
            sb.AppendLine($"Local Player: {runner.LocalPlayer}");
            sb.AppendLine($"Player Count: {runner.ActivePlayers.Count()}");
            sb.AppendLine($"Is Server: {runner.IsServer}");
            sb.AppendLine($"Is Shared Master: {runner.IsSharedModeMasterClient}");
            sb.AppendLine($"Scene: {runner.SceneManager.MainRunnerScene}");

            if (runner.SessionInfo != null && runner.SessionInfo.Region != null)
                sb.AppendLine($"Region: {runner.SessionInfo.Region}");

            sb.AppendLine("Players:");
            foreach (var player in runner.ActivePlayers)
            {
                sb.AppendLine($" - {player} {(player == runner.LocalPlayer ? "(Local)" : "")}");
            }

            sb.AppendLine("=====================");

            GameLogger.Info(sb.ToString());
#endif
        }

        public void RoomOptions_OnDespawnAllPressed()
        {
#if UNITY_EDITOR

#endif
        }

        public void RoomOptions_OnBringAllPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            var tpLocal = _gorillaService?.LocalGorilla as Gorilla;
            var tpGorillas = _gorillaService?.Gorillas;
            if (tpGorillas != null && tpLocal != null)
            {
                foreach (var gorillaEntry in tpGorillas)
                {
                    var gorilla = (Gorilla)gorillaEntry;
                    if (gorilla.Object.HasStateAuthority)
                    {
                        gorilla.transform.position = tpLocal.transform.position;
                    }
                }
            }
#endif
        }

        public void RoomOptions_OnSuicidePressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            (_gorillaService?.LocalGorilla as Gorilla)?.health?.Damage(byte.MaxValue, HitType.Head);
#endif
        }

        public void RoomOptions_KillAllPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            var killGorillas = _gorillaService?.Gorillas;
            if (killGorillas != null)
                foreach (var gorillaEntry in killGorillas)
                    ((Gorilla)gorillaEntry).health.Damage(byte.MaxValue, HitType.Head);
#endif
        }

        public void SpawnObject_PistolPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            InternalSpawnObject(m_PistolObject, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnObject_RiflePressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            InternalSpawnObject(m_RifleObject, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnObject_BananaGunPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            InternalSpawnObject(m_BananaGunObject, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnObject_C4Pressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            // spawn c4 before so we can detect the c4 as local players
            // for the c4 controller
            InternalSpawnObject(m_C4Object, transform.position, Quaternion.identity);
            InternalSpawnObject(m_C4ControllerObject, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnObject_ShotgunPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            InternalSpawnObject(m_ShotgunObject, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnObject_GrenadePressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            InternalSpawnObject(m_GrenadeObject, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnObject_ShieldPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            InternalSpawnObject(m_ShieldObject, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnObject_FryingPanPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            InternalSpawnObject(m_FryingPanObject, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnObject_PearlPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            InternalSpawnObject(m_PearlObject, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnObject_ShieldPotionPressed()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            InternalSpawnObject(m_ShieldPotionObject, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnObject_MissilePressed(int amount)
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            for (int i = 0; i < amount; i++)
                InternalSpawnObject(m_MissileObject, transform.position, Quaternion.Euler(Vector3.down));
#endif
        }

        public void OnPageNextPressed()
        {
            m_CurrentPage = Mathf.Clamp(m_CurrentPage + 1, 0, m_Pages.Length - 1);
            m_PageText.text = "PAGE " + (m_CurrentPage + 1);
            for (int i = 0; i < m_Pages.Length; i++)
                m_Pages[i].SetActive(i == m_CurrentPage);
        }

        public void OnPagePreviousPressed()
        {
            m_CurrentPage = Mathf.Clamp(m_CurrentPage - 1, 0, m_Pages.Length - 1);
            m_PageText.text = "PAGE " + (m_CurrentPage + 1);
            for (int i = 0; i < m_Pages.Length; i++)
                m_Pages[i].SetActive(i == m_CurrentPage);
        }

        public void SpawnAmmo_SpawnPistol()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            if (m_SaveToInvToggle.isOn)
                Backpack.myBackpack.AddNonGrabbable(new NonGrabbableBackpackItem
                {
                    name = "PistolAmmo",
                    amount = m_AddAmmoAmount,
                });
            else
                InternalSpawnObject(m_PistolAmmo, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnAmmo_SpawnRifle()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            if (m_SaveToInvToggle.isOn)
                Backpack.myBackpack.AddNonGrabbable(new NonGrabbableBackpackItem
                {
                    name = "AutoAmmo",
                    amount = m_AddAmmoAmount,
                });
            else
                InternalSpawnObject(m_RifleAmmo, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnAmmo_SpawnShotgun()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            if (m_SaveToInvToggle.isOn)
                Backpack.myBackpack.AddNonGrabbable(new NonGrabbableBackpackItem
                {
                    name = "ShotgunAmmo",
                    amount = m_AddAmmoAmount,
                });
            else
                InternalSpawnObject(m_ShotgunAmmo, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnAmmo_SpawnSniper()
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            if (m_SaveToInvToggle.isOn)
                Backpack.myBackpack.AddNonGrabbable(new NonGrabbableBackpackItem
                {
                    name = "SniperAmmo",
                    amount = m_AddAmmoAmount,
                });
            else
                InternalSpawnObject(m_SniperAmmo, transform.position, Quaternion.identity);
#endif
        }

        public void SpawnAmmo_SetSpawnAmount(int amount)
        {
#if UNITY_EDITOR
            if (!IsDeveloper)
            {
                GameLogger.Error("You cannot use this option because you are not a developer.");
                return;
            }

            m_AddAmmoAmount = amount;
            m_CurrAmountText.text = "CUR AMT: " + amount;
#endif
        }

        private void InternalSpawnObject(NetworkObject obj, Vector3 position, Quaternion quaternion)
        {
#if UNITY_EDITOR
            NetworkObject spawnedObj = NetworkManager.Runner.Spawn(obj,
                position: position + (Vector3.up * 3),
                rotation: quaternion,
                inputAuthority: (_gorillaService?.LocalGorilla as Gorilla)?.Object.InputAuthority ?? default);

            GrabbableRarity rarity = spawnedObj.GetComponent<GrabbableRarity>();
            if (rarity) rarity.rarity = Rarity.Legendary;
#endif
        }
    }
}
