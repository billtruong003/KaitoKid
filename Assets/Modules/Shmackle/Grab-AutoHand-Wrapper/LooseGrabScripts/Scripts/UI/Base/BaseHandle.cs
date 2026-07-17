using DG.Tweening;
using NaughtyAttributes;
using Shmackle.Sound;
using UnityEngine;
using UnityEngine.Events;

namespace Shmackle.UI
{
    public class BaseHandle : MonoBehaviour
    {
        [SerializeField]
        private AudioClip _audioClip;

        [SerializeField]
        private Vector3 _rotateUp = new Vector3(-22f, 0, 0);
        [SerializeField]
        private Vector3 _rotateDown = new Vector3(22f, 0, 0);
        [SerializeField]
        private float _rotateDuration = 0.2f;

        [SerializeField] private Tags  _fingerTag             = Tags.Finger;

        [HorizontalLine]
        public UnityEvent OnButtonPressed;

        public  bool IsPressed => _onPressed;
        private bool _onPressed;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_fingerTag.ToString()))
            {
                return;
            }

            OnPress();
        }

        public void OnPress()
        {
            if (!IsAvailable())
            {
                return;
            }

            if (_onPressed)
            {
                return;
            }

            _onPressed = true;

            if (_audioClip != null)
            {
                SoundManager.Instance.PlayOneShot(_audioClip);
            }

            Quaternion upQuat       = Quaternion.Euler(_rotateUp);
            bool       isAtUp       = Quaternion.Angle(transform.localRotation, upQuat) < 0.1f;
            var        rotateTarget = isAtUp ? _rotateDown : _rotateUp;

            transform
                .DOLocalRotate(rotateTarget, _rotateDuration)
                .OnComplete(() =>
                            {
                                OnButtonPressed?.Invoke();
                                _onPressed = false;
                            });
        }

        protected virtual bool IsAvailable()
        {
            return true;
        }
    }
}