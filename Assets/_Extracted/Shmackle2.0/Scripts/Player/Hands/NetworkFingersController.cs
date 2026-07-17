using Fusion;
using Fusion.XR.Shared.Core;
using System.Linq;

namespace Shmackle.Player.Hands
{
    public class NetworkFingersController : NetworkBehaviour
    {
        private INetworkController _networkController;
        private ShmackleHardwareHand _networkRigHardwareHand;
        private IHardwareRig _hardwareRig;
        private ShmackleHardwareHand _hardwareHand;
        private CompressedFingerBendCommand _previousCompressedCommand;
        
        private int _frameCounter = 0;
        private const int NETWORK_UPDATE_INTERVAL = 3; // Send updates every 3 frames
        /// <summary>
        /// Networked property that stores compressed finger bend angles for network synchronization.
        /// Used to efficiently transmit finger pose data between connected players.
        /// </summary>
        [Networked] private CompressedFingerBendCommand CompressedFingerBendCommand { get; set; }

        private void Awake()
        {
            _networkController = GetComponentInParent<INetworkController>();
            if (_networkController == null)
                throw new System.Exception("Should be placed under a INetworkController hierarchy");

            var rig = GetComponentInParent<IRig>();
            if (rig == null)
                throw new System.Exception("Should be placed under a IRig hierarchy");

            var headset = rig.gameObject.GetComponentInChildren<IHeadset>();
            if (headset == null)
                throw new System.Exception("No IHeadset found in IRig children");

            _networkRigHardwareHand = headset.gameObject.GetComponentsInChildren<ShmackleHardwareHand>(true)
                .FirstOrDefault(x => x.Side == _networkController.Side);
            if (!_networkRigHardwareHand)
                throw new System.Exception($"No ShmackleHardwareHand found with side {_networkController.Side}");

            _networkRigHardwareHand.UseManualAngleControl(true);
        }

        public override void Render()
        {
            base.Render();

            if (Object.HasStateAuthority)
            {
                if (_hardwareRig == null)
                {
                    _hardwareRig = HardwareRigsRegistry.GetHardwareRig();
                    var headset = _hardwareRig.gameObject.GetComponentInChildren<IHardwareHeadset>();

                    _hardwareHand = headset.gameObject.GetComponentsInChildren<ShmackleHardwareHand>(true)
                        .FirstOrDefault(x => x.Side == _networkController.Side);
                }

                if (_hardwareHand)
                {
                    // Take the commands from the HardwareRig's hands.
                    var fingerBendCommand = _hardwareHand.FingerBendCommand;

                    // Pass on the finger angles to NetworkRig's avatar
                    if (_networkRigHardwareHand)
                        _networkRigHardwareHand.SetManualAngle(fingerBendCommand);

                    // Pass on the finger angles to the network (compressed) only if changed and every N frames
                    _frameCounter++;
                    if (_frameCounter >= NETWORK_UPDATE_INTERVAL)
                    {
                        var newCompressedCommand = CompressedFingerBendCommand.FromFingerBendCommand(fingerBendCommand);
                        if (!newCompressedCommand.Equals(_previousCompressedCommand))
                        {
                            CompressedFingerBendCommand = newCompressedCommand;
                            _previousCompressedCommand = newCompressedCommand;
                        }
                        _frameCounter = 0;
                    }
                }
            }
            else
            {
                // Take the network commands and apply on to non-state authority players for sync.
                if (_networkRigHardwareHand)
                {
                    var fingerBendCommand = CompressedFingerBendCommand.ToFingerBendCommand(CompressedFingerBendCommand);
                    _networkRigHardwareHand.SetManualAngle(fingerBendCommand);
                }
            }
        }
    }
}