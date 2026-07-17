using Fusion;
using Fusion.XR.Shared.Core;
using Shmackle.Player.Kiss;
using UnityEngine;

namespace Shmackle.Player
{
    /// <summary>
    /// For detecting kiss interactions between players.
    /// Attaches to the local player's hardware rig and handles collision detection with remote kiss targets.
    /// Manages the physics-based trigger detection, validates kiss conditions like facing direction,
    /// and relays kiss events through the network when valid kiss interactions occur.
    /// </summary>
    public class RemoteKissTrigger : MonoBehaviour
    {
        private IHardwareRig _hardwareRig;
        private PlayerNetworkEventRelay _networkEventRelay;
        private KissPool _kissPool;

        private void Awake() { _hardwareRig = GetComponentInParent<IHardwareRig>(); }

        private void Update()
        {
            if (_hardwareRig?.LocalUserNetworkRig != null && !_networkEventRelay)
                _networkEventRelay = _hardwareRig.LocalUserNetworkRig.gameObject.GetComponent<PlayerNetworkEventRelay>();

            if (_hardwareRig != null && !_kissPool)
                _kissPool = _hardwareRig.transform.GetComponentInChildren<KissPool>(true);
        }

        /// <summary>
        /// Handles collision detection for kiss interactions between players.
        /// </summary>
        /// <param name="other">The collider that was entered</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out RemoteKissTarget target))
            {
                NetworkObject targetNetworkObject = target.GetComponentInParent<NetworkObject>();

                if (targetNetworkObject || _networkEventRelay)
                {
                    // Calculate the direction the player is facing relative to the target
                    // Positive value means facing front, negative means facing back
                    var entryDirection = Vector3.Dot((transform.position - target.transform.position).normalized, target.transform.forward);

                    // Prevent kisses from being triggered from behind the target
                    if (entryDirection <= 0f)
                        return;

                    // Calculate the closest point on the target collider to trigger position
                    Vector3 kissContactPoint = other.ClosestPoint(transform.position);

                    // Calculate the surface normal at the contact point
                    Vector3 surfaceNormal = (transform.position - kissContactPoint).normalized;

                    // Calculate rotation for the kiss effect, oriented along the surface normal
                    // and aligned with the user's up direction
                    Quaternion kissRotation = Quaternion.LookRotation(-surfaceNormal, _hardwareRig.LocalUserNetworkRig.transform.up);

                    //Convert World Position to Local Position
                    Vector3 localContactPoint = target.transform.InverseTransformPoint(kissContactPoint);

                    //Convert World Rotation to Local Rotation
                    // This describes how the decal is rotated relative to the target's current facing
                    Quaternion localRotation = Quaternion.Inverse(target.transform.rotation) * kissRotation;

                    _networkEventRelay.KissEvent(localContactPoint, localRotation, targetNetworkObject.Id);
                }
            }
        }
    }
}