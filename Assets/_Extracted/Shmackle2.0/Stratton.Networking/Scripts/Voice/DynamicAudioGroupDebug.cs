using UnityEngine;

namespace Stratton.Networking.Voice
{
    [RequireComponent(typeof(CustomDynamicAudioGroupMember))]
    public class DynamicAudioGroupDebug : MonoBehaviour
    {
        [SerializeField]
        private CustomDynamicAudioGroupMember _dynamicAudioGroupMember;

        private void Awake()
        {
            if (_dynamicAudioGroupMember == null)
            {
                _dynamicAudioGroupMember = GetComponent<CustomDynamicAudioGroupMember>();
            }
        }

        private void OnDrawGizmos()
        {
            if (_dynamicAudioGroupMember != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, _dynamicAudioGroupMember.proximityDistance);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, _dynamicAudioGroupMember.proximityLeavingDistance);
            }
        }
    }
}
