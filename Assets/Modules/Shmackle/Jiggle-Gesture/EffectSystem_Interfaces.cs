// Assets/_Shmackle/Scripts/Effects/EffectSystem_Interfaces.cs
using System;
using UnityEngine;

namespace Shmackle.Effects
{
    public enum EffectType
    {
        GenericDissolve,
        Gert,
        Freeze,
        BloodJmanTransform
    }

    /// <summary>
    /// Giao diện cốt lõi cho mọi hiệu ứng có thể áp dụng lên nhân vật.
    /// Mỗi hiệu ứng sẽ triển khai giao diện này để đóng gói logic của riêng nó.
    /// </summary>
    public interface ICharacterEffect
    {
        EffectType Type { get; }
        void Initialize(MaterialPropertyBlock propertyBlock);
        void Apply(bool enable, float duration, Action onComplete = null);
        void SetRenderers(System.Collections.Generic.IEnumerable<Renderer> renderers);
    }
}