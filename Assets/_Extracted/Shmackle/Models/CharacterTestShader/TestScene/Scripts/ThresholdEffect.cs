// Assets/_Shmackle/Scripts/Effects/ThresholdEffect.cs
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Shmackle.Data;

namespace Shmackle.Effects
{
    [Serializable]
    public class ThresholdEffectSettings
    {
        public EffectType EffectType;
        [Tooltip("Tên thuộc tính trong Shader, ví dụ: _DissolveAmount, _TransitionProgress")]
        public string ShaderPropertyName;
        [Tooltip("Giá trị bắt đầu của hiệu ứng (thường là trạng thái 'tắt')")]
        public float MinValue = 0f;
        [Tooltip("Giá trị kết thúc của hiệu ứng (thường là trạng thái 'bật')")]
        public float MaxValue = 1f;
        [Tooltip("Vật liệu đặc biệt cần áp dụng khi hiệu ứng kích hoạt. Bỏ trống nếu không cần.")]
        public Material EffectMaterial;
    }

    public class ThresholdEffect : ICharacterEffect
    {
        public EffectType Type => _settings.EffectType;

        private readonly ThresholdEffectSettings _settings;
        private readonly Func<DripData_Runtime, Material> _materialSelector;
        private readonly Action<Func<DripData_Runtime, Material>> _applyMaterialAction;

        private MaterialPropertyBlock _propertyBlock;
        private Tween _activeTween;
        private IEnumerable<Renderer> _targetRenderers;
        private int _propertyID;

        public ThresholdEffect(ThresholdEffectSettings settings, Action<Func<DripData_Runtime, Material>> applyMaterialAction)
        {
            _settings = settings;
            _applyMaterialAction = applyMaterialAction;

            if (settings.EffectMaterial != null)
            {
                _materialSelector = (runtime) => settings.EffectMaterial;
            }
            else
            {
                // Logic để chọn material từ DripData_Runtime, ví dụ cho Gert
                _materialSelector = (runtime) => runtime.CharacterGertMaterial;
            }
        }

        public void Initialize(MaterialPropertyBlock propertyBlock)
        {
            _propertyBlock = propertyBlock;
            _propertyID = Shader.PropertyToID(_settings.ShaderPropertyName);
        }

        public void SetRenderers(IEnumerable<Renderer> renderers)
        {
            _targetRenderers = renderers;
        }

        public void Apply(bool enable, float duration, Action onComplete = null)
        {
            _activeTween?.Kill();

            if (enable)
            {
                _applyMaterialAction(_materialSelector);
            }

            float startValue = enable ? _settings.MinValue : _settings.MaxValue;
            float endValue = enable ? _settings.MaxValue : _settings.MinValue;
            float currentValue = startValue;

            _activeTween = DOTween.To(() => currentValue, x => currentValue = x, endValue, duration)
                .SetEase(Ease.Linear)
                .OnUpdate(() => UpdateShaderProperty(currentValue))
                .OnComplete(() =>
                {
                    if (!enable)
                    {
                        // Khi tắt hiệu ứng, một Action bên ngoài sẽ gọi để trả về material mặc định
                    }
                    onComplete?.Invoke();
                });
        }

        private void UpdateShaderProperty(float value)
        {
            if (_targetRenderers == null) return;

            _propertyBlock.SetFloat(_propertyID, value);
            foreach (var renderer in _targetRenderers)
            {
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }
    }
}