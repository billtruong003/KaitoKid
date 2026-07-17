using UnityEngine;
using UnityEngine.XR;

namespace Shmackle.UI
{
    [RequireComponent(typeof(Canvas))]
    public class UICanvasDynamicRenderMode : MonoBehaviour
    {
        #region Private Fields

        private Canvas _canvas;
        private RectTransform _rectTransform;

        private bool _hasDefaultWorldConfig = false;
        private Vector2 _defaultWorldSize;
        private Vector3 _defaultWorldPosition;
        private Quaternion _defaultWorldRotation;
        private Vector3 _defaultWorldScale;

        #endregion

        #region Private Methods

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _rectTransform = GetComponent<RectTransform>();
            if ( _canvas.renderMode == RenderMode.WorldSpace) 
            {
                _defaultWorldSize     = _rectTransform.sizeDelta;
                _defaultWorldPosition = _rectTransform.localPosition;
                _defaultWorldRotation = _rectTransform.localRotation;
                _defaultWorldScale    = _rectTransform.localScale;
                _hasDefaultWorldConfig = true;
            }
        }

        private void OnEnable()
        {
            UnityEngine.XR.InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.isValid)
            {
                _canvas.renderMode = RenderMode.WorldSpace;
                if (_hasDefaultWorldConfig)
                {
                    _rectTransform.sizeDelta     = _defaultWorldSize;
                    _rectTransform.localPosition = _defaultWorldPosition;
                    _rectTransform.localRotation = _defaultWorldRotation;
                    _rectTransform.localScale    = _defaultWorldScale;
                }
            }
            else
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        #endregion
    }
}
