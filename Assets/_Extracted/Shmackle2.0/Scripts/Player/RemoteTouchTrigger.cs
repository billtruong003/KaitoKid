using Fusion.XR.Shared.Core;
using UnityEngine;

namespace Shmackle.Player
{
    /// <summary>
    /// Detects collision/trigger events with remote player components when attached to a local player's palm.
    /// This script is placed on the local layer's hand/palm area and triggers interactions when it contacts
    /// remote layer body parts (e.g., face, hand, body).
    /// </summary>
    public class RemoteTouchTrigger : MonoBehaviour
    {
        [SerializeField]
        private float _slapSpeedThreshold = 1;

        [SerializeField]
        private float _oppositeSideCooldown = 1f;

        private IHardwareRig _hardwareRig;
        private PlayerNetworkEventRelay _networkEventRelay;
        private Vector3 _lastPosition;
        private float _currentSpeed;
        private RemoteButtTouchTarget.Side? _lastSlappedSide;
        private float _lastSlapTime = -999f;

        private void Awake()
        {
            _hardwareRig = GetComponentInParent<IHardwareRig>();
        }

        private void Update()
        {
            // TODO: implement on and off
            // call this on update LocalUserNetworkRig is set up on runtime.
            if (_hardwareRig?.LocalUserNetworkRig != null && _networkEventRelay == null)
            {
                _networkEventRelay = _hardwareRig.LocalUserNetworkRig.gameObject.GetComponent<PlayerNetworkEventRelay>();
            }
            
            if(_networkEventRelay == null) return;
            
            Vector3 delta = transform.position - _lastPosition;
            float distance = delta.magnitude;
            float dt = Time.deltaTime;

            if (dt > 0f)
            {
                _currentSpeed = distance / dt;
            }

            _lastPosition = transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out RemoteButtTouchTarget remoteButtTouchTarget))
            {
                // remoteButtTouchTarget.RigPartSide
                var currentSide = remoteButtTouchTarget.RigPartSide;

                // Check if trying to slap the opposite side too soon
                if (_lastSlappedSide.HasValue && _lastSlappedSide.Value != currentSide)
                {
                    if (Time.time - _lastSlapTime < _oppositeSideCooldown)
                    {
                        return; // Opposite side is on cooldown
                    }
                }

                if (_currentSpeed >= _slapSpeedThreshold && _networkEventRelay)
                {
                    var contactPosition = other.ClosestPoint(transform.position);

                    _networkEventRelay.ButtSmackEvent(contactPosition);

                    // Record the side and time of this slap
                    _lastSlappedSide = currentSide;
                    _lastSlapTime = Time.time;
                }
            }
        }
    }
}