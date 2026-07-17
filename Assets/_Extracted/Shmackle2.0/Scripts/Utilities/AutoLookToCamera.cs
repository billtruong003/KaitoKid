using UnityEngine;

namespace Shmackle.Utilities
{
    public class AutoLookToCamera : MonoBehaviour
    {
        #region Serialized Fields
        
        [SerializeField]
        private float _distanceZ = 1000f;
        [SerializeField]
        private bool _applyPositionOffset = true;

        #endregion

        private Transform _head;

        // TODO: Instead of the Update method we need to make it event-driven and to have the UI attached to the rig's main camera transform for the best performance. Ticket: https://strattonstudios1.atlassian.net/browse/JMAN-77
        private void LateUpdate()
        {
            if (!_head) _head = Camera.main ? Camera.main.transform : null;
            if (!_head) return;

            if (_applyPositionOffset)
            {
                transform.position = _head.position + _head.forward * _distanceZ;
            }
            transform.rotation = Quaternion.LookRotation(transform.position - _head.position, Vector3.up);
        }
    }
}