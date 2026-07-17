// Assets/_Shmackle/Shaders/BillsToon/OptimizeVRDissolve/Shader/Mods Menu/DripPreviewEffect.cs
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Shmackle.Runtime.Player.Drips;

public enum PreviewEffectType
{
    Freeze,
    Gert,
    XRayStandard,
    XRayBloodJman,
    DripEffect
}

[AddComponentMenu("Shmackle/Effects/Drip Preview Effect Controller")]
public class DripPreviewEffect : MonoBehaviour
{
    #region Dependencies and Configuration

    [TitleGroup("Core Dependencies")]
    [Required, SerializeField] private FreezeController freezeController;
    [Required, SerializeField] private XRayController xrayController;
    [Required, SerializeField] private GertController gertController;
    [Required, SerializeField] private DripEffectController dripEffectController;
    [Required, SerializeField] private DripBlendShapeController dripBlendShapeController;
    
    [TitleGroup("X-Ray Wall Preview")]
    [SerializeField] private GameObject protobroOrgans;
    [SerializeField] private GameObject protobroRibs;
    [Required, SerializeField] private GameObject wallToPreviewXray;
    [SerializeField] private float wallDissolveDuration = 0.3f;
    [SerializeField, Range(-2f, 2f)] private float wallVisibleValue = 2f;
    [SerializeField, Range(-2f, 2f)] private float wallDissolvedValue = -2f;

    [TitleGroup("General Configuration")]
    [SerializeField] private float defaultTransitionDuration = 0.5f;

    private Material _wallMaterialInstance;
    private Sequence _masterSequence;
    private PreviewEffectType? _activeEffectType;
    private XRayController.XRayType _activeXRayType;

    private static readonly int DissolveThresholdID = Shader.PropertyToID("_DissolveThreshold");

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeWall();
        ResetAllEffectsToDefaultState();
    }
    private void OnEnable()
    {
        wallToPreviewXray.SetActive(false);
        protobroRibs.SetActive(false);
        protobroOrgans.SetActive(false);
    }

    private void OnDisable()
    {
        wallToPreviewXray.SetActive(false);
        protobroRibs.SetActive(false);
        protobroOrgans.SetActive(false);
        DisableAllEffects(true);
    }
    private void OnDestroy()
    {
        _masterSequence?.Kill();
        if (_wallMaterialInstance != null)
        {
            Destroy(_wallMaterialInstance);
        }
    }

    #endregion

    #region Public API

    public void ApplyPreviewEffect(PreviewEffectType effectType, bool enable, bool immediate = false)
    {
        if (enable)
        {
            if (_activeEffectType.HasValue && _activeEffectType.Value == effectType) return;
            SetEffect(effectType, immediate);
        }
        else
        {
            if (!_activeEffectType.HasValue || _activeEffectType.Value != effectType) return;
            SetEffect(null, immediate);
        }
    }

    public void ReapplyActiveEffect()
    {
        if (!_activeEffectType.HasValue) return;

        _masterSequence?.Kill();
        _masterSequence = DOTween.Sequence();

        AppendEnableEffectLogic(_activeEffectType.Value, true);

        _masterSequence.Play();
    }

    [Button(ButtonSizes.Large), GUIColor(0.9f, 0.4f, 0.4f)]
    [FoldoutGroup("Master Controls")]
    public void DisableAllEffects(bool immediate = false)
    {
        SetEffect(null, immediate);
    }
    public void DisableWallXrayIfExist()
    {
        if (wallToPreviewXray != null && wallToPreviewXray.activeSelf)
        {
            wallToPreviewXray.SetActive(false);
            protobroRibs.SetActive(false);
            protobroOrgans.SetActive(false);
        }
    }
    #endregion

    #region Core Logic

    private void SetEffect(PreviewEffectType? newEffect, bool immediate)
    {
        _masterSequence?.Kill();
        _masterSequence = DOTween.Sequence();

        PreviewEffectType? oldEffect = _activeEffectType;
        _activeEffectType = newEffect;

        var oldImmediate = immediate;
        if (oldEffect == PreviewEffectType.DripEffect)
        {
            oldImmediate = true;
        }
        
        AppendDisableEffectLogic(oldEffect, oldImmediate);

        if (newEffect.HasValue)
        {
            AppendEnableEffectLogic(newEffect.Value, immediate);
        }

        _masterSequence.Play();
    }

    private void AppendDisableEffectLogic(PreviewEffectType? effectToDisable, bool immediate)
    {
        if (!effectToDisable.HasValue) return;

        float duration = immediate ? 0f : defaultTransitionDuration;

        switch (effectToDisable.Value)
        {
            case PreviewEffectType.Freeze:
                _masterSequence.AppendCallback(() => freezeController.DisableFreeze(duration));
                break;
            case PreviewEffectType.Gert:
                _masterSequence.AppendCallback(() => gertController.DisableGert(duration));
                break;
            case PreviewEffectType.XRayStandard:
            case PreviewEffectType.XRayBloodJman:
                xrayController.SetXRayActive(false, _activeXRayType);
                protobroRibs.SetActive(false);
                protobroOrgans.SetActive(false);
                _masterSequence.Append(_wallMaterialInstance.DOFloat(wallDissolvedValue, DissolveThresholdID, wallDissolveDuration))
                               .OnComplete(() =>
                               {
                                   wallToPreviewXray.SetActive(false);
                               });
                break;
            case PreviewEffectType.DripEffect:
                                _masterSequence.AppendCallback(() => {
                                   if (dripEffectController != null)
                                   {
                                       dripEffectController.Apply(false, immediate);
                                       dripBlendShapeController.EnableAutoBlendShapePart(true);
                                   }
                                });
                                break;
        }
    }

    private void AppendEnableEffectLogic(PreviewEffectType effectToEnable, bool immediate)
    {
        float duration = immediate ? 0f : defaultTransitionDuration;

        switch (effectToEnable)
        {
            case PreviewEffectType.Freeze:
                _masterSequence.AppendCallback(() => freezeController.EnableFreeze(duration));
                break;
            case PreviewEffectType.Gert:
                _masterSequence.AppendCallback(() => gertController.EnableGert(duration));
                break;
            case PreviewEffectType.XRayStandard:
            case PreviewEffectType.XRayBloodJman:
                _activeXRayType = (effectToEnable == PreviewEffectType.XRayStandard)
                                ? XRayController.XRayType.Standard
                                : XRayController.XRayType.BloodJman;

                _masterSequence.AppendCallback(() =>
                {
                    wallToPreviewXray.SetActive(true);
                    _wallMaterialInstance.SetFloat(DissolveThresholdID, wallDissolvedValue);
                })
                .AppendCallback(() =>
                {
                    xrayController.SetXRayActive(true, _activeXRayType);
                    _wallMaterialInstance.DOFloat(wallVisibleValue, DissolveThresholdID, wallDissolveDuration);
                }).OnComplete(() =>
                {
                    protobroRibs.SetActive(true);
                    protobroOrgans.SetActive(true);
                });

                break;
            
            case PreviewEffectType.DripEffect:
                _masterSequence.AppendCallback(() =>
                {
                    if (dripEffectController != null)
                    {
                        dripEffectController.Apply(true, immediate);
                        dripBlendShapeController.EnableAutoBlendShapePart(false);
                    }
                });
                break;
        }
    }

    private void InitializeWall()
    {
        if (wallToPreviewXray == null) return;

        var wallRenderer = wallToPreviewXray.GetComponent<Renderer>();
        if (wallRenderer != null)
        {
            _wallMaterialInstance = wallRenderer.material;
        }
        wallToPreviewXray.SetActive(false);
        protobroRibs.SetActive(false);
        protobroOrgans.SetActive(false);
    }

    private void ResetAllEffectsToDefaultState()
    {
        _activeEffectType = null;
        freezeController.DisableFreeze(0f);
        gertController.DisableGert(0f);
        xrayController.SetXRayActive(false, XRayController.XRayType.Standard);
        xrayController.SetXRayActive(false, XRayController.XRayType.BloodJman);

        if (_wallMaterialInstance != null)
        {
            _wallMaterialInstance.SetFloat(DissolveThresholdID, wallVisibleValue);
        }
        if (wallToPreviewXray != null)
        {
            wallToPreviewXray.SetActive(false);
            protobroRibs.SetActive(false);
            protobroOrgans.SetActive(false);
        }
    }

    #endregion

    #region Editor Test Buttons

    [TabGroup("Effect Testers", "Freeze")]
    [Button("Enable")] public void TestEnableFreeze() => ApplyPreviewEffect(PreviewEffectType.Freeze, true);

    [TabGroup("Effect Testers", "Freeze")]
    [Button("Disable")] public void TestDisableFreeze() => ApplyPreviewEffect(PreviewEffectType.Freeze, false);

    [TabGroup("Effect Testers", "Gert")]
    [Button("Enable")] public void TestEnableGert() => ApplyPreviewEffect(PreviewEffectType.Gert, true);

    [TabGroup("Effect Testers", "Gert")]
    [Button("Disable")] public void TestDisableGert() => ApplyPreviewEffect(PreviewEffectType.Gert, false);

    [TabGroup("Effect Testers", "X-Ray")]
    [Button("Enable BloodJman")] public void TestEnableXRay() => ApplyPreviewEffect(PreviewEffectType.XRayBloodJman, true);

    [TabGroup("Effect Testers", "X-Ray")]
    [Button("Disable BloodJman")] public void TestDisableXRay() => ApplyPreviewEffect(PreviewEffectType.XRayBloodJman, false);
    
    [TabGroup("Effect Testers", "Drip Effect")]
    [Button("Enable")] public void TestEnableDripEffect() => ApplyPreviewEffect(PreviewEffectType.DripEffect, true);

    [TabGroup("Effect Testers", "Drip Effect")]
    [Button("Disable")] public void TestDisableDripEffect()
    {
        //ApplyPreviewEffect(PreviewEffectType.DripEffect, false);
        _activeEffectType = null;
        dripEffectController.Apply(false);
        dripBlendShapeController.EnableAutoBlendShapePart(true);
    }

    #endregion
}