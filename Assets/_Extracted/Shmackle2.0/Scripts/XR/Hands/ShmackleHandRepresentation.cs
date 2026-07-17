using Fusion;
using Fusion.XR.Shared.Core;
using Shmackle.Player.Hands;
using System.Linq;
using Shmackle.Player.Grab;
using UnityEngine;

namespace Shmackle.XR.Hands
{
    /// <summary>
    /// Takes hand commands from the parent lateralized (left or right) controller and forwards them.
    /// During Awake, it identifies the matching side, finds the rig root, then searches its children
    /// for another IHandCommandHandler to delegate incoming hand commands to.
    /// </summary>
    public class ShmackleHandRepresentation : MonoBehaviour, IHandCommandHandler
    {
        private ILateralizedRigPart _hardwareRigPart;
        private IGrabCommandHandler _commandHandler;
        
        private ShmackleGrabber _grabber;
        
        [Header("Debug")]
        [SerializeField, ReadOnly] private HandCommand _currentCommand;

        private void Awake()
        {
            IRig iRig = GetComponentInParent<IRig>(); // get the rig (whether hardware or network) from the parent.
            var lateralizedRigPart = GetComponentInParent<ILateralizedRigPart>();
            if (lateralizedRigPart != null)
            {
                RigPartSide side = lateralizedRigPart.Side;

                if (iRig != null)
                {
                    var iHeadSet = iRig.gameObject.GetComponentInChildren<IHeadset>(true);
                    _hardwareRigPart = iHeadSet.gameObject.GetComponentsInChildren<ILateralizedRigPart>()
                        .FirstOrDefault(rig => rig.Side == side);
                }
            }

            if (_hardwareRigPart != null)
            {
                _commandHandler = _hardwareRigPart.gameObject.GetComponent<IGrabCommandHandler>();
            }
            
            if (!_grabber)
            {
                _grabber = GetComponentInParent<ShmackleGrabber>(true);
            }
        }

        public void SetHandCommand(HandCommand command)
        {
            _currentCommand = command;
            _commandHandler?.SetHandCommand(command);

            if (_grabber)
            {
                if (_grabber.GrabbedObject)
                    _commandHandler?.SetGrabbedObject(_grabber.GrabbedObject.gameObject);
                else
                    _commandHandler?.SetGrabbedObject(null);
            }
        }
    }
}