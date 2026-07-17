using System;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Core.Data;
using Teabag.Player.Rig;
using UnityEngine;

namespace GorillaLocomotion
{
    public class LocalGorillaRig : MonoBehaviour, IGorillaRig
    {
        [SerializeField]
        private Player _gorillaPlayer;
        private IGorillaService _gorillaService;

        private RigState _currentRigState;
        private RigState _previousRigState;
        private List<IRigPart> _rigParts = new List<IRigPart>();


        public Player GorillaPlayer => _gorillaPlayer;

        // ── IGorillaRig Implementation ───────────────────────────────────────

        public event EventHandler OnRigInitializedEvent;

        public RigType RigType => RigType.HardwareVR;

        public RigState RigState => _currentRigState;
        public RigState PreviousRigState => _previousRigState;

        public IReadOnlyList<IRigPart> RigParts => _rigParts;

        public Transform HeadTransform => _gorillaPlayer.headCollider.transform;
        public Transform LeftHandTransform => _gorillaPlayer.leftHandOriginal.transform;
        public Transform RightHandTransform => _gorillaPlayer.rightHandOriginal.transform;
        public Transform RootTransform => _gorillaPlayer.transform;

        private void Update()
        {
            UpdateRigState();
        }

        private void Awake()
        {
            _gorillaService = ServiceLocator.Get<IGorillaService>();
            Initialize();
        }

        private void OnDestroy()
        {
            // Update follow Hardware Rig
            // _gorillaService?.UnregisterGorillaRig(this);
        }

        public void Initialize()
        {
            Register();
            PopulateRigParts();

            if (RootTransform != null)
                GameLogger.Info($"LocalGorillaRig initialized: {RootTransform.name}. Parts: {_rigParts.Count}");

            OnRigInitializedEvent?.Invoke(this, EventArgs.Empty);
        }

        private void PopulateRigParts()
        {
            _rigParts.Clear();
            if (HeadTransform != null) _rigParts.Add(new GorillaRigPart(RigPart.Headset, HeadTransform));
            if (LeftHandTransform != null) _rigParts.Add(new GorillaRigPart(RigPart.LeftController, LeftHandTransform));
            if (RightHandTransform != null) _rigParts.Add(new GorillaRigPart(RigPart.RightController, RightHandTransform));
            if (RootTransform != null) _rigParts.Add(new GorillaRigPart(RigPart.Body, RootTransform));
        }

        private struct GorillaRigPart : IRigPart
        {
            public RigPart Part { get; }
            private readonly Transform _transform;

            public GorillaRigPart(RigPart part, Transform transform)
            {
                Part = part;
                _transform = transform;
            }

            public Vector3 Position => _transform != null ? _transform.position : Vector3.zero;
            public Quaternion Rotation => _transform != null ? _transform.rotation : Quaternion.identity;
        }

        /// <summary>
        /// Snapshots all physical rig transforms and controller inputs into a RigState value.
        /// Matches Jungle XRKit's HardwareRig.UpdateRigState() pattern.
        /// </summary>
        private void UpdateRigState()
        {
            _previousRigState = _currentRigState;

            _currentRigState = new RigState
            {
                rigPosition       = RootTransform != null ? RootTransform.position : transform.position,
                rigRotation       = RootTransform != null ? RootTransform.rotation : transform.rotation,

                leftHandPosition  = LeftHandTransform  != null ? LeftHandTransform.position  : Vector3.zero,
                leftHandRotation  = LeftHandTransform  != null ? LeftHandTransform.rotation  : Quaternion.identity,

                rightHandPosition = RightHandTransform != null ? RightHandTransform.position : Vector3.zero,
                rightHandRotation = RightHandTransform != null ? RightHandTransform.rotation : Quaternion.identity,

                headsetPosition   = HeadTransform   != null ? HeadTransform.position   : Vector3.zero,
                headsetRotation   = HeadTransform   != null ? HeadTransform.rotation   : Quaternion.identity,

                playAreaPosition  = RootTransform      != null ? RootTransform.position      : Vector3.zero,
                playAreaRotation  = RootTransform      != null ? RootTransform.rotation      : Quaternion.identity,

                leftHandCommand   = BuildHandCommand(isLeft: true),
                rightHandCommand  = BuildHandCommand(isLeft: false),
            };
        }

        /// <summary>
        /// Builds a <see cref="HandCommand"/> from live VR input for one hand.
        /// </summary>
        private static HandCommand BuildHandCommand(bool isLeft) => new HandCommand
        {
            thumbAxisCommand   = (VRInputHandler.GetInputDown(isLeft, InputType.Primary) ||
                                  VRInputHandler.GetInputDown(isLeft, InputType.Secondary)) ? 1f : 0f,
            indexAxisCommand   = VRInputHandler.GetInputDownAmount(isLeft, InputType.Trigger),
            gripAxisCommand    = VRInputHandler.GetInputDownAmount(isLeft, InputType.Grip),
            triggerAxisCommand = VRInputHandler.GetInputDownAmount(isLeft, InputType.Trigger),
            poseCommand        = 0,
            pinchCommand       = VRInputHandler.GetInputDownAmount(isLeft, InputType.Trigger),
        };

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (RootTransform != null)
            {
                RootTransform.SetPositionAndRotation(position, rotation);
            }

            if (_gorillaPlayer != null)
            {
                _gorillaPlayer.ResetVelocity();
            }
        }

        public void Register()
        {
            // Update follow Hardware Rig
            // _gorillaService?.RegisterGorillaRig(this);
        }
    }

}
