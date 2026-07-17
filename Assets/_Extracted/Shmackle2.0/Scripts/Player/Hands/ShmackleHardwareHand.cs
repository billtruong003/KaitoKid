using Fusion;
using Fusion.XR.Shared.Base;
using Fusion.XR.Shared.Core;
using System;
using UnityEngine;

namespace Shmackle.Player.Hands
{
    [Serializable]
    public struct FingerBendCommand : INetworkStruct
    {
        public float ThumbAngle;
        public float IndexAngle;
        public float MiddleAngle;
    }

    [Serializable]
    public struct CompressedFingerBendCommand : INetworkStruct
    {
        public sbyte ThumbAngle;
        public sbyte IndexAngle;
        public sbyte MiddleAngle;

        public static CompressedFingerBendCommand FromFingerBendCommand(FingerBendCommand command)
        {
            CompressedFingerBendCommand compressedCommand = default;
            // Scale from [-180, 180] to [-127, 127]
            compressedCommand.ThumbAngle = (sbyte)Mathf.Clamp(command.ThumbAngle * 127f / 180f, -128, 127);
            compressedCommand.IndexAngle = (sbyte)Mathf.Clamp(command.IndexAngle * 127f / 180f, -128, 127);
            compressedCommand.MiddleAngle = (sbyte)Mathf.Clamp(command.MiddleAngle * 127f / 180f, -128, 127);
            return compressedCommand;
        }

        public static FingerBendCommand ToFingerBendCommand(CompressedFingerBendCommand compressedCommand)
        {
            FingerBendCommand command = default;
            // Scale from [-127, 127] to [-180, 180]
            command.ThumbAngle = compressedCommand.ThumbAngle * 180f / 127f;
            command.IndexAngle = compressedCommand.IndexAngle * 180f / 127f;
            command.MiddleAngle = compressedCommand.MiddleAngle * 180f / 127f;
            return command;
        }
    }

    public interface IGrabCommandHandler : IHandCommandHandler
    {
        void SetGrabbedObject(GameObject grabbedObjectGameObject);
    }


    /// <summary>
    /// Takes controller commands via the hand command handler interface and forwards the values
    /// to drive finger rotation targets (thumb, index, middle). Intended to map incoming thumb,
    /// trigger, and grip inputs to smooth finger motions by updating the configured constraints.
    /// </summary>
    public class ShmackleHardwareHand : BaseLateralizedHardwareRigPart, IGrabCommandHandler
    {
        public override RigPartKind Kind => RigPartKind.Hand;
        public FingerBendCommand FingerBendCommand => _fingerBendCommand;

        [SerializeField] private FingerTargetRotationConstraints _indexFinger;
        [SerializeField] private FingerTargetRotationConstraints _middleFinger;
        [SerializeField] private FingerTargetRotationConstraints _thumbFinger;

        private PlayerLocomotion _playerLocomotion;
        [SerializeField, ReadOnly] private bool _isLeftHand;

        [Header("Debug")]
        [SerializeField, ReadOnly] private GameObject _grabbedObject;

        [SerializeField, ReadOnly] private FingerBendCommand _fingerBendCommand;

        // for debugging purposes only.
        [SerializeField, ReadOnly] private HandCommand _command;

        protected override void Awake()
        {
            base.Awake();
            _playerLocomotion = GetComponentInParent<PlayerLocomotion>();
            _isLeftHand = _side == RigPartSide.Left;
        }

        public void SetHandCommand(HandCommand command)
        {
            _command = command;

            if (_grabbedObject)
            {
                SetAllFingersMode(FingerMode.Grabbing);
            }
            else if (_playerLocomotion && _playerLocomotion.IsHandTouching(_isLeftHand))
            {
                SetAllFingersMode(FingerMode.SurfaceDetection);
            }
            else
            {
                SetAllFingersMode(FingerMode.Controller);
                SetControllerRotationValues();
            }

            UpdateFingerBendCommand();
        }

        public void UseManualAngleControl(bool useManualAngleControl)
        {
            if (_thumbFinger) _thumbFinger.UseManualAngleControl = useManualAngleControl;
            if (_indexFinger) _indexFinger.UseManualAngleControl = useManualAngleControl;
            if (_middleFinger) _middleFinger.UseManualAngleControl = useManualAngleControl;
        }

        public void SetManualAngle(FingerBendCommand fingerBendCommand)
        {
            if (_thumbFinger) _thumbFinger.SetCurrentAngle(fingerBendCommand.ThumbAngle);
            if (_indexFinger) _indexFinger.SetCurrentAngle(fingerBendCommand.IndexAngle);
            if (_middleFinger) _middleFinger.SetCurrentAngle(fingerBendCommand.MiddleAngle);
        }

        private void SetAllFingersMode(FingerMode mode)
        {
            if (_thumbFinger) _thumbFinger.SetMode(mode);
            if (_indexFinger) _indexFinger.SetMode(mode);
            if (_middleFinger) _middleFinger.SetMode(mode);
        }

        private void SetControllerRotationValues()
        {
            if (_middleFinger) _middleFinger.SetRotationValue(_command.gripCommand);
            if (_thumbFinger) _thumbFinger.SetRotationValue(_command.thumbTouchedCommand);
            if (_indexFinger) _indexFinger.SetRotationValue(_command.triggerCommand);
        }

        private void UpdateFingerBendCommand()
        {
            _fingerBendCommand.ThumbAngle = _thumbFinger ? _thumbFinger.CurrentAngle : 0f;
            _fingerBendCommand.IndexAngle = _indexFinger ? _indexFinger.CurrentAngle : 0f;
            _fingerBendCommand.MiddleAngle = _middleFinger ? _middleFinger.CurrentAngle : 0f;
        }

        public void SetGrabbedObject(GameObject grabbedObject)
        {
            _grabbedObject = grabbedObject;
        }
    }
}