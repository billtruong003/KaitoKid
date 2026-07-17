#if STDB_BINDINGS
using UnityEngine;
using TMPro;
using BillGameCore;

namespace SpumOnline
{
    /// <summary>
    /// Type of damage popup to display, affecting color and style.
    /// </summary>
    public enum DamagePopupType
    {
        Normal,
        Crit,
        Heal
    }

    /// <summary>
    /// Floating damage number that spawns at a world position, floats upward while
    /// fading out, and auto-returns to the pool. Uses BillTween for all animations.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class DamagePopup : PooledObject
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("Animation")]
        [SerializeField] private float floatDistance = 1.2f;
        [SerializeField] private float animationDuration = 1.0f;
        [SerializeField] private float scalePunchSize = 1.5f;
        [SerializeField] private float scalePunchDuration = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color critColor = new Color(1f, 0.9f, 0.1f, 1f); // Yellow
        [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.2f, 1f);  // Green

        [Header("Font Sizes")]
        [SerializeField] private float normalFontSize = 5f;
        [SerializeField] private float critFontSize = 7f;
        [SerializeField] private float healFontSize = 5f;

        // -------------------------------------------------------
        // References
        // -------------------------------------------------------

        private TextMeshPro _text;
        private Tween _moveTween;
        private Tween _fadeTween;
        private Tween _scaleTween;

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Awake()
        {
            _text = GetComponent<TextMeshPro>();

            // Configure TMP defaults
            _text.alignment = TextAlignmentOptions.Center;
            _text.sortingOrder = 100;

            // Ensure the text renders in world space
            _text.rectTransform.sizeDelta = new Vector2(3f, 1.5f);
        }

        // -------------------------------------------------------
        // PooledObject overrides
        // -------------------------------------------------------

        public override void OnSpawnedFromPool()
        {
            // Reset alpha and scale on spawn
            if (_text != null)
            {
                Color c = _text.color;
                c.a = 1f;
                _text.color = c;
            }
            transform.localScale = Vector3.one;
        }

        public override void OnReturnedToPool()
        {
            // Kill any running tweens
            KillActiveTweens();
        }

        // -------------------------------------------------------
        // Initialize
        // -------------------------------------------------------

        /// <summary>
        /// Set up the damage popup with amount and type, then animate it.
        /// </summary>
        public void Initialize(int amount, DamagePopupType type)
        {
            if (_text == null) _text = GetComponent<TextMeshPro>();

            // Kill any lingering tweens from previous use
            KillActiveTweens();

            // Set text
            switch (type)
            {
                case DamagePopupType.Heal:
                    _text.text = $"+{amount}";
                    _text.color = healColor;
                    _text.fontSize = healFontSize;
                    break;

                case DamagePopupType.Crit:
                    _text.text = $"{amount}!";
                    _text.color = critColor;
                    _text.fontSize = critFontSize;
                    break;

                case DamagePopupType.Normal:
                default:
                    _text.text = amount.ToString();
                    _text.color = normalColor;
                    _text.fontSize = normalFontSize;
                    break;
            }

            // Reset scale and alpha
            transform.localScale = Vector3.one;
            Color color = _text.color;
            color.a = 1f;
            _text.color = color;

            // Start animations
            AnimatePopup(type);
        }

        // -------------------------------------------------------
        // Animation
        // -------------------------------------------------------

        private void AnimatePopup(DamagePopupType type)
        {
            // Float upward
            float startY = transform.position.y;
            float endY = startY + floatDistance;

            _moveTween = BillTween.MoveY(transform, endY, animationDuration)
                ?.SetEase(EaseType.OutQuad)
                .SetTarget(this);

            // Fade out (start fading after 30% of the duration)
            float fadeDelay = animationDuration * 0.3f;
            float fadeDuration = animationDuration * 0.7f;

            _fadeTween = BillTween.Float(1f, 0f, fadeDuration, alpha =>
            {
                if (_text != null)
                {
                    Color c = _text.color;
                    c.a = alpha;
                    _text.color = c;
                }
            })?.SetDelay(fadeDelay)
              .SetEase(EaseType.InQuad)
              .SetTarget(this);

            // Scale punch on spawn (pop in, then settle)
            _scaleTween = BillTween.Scale(transform, scalePunchSize, scalePunchDuration)
                ?.SetEase(EaseType.OutBack)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    // Scale back to 1
                    BillTween.Scale(transform, 1f, scalePunchDuration)
                        ?.SetEase(EaseType.InOutSine)
                        .SetTarget(this);
                });

            // Crits get extra scaling emphasis
            if (type == DamagePopupType.Crit)
            {
                transform.localScale = Vector3.one * 0.5f;
                _scaleTween = BillTween.Scale(transform, scalePunchSize * 1.3f, scalePunchDuration)
                    ?.SetEase(EaseType.OutBack)
                    .SetTarget(this)
                    .OnComplete(() =>
                    {
                        BillTween.Scale(transform, 1f, scalePunchDuration * 1.5f)
                            ?.SetEase(EaseType.InOutSine)
                            .SetTarget(this);
                    });
            }

            // Auto-return to pool after the animation finishes
            if (Bill.IsReady)
            {
                Bill.Timer.Delay(animationDuration + 0.1f, () =>
                {
                    if (gameObject != null && gameObject.activeInHierarchy)
                    {
                        this.ReturnToPool();
                    }
                });
            }
        }

        private void KillActiveTweens()
        {
            if (_moveTween != null) { BillTween.Kill(_moveTween); _moveTween = null; }
            if (_fadeTween != null) { BillTween.Kill(_fadeTween); _fadeTween = null; }
            if (_scaleTween != null) { BillTween.Kill(_scaleTween); _scaleTween = null; }
            BillTween.KillTarget(this);
        }
    }
}

#endif // STDB_BINDINGS
