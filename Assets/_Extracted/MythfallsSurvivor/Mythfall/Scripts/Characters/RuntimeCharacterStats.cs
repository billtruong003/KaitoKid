using System.Collections.Generic;

namespace Mythfall.Characters
{
    /// <summary>
    /// Mutable stat container scoped to a single run. Built from a CharacterBaseStats clone,
    /// then modified by upgrade cards (Sprint 3). Final = base + flat + base * (percent / 100).
    /// </summary>
    public class RuntimeCharacterStats
    {
        readonly Dictionary<StatType, float> _baseValues = new(11);
        readonly Dictionary<StatType, float> _flatModifiers = new(11);
        readonly Dictionary<StatType, float> _percentModifiers = new(11);

        public RuntimeCharacterStats(CharacterBaseStats source)
        {
            foreach (StatType s in System.Enum.GetValues(typeof(StatType)))
                _baseValues[s] = source.Get(s);
        }

        public float GetBase(StatType type) => _baseValues.TryGetValue(type, out var v) ? v : 0f;

        public float GetFinal(StatType type)
        {
            float baseVal = GetBase(type);
            float flat = _flatModifiers.TryGetValue(type, out var f) ? f : 0f;
            float pct = _percentModifiers.TryGetValue(type, out var p) ? p : 0f;
            return baseVal + flat + baseVal * (pct / 100f);
        }

        public void AddFlat(StatType type, float amount)
        {
            _flatModifiers.TryGetValue(type, out var current);
            _flatModifiers[type] = current + amount;
        }

        public void AddPercent(StatType type, float percent)
        {
            _percentModifiers.TryGetValue(type, out var current);
            _percentModifiers[type] = current + percent;
        }

        public void ResetModifiers()
        {
            _flatModifiers.Clear();
            _percentModifiers.Clear();
        }
    }
}
