using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using DG.Tweening;
using UnityEngine.XR;
using Sirenix.OdinInspector;
using UnityEngine.VFX;
using Random = UnityEngine.Random;
using _Shmackle.Minigames.BloodJman;
using Shmackle.Utils.CoroutinesTimer;

public class ShmackleKissDetectController : NetworkBehaviour
{
    private enum KissState { Idle, Aiming, Cooldown }

    [Networked] public NetworkBool IsEnabled { get; set; } = true;
    [Networked] private NetworkBool IsKissing { get; set; }

    // References
    [SerializeField, FoldoutGroup("Reference")] private ShmackleNetworkRig owner;
    [SerializeField, FoldoutGroup("Reference")] private AudioSource kissSource;
    [SerializeField, FoldoutGroup("Reference")] private List<AudioClip> kissSounds;
    [SerializeField, FoldoutGroup("Reference")] private ParticleSystem kissEffect;
    [SerializeField, FoldoutGroup("Reference")] private ShmackleKissDecalPooler kissPooler;

    // Settings
    [SerializeField, FoldoutGroup("Setting")] private float kissUpdateTime = 0.5f;
    [SerializeField, FoldoutGroup("Setting")] private float kissCooldownTime = 3f;
    [SerializeField, FoldoutGroup("Setting")] private float kissAnimationTime = 0.5f;
    [SerializeField, FoldoutGroup("Setting")] private float kissDetectDistance;
    [SerializeField, FoldoutGroup("Setting")] private float kissActiveDistance;
    [SerializeField, FoldoutGroup("Setting")] private LayerMask kissTargetMask;

    // Positions & Offsets
    [SerializeField, FoldoutGroup("Active Position")] private Transform kissStartPos;
    [SerializeField, FoldoutGroup("Active Position")] private Transform headAreaPos;
    [SerializeField, FoldoutGroup("Active Position")] private Transform kissDecalParent;
    [SerializeField, FoldoutGroup("Active Position")] private Vector3 kissOffset;
    [SerializeField, FoldoutGroup("Active Position")] private float headRadius = 0.1f;

    // Body & Blendshapes
    [SerializeField, FoldoutGroup("Body")] private SkinnedMeshRenderer mainBodyRender;
    [SerializeField, FoldoutGroup("Body")] private int blendKissKey = -1;
    [SerializeField, FoldoutGroup("Body")] private int blendNerdFaceKey = -1;
    [SerializeField, FoldoutGroup("Body")] private int blendTeeth = -1;

    // Decal IDs
    [SerializeField, FoldoutGroup("Mount Decal ID")] private string kissDecalID;
    [SerializeField, FoldoutGroup("Mount Decal ID")] private string vampireBiteDecalID;

    // Vampire Mode
    [SerializeField, FoldoutGroup("Vampire")] private bool isVampireModeEnabled;
    [SerializeField, FoldoutGroup("Vampire"), Range(0, 1)] private float vampireChance;
    [SerializeField, FoldoutGroup("Vampire")] private Transform vampireBiteOffset;
    [SerializeField, FoldoutGroup("Vampire")] private VisualEffect vampireEffect;
    [SerializeField, FoldoutGroup("Vampire")] private AudioSource biteSource;
    [SerializeField, FoldoutGroup("Vampire")] private AudioClip biteSound;

    private KissState _currentState = KissState.Idle;
    private ShmacklePlayerController _playerTarget;
    private GameObject _headTarget;
    private float _initialTeethRatio;
    private Tweener _kissingAnimationTween;
    private Coroutine _stateMachineCoroutine;

    private const float MIN_TARGET_FACING_DOT_PRODUCT = 0.2f;
    private const float DECAL_FORWARD_OFFSET = 0.015f;
    private readonly RaycastHit[] _raycastHits = new RaycastHit[1];

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            StartStateMachine();
        }
        else
        {
            RPC_RequestSync();
        }
    }

    private void OnDestroy()
    {
        StopStateMachine();
        _kissingAnimationTween?.Kill();
    }

    private void StartStateMachine()
    {
        if (!IsEnabled)
        {
            return;
        }
        StopStateMachine();
        _stateMachineCoroutine = StartCoroutine(KissStateLoop());
    }

    private void StopStateMachine()
    {
        if (_stateMachineCoroutine != null)
        {
            StopCoroutine(_stateMachineCoroutine);
            _stateMachineCoroutine = null;
        }
    }

    private IEnumerator KissStateLoop()
    {
        while (Application.isPlaying)
        {
            if (IsEnabled && Object.HasStateAuthority)
            {
                yield return ProcessCurrentState();
            }
            yield return CoroutineTimeUtils.GetWaitForSeconds(kissUpdateTime);
        }
    }

    private IEnumerator ProcessCurrentState()
    {
        switch (_currentState)
        {
            case KissState.Idle:
                ProcessIdleState();
                break;
            case KissState.Aiming:
                ProcessAimingState();
                break;
            case KissState.Cooldown:
                yield return CoroutineTimeUtils.GetWaitForSeconds(kissCooldownTime);
                TransitionToState(KissState.Idle);
                break;
        }
    }

    private void ProcessIdleState()
    {
        Vector3 detectionOrigin = kissStartPos.position + kissStartPos.forward * kissOffset.z;
        if (Physics.RaycastNonAlloc(detectionOrigin, kissStartPos.forward, _raycastHits, kissDetectDistance, kissTargetMask) > 0)
        {
            if (TryValidateTarget(_raycastHits[0]))
            {
                TransitionToState(KissState.Aiming);
            }
        }
    }

    private void ProcessAimingState()
    {
        if (_headTarget == null || !TryValidateTarget(_raycastHits[0]))
        {
            TransitionToState(KissState.Idle);
            return;
        }

        Vector3 detectionOrigin = kissStartPos.position + kissStartPos.forward * kissOffset.z;
        float distanceToTarget = Vector3.Distance(_headTarget.transform.position, detectionOrigin);

        if (distanceToTarget > kissDetectDistance)
        {
            TransitionToState(KissState.Idle);
            return;
        }

        if (distanceToTarget <= kissActiveDistance)
        {
            PerformKissAction(_raycastHits[0].point);
        }
    }

    private void TransitionToState(KissState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;

        switch (newState)
        {
            case KissState.Idle:
                _headTarget = null;
                _playerTarget = null;
                if (IsKissing)
                {
                    IsKissing = false;
                    RPC_SetKissAnimationState(false);
                }
                break;
            case KissState.Aiming:
                if (!IsKissing)
                {
                    IsKissing = true;
                    RPC_SetKissAnimationState(true);
                }
                break;
            case KissState.Cooldown:
                if (IsKissing)
                {
                    IsKissing = false;
                    RPC_SetKissAnimationState(false);
                }
                break;
        }
    }

    private bool TryValidateTarget(RaycastHit hit)
    {
        if (hit.transform.CompareTag("KissArea"))
        {
            _headTarget = hit.transform.gameObject;
            _playerTarget = null;
            return true;
        }

        var targetPlayer = hit.transform.GetComponentInParent<ShmacklePlayerController>();
        if (targetPlayer == null || targetPlayer.playerModuleRef?.shmackleNetworkRig == null) return false;

        var targetRig = targetPlayer.playerModuleRef.shmackleNetworkRig;
        if (targetRig == owner) return false;

        if (ShmackleConnectionManager.Instance.IsBloodJmanMinigame())
        {
            if (!IsTargetValidInMinigame(targetRig.PlayerRef.PlayerId)) return false;
        }

        if (!IsTargetFacingPlayer(targetRig.headTarget)) return false;

        _playerTarget = targetPlayer;
        _headTarget = targetRig.headTarget;
        return true;
    }

    private bool IsTargetValidInMinigame(int playerId)
    {
        bool isInspector = BloodJmanGameManager.Instance.IsPlayerSpectator(playerId);
        bool isAlive = BloodJmanGameManager.Instance.IsPlayerSurvivorAlive(playerId);
        return !isInspector && isAlive;
    }

    private bool IsTargetFacingPlayer(GameObject targetHead)
    {
        Vector3 directionToMe = (kissStartPos.position - targetHead.transform.position).normalized;
        float forwardDotProduct = Vector3.Dot(targetHead.transform.forward, directionToMe);
        return forwardDotProduct > MIN_TARGET_FACING_DOT_PRODUCT;
    }

    private void PerformKissAction(Vector3 point)
    {
        bool isPlayerTarget = _playerTarget != null;
        bool isVampireBite = isPlayerTarget && isVampireModeEnabled && Random.Range(0f, 1f) <= vampireChance;

        if (isVampireBite)
        {
            _playerTarget.kissController.ReceiveBite(point);
            RPC_PlayBiteEffect(point);
        }
        else
        {
            if (isPlayerTarget) _playerTarget.kissController.ReceiveKiss(point);
            RPC_PlayKissEffect(point);
        }

        TriggerHaptics();
        TransitionToState(KissState.Cooldown);
    }

    private void TriggerHaptics()
    {
        if (!HasStateAuthority) return;
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0, 0.5f, 0.2f);
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0, 0.5f, 0.2f);
    }

    public void ReceiveKiss(Vector3 point) => SpawnMountDecal(point, kissDecalID);
    public void ReceiveBite(Vector3 point) => SpawnMountDecal(point, vampireBiteDecalID);

    private void HandleKissAnimationState(bool isKissing)
    {
        _kissingAnimationTween?.Kill();
        int maxBlend = mainBodyRender.sharedMesh.blendShapeCount - 1;

        if (blendKissKey > maxBlend || blendNerdFaceKey > maxBlend) return;

        if (isKissing)
        {
            if (blendTeeth >= 0) _initialTeethRatio = mainBodyRender.GetBlendShapeWeight(blendTeeth);
            SetBlendShape(blendTeeth, 0);

            _kissingAnimationTween = DOVirtual.Float(50f, 100f, kissAnimationTime, (amount) =>
            {
                SetBlendShape(blendKissKey, amount);
                SetBlendShape(blendNerdFaceKey, amount);
            }).SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            SetBlendShape(blendKissKey, 0);
            SetBlendShape(blendNerdFaceKey, 0);
            SetBlendShape(blendTeeth, _initialTeethRatio);
        }
    }

    private void SetBlendShape(int key, float amount)
    {
        if (key >= 0)
        {
            mainBodyRender.SetBlendShapeWeight(key, amount);
        }
    }

    private void SpawnMountDecal(Vector3 point, string decalID)
    {
        RPC_SpawnKissDecal(point, decalID);
    }

    public void SetActive(bool isActive)
    {
        if (HasStateAuthority)
        {
            IsEnabled = isActive;
        }
    }

    // --- RPC Section ---

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetKissAnimationState(bool isKissing)
    {
        HandleKissAnimationState(isKissing);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayKissEffect(Vector3 point)
    {
        int index = Random.Range(0, kissSounds.Count);
        kissSource.PlayOneShot(kissSounds[index]);
        kissEffect.transform.position = point;
        kissEffect.transform.LookAt(kissStartPos);
        kissEffect.Play();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayBiteEffect(Vector3 point)
    {
        biteSource.PlayOneShot(biteSound);
        vampireBiteOffset.position = point;
        vampireBiteOffset.LookAt(kissStartPos);
        vampireEffect.Play();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SpawnKissDecal(Vector3 point, string decalID)
    {
        if (string.IsNullOrEmpty(decalID)) return;

        if (ShmackleKissDecalPooler.Instance == null)
        {
            if (kissPooler != null) Instantiate(kissPooler);
            StartCoroutine(SpawnDecalAfterPoolerInitialization(point, decalID));
        }
        else
        {
            SpawnDecal(point, decalID);
        }
    }

    private void SpawnDecal(Vector3 point, string decalID)
    {
        if (headAreaPos == null || kissDecalParent == null) return;

        Vector3 direction = (point - headAreaPos.position).normalized;
        Vector3 position = headAreaPos.position + direction * headRadius;

        GameObject decalObject = ShmackleKissDecalPooler.Instance.GetFromPool(decalID, position, Quaternion.identity, kissDecalParent);
        if (decalObject == null) return;

        decalObject.transform.LookAt(headAreaPos);
        decalObject.transform.position += decalObject.transform.forward * DECAL_FORWARD_OFFSET;
    }

    private IEnumerator SpawnDecalAfterPoolerInitialization(Vector3 point, string decalID)
    {
        yield return null;
        if (ShmackleKissDecalPooler.Instance != null)
        {
            SpawnDecal(point, decalID);
        }
    }

    public void SetActiveEnabled(bool isEnabled)
    {
        if (!HasStateAuthority)
        {
            return;
        }
        RPC_SetActiveEnabled(isEnabled);
    }

    [Rpc(RpcSources.Proxies, RpcTargets.StateAuthority)]
    private void RPC_RequestSync(RpcInfo info = default)
    {
        RPC_SyncStateToProxy(IsEnabled, IsKissing, info.Source);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    private void RPC_SyncStateToProxy(NetworkBool isEnabled, NetworkBool isKissing, PlayerRef target)
    {
        if (Runner.LocalPlayer != target) return;

        this.IsEnabled = isEnabled;
        HandleKissAnimationState(isKissing);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SetActiveEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (kissStartPos == null) return;

        Vector3 detectionOrigin = kissStartPos.position + kissStartPos.forward * kissOffset.z;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(detectionOrigin, detectionOrigin + kissStartPos.forward * kissDetectDistance);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(detectionOrigin, detectionOrigin + kissStartPos.forward * kissActiveDistance);
        Gizmos.DrawWireSphere(detectionOrigin, 0.05f);
    }
#endif
}