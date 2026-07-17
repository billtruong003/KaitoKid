using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Linq;
using _Shmackle.Minigames;
using _Shmackle.Minigames.PropHunt;

public class EffectHub : NetworkBehaviour
{
    [Header("Effect Controllers")]
    [FoldoutGroup("Freeze Effect Controllers")]
    [SerializeField]
    private FreezeController freezeController;
    [FoldoutGroup("Freeze Effect Controllers")]
    [SerializeField]
    private float defaultFreezeTransitionDuration = 1.5f;


    [FoldoutGroup("X-ray Effect Controllers")]
    [SerializeField]
    private XRayController xrayController;
    [FoldoutGroup("Outline Effect Controllers")]
    [SerializeField]
    private OutlineController outlineController;


    [FoldoutGroup("Gert Effect Controllers")]
    [SerializeField]
    private GertController gertController;
    [SerializeField]
    private float defaultGertTransitionDuration = 2.0f;

    [FoldoutGroup("Butt Spank")]
    [SerializeField]
    private PlayerJiggleNetworkRelay playerJiggleController;

    [FoldoutGroup("Party Effect")] public GameObject partyEffect;
    [FoldoutGroup("Party Effect")] public GameObject friendUpChargeEffect;
    [FoldoutGroup("Party Effect")] public ParticleSystem friendUpExplosionEffect;

    [FoldoutGroup("Metro Train")] public GameObject readyEffect;

    [FoldoutGroup("BloodJman")] public GameObject bloodJmanTattoo;
    [FoldoutGroup("BloodJman")] public GameObject survivalTattoo;
    [FoldoutGroup("BloodJman")] public GameObject stunFaceHover;
    [FoldoutGroup("BloodJman")] public BillProgress stunProgressIndicator;

    [FoldoutGroup("SnowInteractive")] public GameObject snowInteractorEffect;

    private Tween _stunProgressTween;

    [FoldoutGroup("DoubleJump")] public GameObject impactGroundEffect;

    private void Awake()
    {
        if (PersistentSnowTrailManager.Instance)
        {
            ActivateSnowInteractorEffect();
        }
    }

    private readonly Dictionary<Renderer, Material> activePropOutlines = new();
    private List<Material> materialCache;
    public void SetOutlineOnProp(Renderer propRenderer, Material outlineMaterial)
    {
        if (outlineMaterial == null)
        {
            propRenderer.materials = propRenderer.sharedMaterials;
            activePropOutlines.Remove(propRenderer);
        }
        else
        {
            materialCache = propRenderer.sharedMaterials.ToList();
            materialCache.Add(outlineMaterial);
            propRenderer.materials = materialCache.ToArray();
            activePropOutlines[propRenderer] = outlineMaterial;
        }
    }

    #region SnowInteractor
    public void ActivateSnowInteractorEffect()
    {
        if (snowInteractorEffect != null)
        {
            snowInteractorEffect.SetActive(true);
        }
    }
    #endregion

    #region Public Gert API (Local)
    public void ApplyGertEffect(bool enable)
    {
        ApplyGertEffect(enable, defaultGertTransitionDuration);
    }

    public void ApplyGertEffect(bool enable, float duration)
    {
        if (gertController == null)
        {
            Debug.LogWarning("[EffectHub] GertController chưa được gán.", this);
            return;
        }

        if (enable)
        {
            gertController.EnableGert(duration);
        }
        else
        {
            gertController.DisableGert(duration);
        }
    }

    #endregion
    #region Public Freeze API (Networked)
    [Button]
    [ContextMenu("Activate Global Freeze")]
    public void ActivateGlobalFreeze()
    {
        RPC_BroadcastFreezeCommand(true, defaultFreezeTransitionDuration);
    }

    [Button]
    [ContextMenu("Deactivate Global Freeze")]
    public void DeactivateGlobalFreeze()
    {
        RPC_BroadcastFreezeCommand(false, defaultFreezeTransitionDuration);
    }
    #endregion

    #region Public Outline API (Local)
    [Button]
    public void ActivateOutlinePlayerOnSelf()
    {
        if (outlineController != null)
        {
            outlineController.SetOutline(OutlineController.OutlineType.Player);
        }
    }

    [Button("Deactivate Outline")]
    public void DeactiveOutline()
    {
        if (outlineController != null)
        {
            outlineController.SetOutline(OutlineController.OutlineType.None);
        }
    }

    [Button]
    public void ActivateOutlineBloodJmanOnSelf()
    {
        if (outlineController != null)
        {
            outlineController.SetOutline(OutlineController.OutlineType.BloodJman);
        }
    }
    [Button]
    public void ActivateOutlineObjectReactOnSelf()
    {
        if (outlineController != null)
        {
            outlineController.SetOutline(OutlineController.OutlineType.ObjectReact);
        }
    }
    #endregion
    #region Public X-Ray API (Local)

    public void SetXRayForPropHuntHunters(bool enable)
    {
        //Debug.Log($"[XRayDebugger-Hub] SetXRayForPropHuntHunters called with enable: {enable}");

        if (!Object.HasInputAuthority)
        {
            //Debug.LogWarning("[XRayDebugger-Hub] Call rejected: Not from local player (Input Authority).");
            return;
        }
        if (ShmackleGameManager.Instance == null)
        {
            //Debug.LogError("[XRayDebugger-Hub] ShmackleGameManager.Instance is NULL.");
            return;
        }

        var allPlayers = ShmackleGameManager.Instance.shmacklePlayerList;
        if (allPlayers == null || allPlayers.Count == 0)
        {
            //Debug.LogWarning("[XRayDebugger-Hub] ShmacklePlayerList is null or empty. No players to process.");
            return;
        }

        //Debug.Log($"[XRayDebugger-Hub] Processing {allPlayers.Count} players in the list...");

        foreach (var player in allPlayers)
        {
            if (player == null || player.networkPlayer == null)
            {
                //Debug.LogWarning("[XRayDebugger-Hub] Found a null player in the list. Skipping.");
                continue;
            }

            if (player.networkPlayer.Object.HasInputAuthority)
            {
                //Debug.Log($"[XRayDebugger-Hub] Skipping local player: {player.playerName}");
                continue;
            }

            var propHuntModule = player.networkPlayer.GetComponentInParent<PropHuntPlayerModule>();

            if (propHuntModule == null)
            {
                //Debug.LogWarning($"[XRayDebugger-Hub] Could not find PropHuntPlayerModule on player: {player.playerName}. Skipping.");
                continue;
            }

            //Debug.Log($"[XRayDebugger-Hub] Checking player: {player.playerName}. Their role is: {propHuntModule.currentRole}");

            if (propHuntModule.currentRole == PropHuntPlayerModule.PlayerRole.Hunter)
            {
                if (player.networkPlayer.EffectHub != null)
                {
                    //Debug.Log($"[XRayDebugger-Hub] SUCCESS: Found Hunter '{player.playerName}'. Applying X-Ray state: {enable}");
                    player.networkPlayer.EffectHub.SetXRayState(enable, XRayController.XRayType.Hunter);
                }
                else
                {
                    //Debug.LogError($"[XRayDebugger-Hub] FAILURE: Player '{player.playerName}' is a Hunter, but their EffectHub is NULL.");
                }
            }
        }
    }

    public void SetXRayState(bool enable, XRayController.XRayType type)
    {
        // Đây là trạm gác cuối cùng, xác nhận lệnh đã đến đúng đích
        if (xrayController != null)
        {
            //Debug.Log($"[XRayDebugger-TARGET] Executing SetXRayState({enable}, {type}) on player '{gameObject.name}'");
            xrayController.SetXRayActive(enable, type);
        }
        else
        {
            //Debug.LogError($"[XRayDebugger-TARGET] FAILED: XRayController is NULL on player '{gameObject.name}'");
        }
    }

    [Button]
    public void ActivateStandardXRayOnOthers() => BroadcastLocalXRayCommand(true, XRayController.XRayType.Standard);

    [Button]
    public void DeactivateStandardXRayOnOthers() => BroadcastLocalXRayCommand(false, XRayController.XRayType.Standard);

    [Button]
    public void ActivateBloodJmanXRayOnOthers() => BroadcastLocalXRayCommand(true, XRayController.XRayType.BloodJman);

    [Button]
    public void DeactivateBloodJmanXRayOnOthers() => BroadcastLocalXRayCommand(false, XRayController.XRayType.BloodJman);

    public void ActivateBloodJmanXRayOnOthers(List<int> playerIdsInGame) => BroadcastLocalXRayBloodJmanCommand(true, playerIdsInGame, XRayController.XRayType.BloodJman);

    public void DeactivateBloodJmanXRayOnOthers(List<int> playerIdsInGame) => BroadcastLocalXRayBloodJmanCommand(false, playerIdsInGame, XRayController.XRayType.BloodJman);

    public void DeactivateBloodJmanXrayOnSelf() => xrayController.SetXRayActive(false, XRayController.XRayType.BloodJman);

    #endregion

    #region Public Outline API (Networked)

    public void SetLocalOutline(OutlineController.OutlineType type)
    {
        if (outlineController == null) return;
        outlineController.SetOutline(type);
    }

    public void DeactiveOutlineNetworked()
    {
    }

    public void SetNetworkedOutline(OutlineController.OutlineType type)
    {
        RPC_SetOutlineState(type);
    }
    #endregion

    #region RPCs and Private Logic
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetOutlineState(OutlineController.OutlineType type, RpcInfo info = default)
    {
        if (outlineController != null)
        {
            outlineController.SetOutline(type);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DeactiveOutlineState(RpcInfo info = default)
    {
        if (outlineController != null)
        {
            outlineController.SetOutline(OutlineController.OutlineType.None);
        }
    }


    private void BroadcastLocalXRayCommand(bool enable, XRayController.XRayType type)
    {
        if (Object == null)
            return;
        if (!Object.HasInputAuthority) return;

        foreach (var player in ShmackleGameManager.Instance.shmacklePlayerList)
        {
            if (player.networkPlayer == null || player.networkPlayer.HasInputAuthority)
            {
                continue;
            }

            var otherPlayerXRayController = player.networkPlayer.GetComponentInChildren<XRayController>();
            if (otherPlayerXRayController != null)
            {
                otherPlayerXRayController.SetXRayActive(enable, type);
            }
        }
    }

    private void BroadcastLocalXRayBloodJmanCommand(bool enable, List<int> playerIdsInGame, XRayController.XRayType type)
    {
        if (!Object.HasInputAuthority) return;

        foreach (var player in ShmackleGameManager.Instance.shmacklePlayerList)
        {
            if (player.networkPlayer == null || player.networkPlayer.HasInputAuthority)
            {
                continue;
            }

            if (playerIdsInGame.Contains(player.playerId) == false)
            {
                continue;
            }

            var otherPlayerXRayController = player.networkPlayer.GetComponentInChildren<XRayController>();
            if (otherPlayerXRayController != null)
            {
                otherPlayerXRayController.SetXRayActive(enable, type);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_BroadcastFreezeCommand(bool enable, float duration, RpcInfo info = default)
    {
        var senderPlayerRef = info.Source;

        foreach (var player in ShmackleGameManager.Instance.shmacklePlayerList)
        {
            if (player.playerId == senderPlayerRef.PlayerId) continue;

            if (player.networkPlayer != null)
            {
                var targetHub = player.networkPlayer.GetComponent<EffectHub>();
                targetHub?.ApplyLocalFreezeEffect(enable, duration);
            }
        }
    }

    public void ApplyLocalFreezeEffect(bool enable, float duration)
    {
        if (freezeController == null) return;

        if (enable)
        {
            freezeController.EnableFreeze(duration);
        }
        else
        {
            freezeController.DisableFreeze(duration);
        }
    }

    public void EnableFreezeEffectUI(bool isEnable)
    {
        freezeController.EnableFreezeUI(isEnable);
    }

    public void ApplyButtSpankEffect(Vector3 worldSpaceDirection)
    {
        if (playerJiggleController == null) return;

        playerJiggleController.Rpc_ApplyExternalImpact(worldSpaceDirection);
    }

    #endregion

    #region BloodJMan
    public void StartStunVisuals(float duration)
    {
        if (stunFaceHover != null)
        {
            stunFaceHover.SetActive(true);
        }

        if (stunProgressIndicator != null)
        {
            stunProgressIndicator.gameObject.SetActive(true);
            _stunProgressTween?.Kill();
            stunProgressIndicator.SetNormalizedProgress(1f);
            _stunProgressTween = DOTween.To(
                () => 1f,
                x => stunProgressIndicator.SetNormalizedProgress(x),
                0f,
                duration
            ).SetEase(Ease.Linear);
        }
    }

    public void StopStunVisuals()
    {
        if (stunFaceHover != null)
        {
            stunFaceHover.SetActive(false);
        }

        if (stunProgressIndicator != null)
        {
            stunProgressIndicator.gameObject.SetActive(false);
            _stunProgressTween?.Kill();
            stunProgressIndicator.SetNormalizedProgress(0f);
        }
    }
    public void SetBloodJmanStun(bool isStunned)
    {
        if (stunFaceHover != null)
        {
            stunFaceHover.SetActive(isStunned);
        }
    }
    #endregion

    #region Validation
    private void OnValidate()
    {
        if (freezeController == null)
        {
            freezeController = GetComponentInChildren<FreezeController>(true);
        }
        if (xrayController == null)
        {
            xrayController = GetComponentInChildren<XRayController>(true);
        }
        if (outlineController == null)
        {
            outlineController = GetComponentInChildren<OutlineController>(true);
        }
        if (gertController == null)
        {
            gertController = GetComponentInChildren<GertController>(true);
        }
    }
    #endregion
}