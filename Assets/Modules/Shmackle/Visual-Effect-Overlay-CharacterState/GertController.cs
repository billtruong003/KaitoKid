// Đặt file này trong: Assets/_Shmackle/Scripts/Effects/Controllers/
using UnityEngine;
using DG.Tweening;

public class GertController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private CharacterMaterialManager materialManager;

    private Tween _activeTransitionTween;
    private bool _isEffectActive = false;

    private float _currentGertProgress = 0f;

    public void EnableGert(float transitionDuration)
    {
        if (_isEffectActive || materialManager == null) return;
        _isEffectActive = true;

        Debug.Log($"EnableGert transitionDuration {transitionDuration}");

        _activeTransitionTween?.Kill();
        materialManager.ApplyGertMaterials();
        _activeTransitionTween = DOTween.To(() => _currentGertProgress,
                                            x =>
                                            {
                                                _currentGertProgress = x;
                                                materialManager.SetGertShaderProperty(x);
                                            },
                                            1f,
                                            transitionDuration)
            .SetEase(Ease.Linear);
    }

    public void DisableGert(float transitionDuration)
    {
        if (!_isEffectActive || materialManager == null) return;
        _isEffectActive = false;

        Debug.Log($"DisableGert transitionDuration {transitionDuration}");

        _activeTransitionTween?.Kill();

        _activeTransitionTween = DOTween.To(() => _currentGertProgress,
                                            x =>
                                            {
                                                _currentGertProgress = x;
                                                materialManager.SetGertShaderProperty(x);
                                            },
                                            0f,
                                            transitionDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (!_isEffectActive)
                {
                    materialManager.ApplyDefaultMaterials();
                }
            });
    }

    private void OnDestroy()
    {
        _activeTransitionTween?.Kill();
    }
}