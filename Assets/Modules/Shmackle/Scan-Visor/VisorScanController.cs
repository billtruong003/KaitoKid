using UnityEngine;
using DG.Tweening;
using System;
using Unity.VisualScripting;

[RequireComponent(typeof(Renderer))]
public class VisorScanTween : MonoBehaviour
{
    [SerializeField] private float _duration = 1.5f;
    [SerializeField] private Ease _easeType = Ease.OutExpo;
    [SerializeField] private Action onComplete;

    private Material _targetMaterial;
    private Tween _activeTween;
    private static readonly int _ProgressID = Shader.PropertyToID("_EffectProgress");

    private void Awake()
    {
        _targetMaterial = GetComponent<Renderer>().material;
        _targetMaterial.SetFloat(_ProgressID, 0f);
    }

    private void OnDestroy()
    {
        _activeTween?.Kill();
    }

    public void ScanOn()
    {
        RunScanTween(1f, onComplete);
    }

    public void ScanOff()
    {
        RunScanTween(0f, onComplete);
    }

    private void RunScanTween(float targetValue, Action onComplete)
    {
        _activeTween?.Kill();

        _activeTween = _targetMaterial.DOFloat(targetValue, _ProgressID, _duration)
            .SetEase(_easeType)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
                this.gameObject.SetActive(targetValue < 1);
                _activeTween = null;
            });
    }

    // Editor Testing Buttons
    [ContextMenu("Trigger Scan ON")]
    private void TestScanOn() => ScanOn();

    [ContextMenu("Trigger Scan OFF")]
    private void TestScanOff() => ScanOff();
}