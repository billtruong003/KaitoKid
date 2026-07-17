using DG.Tweening;
using UnityEngine;

namespace Shmackle.Animation
{
    public class TweenPopup : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private float _duration = 0.5f;
        [SerializeField]
        private float _offset = -100;
        [SerializeField]
        private Ease _easeType = Ease.OutBack;

        #endregion

        #region Private Fields

        private Vector3 _originalPos;

        #endregion

        #region Private Methods

        private void Awake()
        {
            _originalPos = transform.localPosition;
        }

        private void OnEnable()
        {
            transform.localPosition = _originalPos + Vector3.up * _offset;

            transform.DOKill();

            transform.DOLocalMove(_originalPos, _duration)
                     .SetEase(_easeType);
        }

        #endregion
    }
}