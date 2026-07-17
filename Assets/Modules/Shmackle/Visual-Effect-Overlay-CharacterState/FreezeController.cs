// Assets/_Shmackle/Scripts/Effects/Controllers/FreezeController.cs
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class FreezeController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField]
    private Material freezeEffectPrototype; // Material cho hiệu ứng trên body

    [Header("Screen Freeze Effect")]
    [Tooltip("Renderer của object hiệu ứng đóng băng toàn màn hình (ví dụ: FrostPP).")]
    [SerializeField, Required]
    private Renderer screenFreezeRenderer;
    [SerializeField] private bool useScreenFreezeEffect = false;

    [Header("UI Components")]
    [Tooltip("Thanh tiến trình hiển thị thời gian đóng băng còn lại.")]
    [SerializeField, Required]
    private BillProgress freezeProgressBar;
    [SerializeField, Required]
    private GameObject freezeUI;

    [Header("Dependencies")]
    [SerializeField] private MaterialEffectApplier effectApplier;
    [SerializeField, Required] private ShmackleNetworkRig _networkRig;

    private Material _instancedBodyMaterial;
    private Tween _bodyTransitionTween;
    private static readonly int TransitionProgressID = Shader.PropertyToID("_TransitionProgress");

    private Material _instancedScreenMaterial;
    private Tween _screenFreezeTween;
    private static readonly int FrostAmountID = Shader.PropertyToID("_FrostAmount");

    private bool _isEffectActive = false;
    private Tween _progressTween;

    private void Awake()
    {
        InitializeMaterials();
        if (freezeProgressBar != null)
        {
            freezeProgressBar.gameObject.SetActive(false);
        }
    }

    public void EnableFreeze(float duration)
    {
        if (_isEffectActive || _instancedBodyMaterial == null || _instancedScreenMaterial == null) return;
        _isEffectActive = true;

        _instancedBodyMaterial.SetFloat(TransitionProgressID, 0f);
        effectApplier.ApplyEffectMaterialToBody(_instancedBodyMaterial);
        TransitionBodyMaterial(1f, 0.5f, null);
        AnimateScreenAndProgress(duration);
    }

    public void DisableFreeze(float transitionDuration)
    {
        if (!_isEffectActive || _instancedBodyMaterial == null || _instancedScreenMaterial == null) return;
        _isEffectActive = false;

        // 1. Dừng thanh progress bar
        _progressTween?.Kill();
        if (freezeProgressBar != null)
        {
            freezeProgressBar.gameObject.SetActive(false);
        }
        freezeUI.SetActive(false);
        if (screenFreezeRenderer != null && useScreenFreezeEffect)
            StopScreenFreeze(transitionDuration);
        TransitionBodyMaterial(0f, transitionDuration, () =>
        {
            effectApplier.RemoveEffectMaterialFromBody(_instancedBodyMaterial);
        });
    }

    public void EnableFreezeUI(bool isEnable)
    {
        freezeUI.SetActive(isEnable);
        freezeProgressBar.gameObject.SetActive(isEnable);
    }

    private void AnimateScreenAndProgress(float totalDuration)
    {
        if (freezeProgressBar == null || _networkRig == null || !_networkRig.IsLocalNetworkRig)
        {
            return;
        }

        if (screenFreezeRenderer != null && useScreenFreezeEffect)
        {
            screenFreezeRenderer.gameObject.SetActive(true);
            _screenFreezeTween?.Kill();
            _instancedScreenMaterial.SetFloat(FrostAmountID, 0f);
            _screenFreezeTween = _instancedScreenMaterial.DOFloat(1.015f, FrostAmountID, 1)
                .SetEase(Ease.Linear)
                .SetUpdate(true);
        }

        freezeUI.SetActive(true);
        freezeProgressBar.gameObject.SetActive(true);
        freezeProgressBar.SetNormalizedProgress(1f);

        _progressTween?.Kill();
        _progressTween = DOTween.To(
                () => 1f,
                value => freezeProgressBar.SetNormalizedProgress(value),
                0f,
                totalDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                freezeProgressBar.gameObject.SetActive(false);
            });
    }

    private void StopScreenFreeze(float fadeOutDuration)
    {
        if (screenFreezeRenderer == null || _networkRig == null || !_networkRig.IsLocalNetworkRig)
        {
            return;
        }

        _screenFreezeTween?.Kill();
        _screenFreezeTween = _instancedScreenMaterial.DOFloat(0f, FrostAmountID, 1)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                screenFreezeRenderer.gameObject.SetActive(false);
            });
    }

    private void InitializeMaterials()
    {
        if (freezeEffectPrototype != null)
        {
            _instancedBodyMaterial = new Material(freezeEffectPrototype);
        }

        if (screenFreezeRenderer != null)
        {
            _instancedScreenMaterial = screenFreezeRenderer.material;
            screenFreezeRenderer.gameObject.SetActive(false);
        }
    }

    private void TransitionBodyMaterial(float targetProgress, float duration, TweenCallback onCompleteAction)
    {
        _bodyTransitionTween?.Kill();

        if (_instancedBodyMaterial == null)
        {
            onCompleteAction?.Invoke();
            return;
        }

        _bodyTransitionTween = _instancedBodyMaterial.DOFloat(targetProgress, TransitionProgressID, duration)
            .SetEase(Ease.Linear)
            .OnComplete(onCompleteAction)
            .SetUpdate(true);
    }

    private void OnDestroy()
    {
        _bodyTransitionTween?.Kill();
        _screenFreezeTween?.Kill();
        _progressTween?.Kill();

        if (_instancedBodyMaterial != null)
        {
            Destroy(_instancedBodyMaterial);
        }
        if (_instancedScreenMaterial != null)
        {
            Destroy(_instancedScreenMaterial);
        }
    }

    private void OnValidate()
    {
        if (effectApplier == null)
        {
            effectApplier = GetComponent<MaterialEffectApplier>();
        }
    }
}