using System.Collections;
using Teabag.Core;
using TMPro;
using UnityEngine;

namespace Teabag.UI.Quest
{
    /// <summary>
    /// Individual notification toast that handles its own animation and lifecycle.
    /// Used by QuestManager.
    /// </summary>
    public class QuestNotificationToast : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _rewardText;

        private QuestConfig _config;
        private QuestManager _manager;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        
        private Vector2 _targetPosition;
        private Coroutine _moveCoroutine;
        private Coroutine _fadeCoroutine;
        private WaitForSeconds _waitDisplayDuration;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            _rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Populates the toast with quest data and starts the slide-in animation.
        /// </summary>
        public void Initialize(QuestSnapshot snapshot, QuestConfig config, QuestManager manager, Vector2 baseAnchorPosition)
        {
            _config = config;
            _manager = manager;

            if (_titleText != null) _titleText.text = snapshot.Name;
            if (_descriptionText != null) _descriptionText.text = snapshot.Description;
            if (_rewardText != null) _rewardText.text = $"+{snapshot.RewardAmount}";

            if (_waitDisplayDuration == null)
            {
                _waitDisplayDuration = new WaitForSeconds(_config.DisplayDuration);
            }

            // Target is the anchor point on the screen
            _targetPosition = baseAnchorPosition;
            
            // Start to the left of the anchor
            _rectTransform.anchoredPosition = _targetPosition + new Vector2(-_config.HorizontalSlideOffset, 0);

            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

            SetAlpha(_config.BaseAlpha);
            _moveCoroutine = StartCoroutine(SlideInCoroutine());
        }

        /// <summary>
        /// Shifts the notification up to make room for a new one.
        /// </summary>
        public void PushUp()
        {
            _targetPosition.y += _config.VerticalSpacing;
            
            if (gameObject.activeInHierarchy)
            {
                if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
                _moveCoroutine = StartCoroutine(PushUpCoroutine());
            }
        }

        private void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = alpha;
        }

        private IEnumerator SlideInCoroutine()
        {
            float elapsed = 0;
            Vector2 start = _rectTransform.anchoredPosition;
            while (elapsed < _config.SlideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / _config.SlideInDuration);
                _rectTransform.anchoredPosition = Vector2.Lerp(start, _targetPosition, t);
                yield return null;
            }
            _rectTransform.anchoredPosition = _targetPosition;

            // Start the wait-and-fade timer in a separate coroutine so it isn't cancelled by PushUp.
            StartCoroutine(WaitAndFadeCoroutine());
        }

        private IEnumerator WaitAndFadeCoroutine()
        {
            yield return _waitDisplayDuration;
            _fadeCoroutine = StartCoroutine(FadeOutCoroutine());
        }

        private IEnumerator PushUpCoroutine()
        {
            float elapsed = 0;
            Vector2 start = _rectTransform.anchoredPosition;
            while (elapsed < _config.PushUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / _config.PushUpDuration);
                _rectTransform.anchoredPosition = Vector2.Lerp(start, _targetPosition, t);
                yield return null;
            }
            _rectTransform.anchoredPosition = _targetPosition;
        }

        private IEnumerator FadeOutCoroutine()
        {
            float elapsed = 0;
            float startAlpha = _config.BaseAlpha;
            while (elapsed < _config.FadeOutDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(startAlpha * (1f - (elapsed / _config.FadeOutDuration)));
                yield return null;
            }
            SetAlpha(0f);

            if (_manager != null)
            {
                _manager.ReturnToPool(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
