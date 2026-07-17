using DG.Tweening;
using UnityEngine;

namespace Shmackle.Animation
{
    public class TweenYOscillator : MonoBehaviour
    {
        #region Serialilzed Fields
        
        [SerializeField]
        private float _distance = 0.25f;
        [SerializeField]
        private float _duration = 1f;
        [SerializeField]
        private bool _autoStart = true;

        #endregion

        #region Private Fields

        private Tween _moveTween;
        private bool _hasStarted = false;

        #endregion

        #region Private Methods

        private void Start()
        {
            if (_autoStart)
            {
                Restart();
                _hasStarted = true;
            }
        }

        private void OnEnable()
        {
            // For initialization purposes on spawn, make sure it runs only on Start initially
            if (_hasStarted && _autoStart)
            {
                Restart();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        #endregion

        #region Public Methods

        public void Restart()
        {
            Stop();
            float halfDistance = _distance / 2;
            float startY = transform.position.y;
            _moveTween = transform.DOMoveY(startY - halfDistance, _duration / 2)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    _moveTween = transform.DOMoveY(startY + halfDistance, _duration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                });
        }

        public void Stop()
        {
            if (_moveTween != null && _moveTween.IsActive())
            {
                _moveTween.Kill();
            }
        }

        #endregion
    }
}
