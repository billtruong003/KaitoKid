using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using GorillaLocomotion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Services;
using TMPro;
using UnityEngine;

namespace Teabag.Player
{
    public class Gorilla : NetworkBehaviour, IGorilla, IHittable
    {
        [Networked, OnChangedRender(nameof(OnNameChanged))]
        public string playerName { get; set; }

        [Networked, OnChangedRender(nameof(OnCrownChanged))]
        public NetworkBool hasCrown { get; set; }

        public string id
        {
            get
            {
                if (!Object || !Runner)
                    return string.Empty;

                string userId = Runner.GetPlayerUserId(Object.StateAuthority);
                if (!string.IsNullOrEmpty(userId))
                    return userId;

                if (HasStateAuthority)
                    return PlayerData.playFabId;
                return string.Empty;
            }
        }

        [Header("Tracking Targets")]
        [SerializeField] private Transform _headTransform;
        [SerializeField] private Transform _bodyTransform;
        [SerializeField] private Transform _leftHandTransform;
        [SerializeField] private Transform _rightHandTransform;
        [SerializeField] private Transform _rootBoneTransform;

        [Header("Visual Model")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private LayerMask _defaultBodyLayer;
        [SerializeField] private LayerMask _invisibleBodyLayer;

        public Transform headTransform => _headTransform;
        public Transform bodyTransform => _bodyTransform;
        public Transform leftHandTransform => _leftHandTransform;
        public Transform rightHandTransform => _rightHandTransform;
        public Transform rootBoneTransform => _rootBoneTransform;

        [Header("Hands")]
        public GorillaHand leftHand;
        public GorillaHand rightHand;

        [Header("Components")]
        public GorillaMaterial material;
        public GorillaCosmetics cosmetics;
        public GorillaParachute parachute;
        public Jetpack jetpack;
        public GorillaHealth health;
        public GorillaTeam team;
        public GorillaBody body;

        [Header("Visual")]
        public TMP_Text playerText;
        [SerializeField] private GorillaCrown crown;

        [Header("Audio")]
        public AudioSource source;
        public bool isMuted
        {
            get => !source.enabled;
            set
            {
                source.enabled = !value;
                ServiceLocator.Get<IDataPersistenceService>()?.TrySaveData($"{id}_MUTED", value ? 1 : 0);
            }
        }

        [Header("Prefabs")]
        public GorillaRagdoll ragdoll;

        string IGorilla.PlayerName => playerName;
        string IGorilla.PlayerId => id;
        bool IGorilla.IsDead => health != null && health.isDead;
        Transform IGorilla.HeadTransform => headTransform;
        Transform IGorilla.BodyTransform => bodyTransform;
        Transform IGorilla.LeftHandTransform => leftHandTransform;
        Transform IGorilla.RightHandTransform => rightHandTransform;
        Transform IGorilla.Transform => transform;

        private bool _registeredWithService;
        private IGorillaService _gorillaService;

        private IHardwareRig LocalHardwareRig
        {
            get
            {
                var rigInfoService = ServiceLocator.Get<IRigInfoService>();
                return rigInfoService?.HardwareRig;
            }
        }
        public Vector3 GetVelocity
        {
            get => LocalHardwareRig?.LocomotionController?.PlayerRigidbody?.linearVelocity
                ?? Vector3.zero;
        }

        public const string TAG_NUT = "Nut";
        public const string TAG_HEAD = "Head";

        public override void Spawned()
        {
            base.Spawned();

            Runner.SetPlayerObject(Object.StateAuthority, Object);
            if (HasStateAuthority)
            {
                playerName = PlayerData.displayName;
                SetLocalPlayerVisibility(false);

                // Restore crown from the session crown service so it survives server changes.
                var crownService = ServiceLocator.Get<ICrownService>();
                var authenticationService = ServiceLocator.Get<IAuthenticationService>();
                string authPlayFabId = authenticationService?.PlayFabId;
                string playerDataPlayFabId = PlayerData.playFabId;
                string crownKey = !string.IsNullOrEmpty(authPlayFabId)
                    ? authPlayFabId
                    : !string.IsNullOrEmpty(playerDataPlayFabId)
                        ? playerDataPlayFabId
                        : string.Empty;
                bool hadCrown = crownService?.HasCrown(crownKey) ?? false;
                hasCrown = hadCrown;
            }

            OnNameChanged();
            OnCrownChanged();
            GameLogger.Info($"Gorilla has been spawned (Id={id}, name={playerName})");
            TryRegisterWithService();
        }

        private void TryRegisterWithService()
        {
            if (_registeredWithService) return;
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
            if (_gorillaService == null) return;

            _gorillaService.RegisterGorilla(this);

            if (HasStateAuthority && LocalHardwareRig is HardwareRig rig)
            {
                _gorillaService.RegisterGorillaRig(rig);
            }

            _registeredWithService = true;
        }

        private void SetLocalPlayerVisibility(bool visible)
        {
            if (body == null || body.body == null) return;

            LayerMask mask = visible ? _defaultBodyLayer : _invisibleBodyLayer;
            int layer = 0;
            int value = mask.value;

            // Get the first set bit index
            if (value > 0)
            {
                for (int i = 0; i < 32; i++)
                {
                    if ((value & (1 << i)) != 0)
                    {
                        layer = i;
                        break;
                    }
                }
            }

            body.body.gameObject.layer = layer;
        }

        [ContextMenu("Spawn Ragdoll")]
        public void TestSpawn()
        {
            SpawnRagdoll(transform.position, transform.rotation, Vector3.zero);
        }
        public GorillaRagdoll SpawnRagdoll(Vector3 deathPos, Quaternion deathRot, Vector3 velocity)
        {
            GameLogger.Info($"Spawning Ragdoll (deatPos={deathPos}, deathRot={deathRot}, velocity={velocity})");

            GorillaRagdoll rag = Instantiate(ragdoll);
            rag.Clone(this, deathPos + Vector3.up * 0.25f, deathRot, velocity);
            return rag;
        }


        private void Update()
        {
            if (!_registeredWithService) TryRegisterWithService();
            UpdatePosition();
        }
        private void LateUpdate() => UpdatePosition();
        public override void Render() => UpdatePosition();
        public override void FixedUpdateNetwork() => UpdatePosition();

        public void UpdatePosition()
        {
            if (!Object || !Runner) return;

            var hardwareRig = LocalHardwareRig;
            if (hardwareRig == null) return;

            if (playerText != null)
            {
                bool isWithinNameplateRange = Vector3.Distance(headTransform.position, hardwareRig.Headset.Position) < 32;
                playerText.enabled = !HasStateAuthority && isWithinNameplateRange;
            }

            if (!HasStateAuthority)
                return;

            headTransform.position = hardwareRig.FollowerHead.HeadPosition;
            headTransform.rotation = hardwareRig.Headset.Rotation;

            leftHandTransform.position = hardwareRig.LeftFollowerHand.Position;
            leftHandTransform.rotation = hardwareRig.LeftFollowerHand.Rotation;

            rightHandTransform.position = hardwareRig.RightFollowerHand.Position;
            rightHandTransform.rotation = hardwareRig.RightFollowerHand.Rotation;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPCAddForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
        {
            var hardwareRig = LocalHardwareRig;
            hardwareRig?.LocomotionController?.PlayerRigidbody?.AddForce(force, mode);
        }

        public void OnNameChanged()
        {
            if (playerText != null)
            {
                playerText.text = playerName;
            }

            name = $"Gorilla ({playerName})";
        }

        public void OnCrownChanged()
        {
            crown?.SetVisible(hasCrown);
        }

        public void OnHit(byte damage, float hitVelocity, RaycastHit hit, Vector3 source, PlayerRef? killer = null)
        {
            if (health && !health.isDead)
            {
                HitType hitType = HitType.Normal;
                if (hit.transform.CompareTag(TAG_HEAD)) hitType = HitType.Head;
                else if (hit.transform.CompareTag(TAG_NUT)) hitType = HitType.Nut;

                health.Damage(damage, hitType, killer, source);

                if ((GameServices.IsModEnabled?.Invoke("Fling") ?? false))
                {
                    RPCAddForce(Vector3.Normalize(transform.position - source) * (hitVelocity / Time.deltaTime) + Vector3.up * 3);
                }
            }
        }

        private void OnDestroy()
        {
            if (_registeredWithService)
            {
                var service = _gorillaService ?? ServiceLocator.Get<IGorillaService>();
                if (service != null)
                {
                    service.UnregisterGorilla(this);
                    if (HasStateAuthority && LocalHardwareRig != null)
                    {
                        service.UnregisterGorillaRig(LocalHardwareRig);
                    }
                }
                _registeredWithService = false;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            var service = _gorillaService ?? ServiceLocator.Get<IGorillaService>();
            if (service != null)
            {
                service.UnregisterGorilla(this);
                if (HasStateAuthority && LocalHardwareRig != null)
                {
                    service.UnregisterGorillaRig(LocalHardwareRig);
                }
            }
            _registeredWithService = false;
            _gorillaService = null;
        }

        private void OnDrawGizmos()
        {
            var hardwareRig = LocalHardwareRig;
            if (hardwareRig != null)
            {
                Gizmos.DrawWireSphere(hardwareRig.Headset.Position, 0.2f);
                Gizmos.DrawWireSphere(hardwareRig.LeftFollowerHand.Position, 0.2f);
                Gizmos.DrawWireSphere(hardwareRig.RightFollowerHand.Position, 0.2f);
            }
        }
        public static Gorilla Find(PlayerRef player)
        {
            if (player == PlayerRef.None) return null;

            // Best way: Use Fusion's player-to-object mapping
            var gorillaService = ServiceLocator.Get<IGorillaService>();
            var localGorilla = gorillaService?.LocalGorilla as Gorilla;
            NetworkRunner runner = localGorilla != null ? localGorilla.Runner : null;
            if (runner == null) runner = FindObjectOfType<NetworkRunner>(); // Last resort to find runner

            if (runner != null && runner.IsRunning)
            {
                var obj = runner.GetPlayerObject(player);
                if (obj != null)
                {
                    var g = obj.GetComponent<Gorilla>();
                    if (g != null) return g;
                }
            }

            // Fallback 1: Search the service's list
            var allGorillas = gorillaService?.Gorillas;
            if (allGorillas != null)
            {
                foreach (IGorilla entry in allGorillas)
                {
                    var gorilla = entry as Gorilla;
                    if (gorilla != null && gorilla.Object != null && gorilla.Object.IsValid && gorilla.Object.StateAuthority == player)
                        return gorilla;
                }
            }

            // Fallback 2: Nuclear option - search all Gorillas in the scene
            foreach (Gorilla gorilla in FindObjectsOfType<Gorilla>())
            {
                if (gorilla != null && gorilla.Object != null && gorilla.Object.IsValid && gorilla.Object.StateAuthority == player)
                    return gorilla;
            }

            return null;
        }
    }
}
