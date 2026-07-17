using Fusion;
using Fusion.XR.Shared.Core;
using Stratton.Core;
using Stratton.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Shmackle.Player
{
    [DefaultExecutionOrder(INetworkRig.EXECUTION_ORDER)]
    public sealed class PlayerNetworkRig : NetworkBehaviour, INetworkRig
    {
        public const int EXECUTION_ORDER = INetworkRig.EXECUTION_ORDER;

        #region Serialized Fields

        [SerializeField] private ExtrapolationTiming _extrapolationTiming = ExtrapolationTiming.DuringUnityOnBeforeRender;

        #endregion

        #region Fields

        private NetworkingSystem _networkingSystem;

        private Rigidbody _rigidbody;
        private INetworkHeadset _networkHeadset;
        private IHardwareRig _localHardwareRig;
        private PlayerLocomotion _localPlayerLocomotion;

        #endregion

        #region Properties

        public IHeadset Headset => _networkHeadset;
        public ExtrapolationTiming RequiredExtrapolationTiming => _extrapolationTiming;
        public List<INetworkRigPart> RigParts { get; } = new List<INetworkRigPart>();
        public PlayerLocomotion LocalPlayerLocomotion => _localPlayerLocomotion;

        #endregion

        #region Public Methods

        #endregion

        #region NetworkBehaviour Methods

        public override void Spawned()
        {
            if (Object.HasStateAuthority) RegisterLocalNetworkUserRig();

            name = Object.InputAuthority.ToString();

            _rigidbody = GetComponent<Rigidbody>();

            Application.onBeforeRender += OnBeforeRender;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            Application.onBeforeRender -= OnBeforeRender;
        }

        public override sealed void FixedUpdateNetwork()
        {
            UpdateWithLocalHardwareRig();

            if ((Runner.IsServer || Object.HasStateAuthority) && !Object.InputAuthority.IsNone)
            {
                if (Runner.GameMode != GameMode.Shared)
                    Runner.ClearPlayerAreaOfInterest(Object.InputAuthority);

                Runner.AddPlayerAreaOfInterest(Object.InputAuthority, transform.position, _networkingSystem.NetworkingSettings.AreaOfInterestRadius);
            }
        }

        public override sealed void Render()
        {
            if (Object.HasStateAuthority)
            {
                // We also do it in the render in case of late availability of the hardware rig
              
                RegisterLocalNetworkUserRig();

                if (_extrapolationTiming == ExtrapolationTiming.DuringFusionRender)
                {
                    ExtrapolateWithLocalHardwareRig();
                }
            }
        }

        #endregion

        #region MonoBehaviour Methods

        private void Awake()
        {
            _networkingSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();
        }

        #endregion

        #region Private Methods

        [BeforeRenderOrder(NetworkRig.EXECUTION_ORDER)]
        private void OnBeforeRender()
        {
            if (Object.HasStateAuthority)
            {
                if (_extrapolationTiming == ExtrapolationTiming.DuringUnityOnBeforeRender)
                {
                    ExtrapolateWithLocalHardwareRig();
                }
            }
        }

        private void RegisterLocalNetworkUserRig()
        {
            if (_localHardwareRig != null) return;
            if (Object.HasStateAuthority)
            {
                foreach (var rig in HardwareRigsRegistry.GetAvailableHardwareRigs())
                {
                    _localHardwareRig = rig;
                    _localHardwareRig.RegisterLocalUserNetworkRig(this);
                    _localPlayerLocomotion = _localHardwareRig.gameObject.GetComponentInChildren<PlayerLocomotion>();
                }
            }
        }

        #endregion

        public void RegisterNetworkRigPart(INetworkRigPart rigPart)
        {
            if (RigParts.Contains(rigPart) == false)
            {
                RigParts.Add(rigPart);

                if (rigPart is INetworkHeadset headset)
                {
                    _networkHeadset = headset;
                }
            }
        }

        public void UnregisterNetworkRigPart(INetworkRigPart rigPart)
        {
            if (RigParts.Contains(rigPart))
            {
                RigParts.Remove(rigPart);
            }
        }

        private void UpdateWithLocalHardwareRig()
        {
            if (_localPlayerLocomotion == null) return;
            transform.position = _localPlayerLocomotion.transform.position;
            transform.rotation = _localPlayerLocomotion.transform.rotation;
            transform.localScale = _localPlayerLocomotion.transform.localScale;
            _rigidbody.CopyFrom(_localPlayerLocomotion.Rigidbody);
        }

        private void ExtrapolateWithLocalHardwareRig()
        {
            if (_localPlayerLocomotion == null) return;
            transform.position = _localPlayerLocomotion.transform.position;
            transform.rotation = _localPlayerLocomotion.transform.rotation;
            transform.localScale = _localPlayerLocomotion.transform.localScale;
            _rigidbody.CopyFrom(_localPlayerLocomotion.Rigidbody);
        }
    }
}

public static class RigidbodyCopy
{
    /// <summary>
    /// Copy the dynamic state (velocities) and, optionally, the tuning/config from one Rigidbody to another.
    /// </summary>
    public static void CopyFrom(this Rigidbody to, Rigidbody from, bool copyTuning = true, bool wake = true)
    {
        if (to == null || from == null) return;

        // Velocities (world space)
        bool wasKinematic = to.isKinematic;
        if (wasKinematic) to.isKinematic = false;   // kinematic bodies ignore velocities

        to.linearVelocity = from.linearVelocity;
        to.angularVelocity = from.angularVelocity;

        if (wake) to.WakeUp();

        if (copyTuning)
        {
            // Common �feel� properties
            to.mass = from.mass;
            to.linearDamping = from.linearDamping;
            to.angularDamping = from.angularDamping;
            to.useGravity = from.useGravity;
            to.interpolation = from.interpolation;
            to.collisionDetectionMode = from.collisionDetectionMode;
            to.constraints = from.constraints;

            // Optional, if you rely on them:
            to.maxAngularVelocity = from.maxAngularVelocity;
            to.solverIterations = from.solverIterations;
            to.solverVelocityIterations = from.solverVelocityIterations;
            // centerOfMass / inertia tensor are settable, but only copy them if you�ve customized them:
            // to.centerOfMass = from.centerOfMass;
            // to.inertiaTensorRotation = from.inertiaTensorRotation;
            // to.inertiaTensor = from.inertiaTensor;
        }

        // If the target was originally kinematic and you want it to stay that way, put this back.
        // (Note: kinematic bodies won�t move due to the velocities you just set.)
        if (wasKinematic) to.isKinematic = true;
    }
}