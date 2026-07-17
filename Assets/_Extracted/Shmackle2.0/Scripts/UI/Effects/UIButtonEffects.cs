using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shmackle.UI
{
    public class UIButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Serialized Fields

        [SerializeField]
        private RectTransform _mainTransform;
        [Header("Audio")]
        [SerializeField]
        private AudioClip _clickSound;
        [Header("Tween Settings")]
        [SerializeField]
        private float _hoverScale = 0.9f;
        [SerializeField]
        private float _tweenDuration = 0.1f;
        [SerializeField]
        private Ease _scaleEaseType = Ease.OutQuad;

        #endregion

        #region Private Fields

        private Button _button;
        private Vector3 _originalScale;
        private Tweener _currentTween;
        private static AudioSource _audioSource;

        #endregion

        #region Private Methods

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClickButton);

            if (_mainTransform == null)
            {
                _mainTransform = _button.targetGraphic.rectTransform;
            }

            if (_clickSound != null)
            {
                // TODO: Review on audio task. Make a centralized 2D audio source on audio task that won't get deactivated when UI's are transitioned
                if (_audioSource == null)
                {
                    GameObject tempAudio = new GameObject("GenericButtonAudio");
                    _audioSource = tempAudio.AddComponent<AudioSource>();
                    _audioSource.spatialBlend = 0;
                }
            }

            _originalScale = _mainTransform.localScale;
        }

        private void OnEnable()
        {
            _mainTransform.localScale = _originalScale;
        }

        private void OnClickButton()
        {
            if (_clickSound != null)
            {
                _audioSource.PlayOneShot(_clickSound);
            }
        }

        private void PlayTween(float targetScale)
        {
            _currentTween?.Kill();
            _currentTween = _mainTransform.DOScale(_originalScale * targetScale, _tweenDuration).SetEase(_scaleEaseType);
        }

        #endregion

        #region IPointerHandlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayTween(_hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PlayTween(1f);
        }

        #endregion
    }
}
