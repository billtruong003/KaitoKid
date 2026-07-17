using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Fusion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Core.Data;
using UnityEngine;

namespace Teabag.Player
{

    /// <summary>
    /// Drives finger bone animation from VR input using Jungle XRKit's 4-axis scheme:
    ///   thumbAxisCommand  — thumb extension
    ///   indexAxisCommand  — index finger (driven by trigger input)
    ///   gripAxisCommand   — grip / pinky
    ///   triggerAxisCommand — trigger finger
    /// Animator parameter names follow: Thumb.L, Index.L, Pinky.L (and .R).
    /// </summary>
    public class GorillaHandAnimator : NetworkBehaviour
    {
        private GorillaHand _hand;
        private IRigInfoService _gorillaService;

        [Header("Fingers — 4-Axis Scheme")]
        [SerializeField] private Finger _triggerFinger;
        [SerializeField] private Finger _indexFinger;
        [SerializeField] private Finger _gripFinger;
        [SerializeField] private Finger _thumbFinger;

        [Header("Fingers - Old Scheme - TO BE REMOVED")]
        public Finger indexFinger;
        public Finger middleFinger;
        public Finger thumbFinger;

        private const float LerpFactor = 0.35f;

        [Networked]
        public byte trigger { get; set; }
        [Networked]
        public byte grip { get; set; }
        [Networked]
        public bool thumb { get; set; }

        private void Awake()
        {
            _hand = GetComponent<GorillaHand>();
        }

        public override void Spawned()
        {
            base.Spawned();
            _gorillaService = ServiceLocator.Get<IRigInfoService>();
        }

        private void Update()
        {
            if (!Object || !Runner) return;

            if (HasStateAuthority && _gorillaService != null)
            {


                // Sync networked bytes from the high-fidelity RigState snapshot.
                // This ensures other players see your fingers moving.
                var rigState = _gorillaService.CurrentRigState;
                var handCmd = _hand.isLeftHand ? rigState.leftHandCommand : rigState.rightHandCommand;

                trigger = (byte)(handCmd.triggerAxisCommand * 255);

                // Maintain the special Toggle Grab case for networking.
                if (_hand.isHoldingToggleGrab)
                    grip = 255;
                else
                    grip = (byte)(handCmd.gripAxisCommand * 255);

                thumb = handCmd.thumbAxisCommand > 0.5f;

            }

            // if (HasStateAuthority)
            // {
            //     trigger = (byte)(VRInputHandler.GetInputDownAmount(_hand.isLeftHand, InputType.Trigger) * 255);
            //     if (_hand.isHoldingToggleGrab)
            //         grip = 255;
            //     else
            //         grip = (byte)(VRInputHandler.GetInputDownAmount(_hand.isLeftHand, InputType.Grip) * 255);
            //     thumb = VRInputHandler.GetInputDown(_hand.isLeftHand, InputType.Primary) || VRInputHandler.GetInputDown(_hand.isLeftHand, InputType.Secondary);
            // }

            Refresh();
        }

        public void Refresh()
        {
            if (Object == null)
                return;

            if (!Object.IsValid)
                return;

            float triggerTarget = trigger / 255f;
            float gripTarget = grip / 255f;
            float thumbTarget = thumb ? 1f : 0f;

            if (_triggerFinger != null) _triggerFinger.t = Mathf.Lerp(_triggerFinger.t, triggerTarget, LerpFactor);
            if (_indexFinger != null) _indexFinger.t = Mathf.Lerp(_indexFinger.t, triggerTarget, LerpFactor);
            if (_gripFinger != null) _gripFinger.t = Mathf.Lerp(_gripFinger.t, gripTarget, LerpFactor);
            if (_thumbFinger != null) _thumbFinger.t = Mathf.Lerp(_thumbFinger.t, thumbTarget, LerpFactor);
        }
    }
}
