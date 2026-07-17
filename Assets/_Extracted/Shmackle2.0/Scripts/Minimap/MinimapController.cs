using MessagePipe;
using System;
using System.Collections;
using UnityEngine;

namespace Shmackle.Minimap
{
    [RequireComponent(typeof(Camera))]
    public class MinimapController : MonoBehaviour
    {

        #region Serialized Fields

        [SerializeField]
        private bool _startEnabled = false;
        [SerializeField]
        private Transform _mainCameraTransform;

        #endregion

        #region Private Fields

        private Camera _camera;
        private IDisposable _setEnabledSubsription;
        private bool _isEnabled;

        #endregion

        #region Properties

        public bool IsEnabled
        {
            get {  return _isEnabled; }
            private set 
            { 
                _isEnabled = value;
                _camera.enabled = _isEnabled;
            }
        }

        #endregion

        #region Private Methods

        private void Awake()
        {
            _camera = GetComponentInChildren<Camera>();
            IsEnabled = true;
            _setEnabledSubsription = GlobalMessagePipe.GetSubscriber<MinimapSetEnabledEvent>().Subscribe(e => SetMinimapEnabled(e.IsEnabled));
            if (_mainCameraTransform == null)
            {
                _mainCameraTransform = Camera.main.transform;
            }
        }

        private void Start()
        {
            // Delay disable to prewarm the render texture
            if (!_startEnabled)
            {
                StartCoroutine(DelayDisable());
            }
        }

        private IEnumerator DelayDisable()
        {
            yield return null;
            IsEnabled = false;
        }

        private void OnDestroy()
        {
            _setEnabledSubsription?.Dispose();
        }

        private void SetMinimapEnabled(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        private void LateUpdate()
        {
            if (IsEnabled)
            {
                if(_mainCameraTransform != null)
                {
                    Vector3 targetEuler = transform.eulerAngles;
                    targetEuler.y = _mainCameraTransform.transform.eulerAngles.y;
                    transform.eulerAngles = targetEuler;
                    
                    Vector3 position = _mainCameraTransform.position;
                    position.y = transform.position.y;
                    transform.position = position;
                }
            }
        }

        #endregion
    }
}
