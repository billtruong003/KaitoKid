using UnityEngine;
using UnityEngine.Events;

namespace Shmackle.Pad
{
    public class ShmacklePadAnimationEvents : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private ShmacklePad _pad;
        [SerializeField]
        private UnityEvent _onActivationStartEvent;
        [SerializeField]
        private UnityEvent _onActivationStopEvent;
        [SerializeField]
        private UnityEvent _onDeactivationStartEvent;
        [SerializeField]
        private UnityEvent _onDeactivationStopEvent;

        #endregion

        #region Private Methods

        private void Awake()
        {
            if (!_pad)
            {
                _pad = GetComponentInParent<ShmacklePad>();
            }
        }

        #endregion

        #region Public Methods

        public void OnAnimationOpenPad()
        {
            Debug.Log("OnAnimationOpenPad");
            if (_pad.Object)
            {
                // Note that we use negative timescale animation for open/ close
                if (_pad.ShmacklePadActivationInfo.IsActive)
                {
                    _onActivationStartEvent?.Invoke();
                }
                else
                {
                    _onDeactivationStopEvent?.Invoke();
                }
            }
        }

        public void OnAnimationClosePad()
        {
            Debug.Log("OnAnimationClosePad");
            if (_pad.Object)
            {
                if (_pad.ShmacklePadActivationInfo.IsActive)
                {
                    _onActivationStopEvent?.Invoke();
                }
                else
                {
                    _onDeactivationStartEvent?.Invoke();
                }
            }
        }

        #endregion
    }
}