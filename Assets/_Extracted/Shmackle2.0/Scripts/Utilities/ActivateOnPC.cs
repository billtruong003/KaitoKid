using UnityEngine;
using UnityEngine.XR;

namespace Shmackle.Utilities
{
    public class ActivateOnPC : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private GameObject _target;

        #endregion

        #region Private Methods

        private void Awake()
        {
            if (_target == null)
            {
                _target = gameObject;
            }
        }

        private void OnEnable()
        {
            UnityEngine.XR.InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            _target.gameObject.SetActive(!head.isValid);   
        }

        #endregion

    }
}