using NaughtyAttributes;
using UnityEngine;

namespace Stratton.Debugging
{
    [System.Serializable]
    public struct SimulatedCommandInfo
    {
        public string DisplayName;
        public string Command;

        [Header("Optional Fields")]

        [NaughtyAttributes.ValidateInput(nameof(LessThanMax), message: "Min must be Lower than Max (or both 0).")]
        [AllowNesting]
        public float Min;
        [NaughtyAttributes.ValidateInput(nameof(GreaterThanMin), message: "Max must be Greater than Min (or both 0).")]
        [AllowNesting]
        public float Max;
        public float Adjustment;
        public ScriptableObject ScriptableObject;
        public string[] Options;

        [Header("Default")]
        public string[] Arguments;

        public override readonly string ToString()
        {
            string args = string.Join(" ", Arguments);
            return $"{Command} {args}";
        }

        #region Input Validation
        private readonly bool LessThanMax(float value)
        {
            return value < Max || value == 0 && Max == 0;
        }

        private readonly bool GreaterThanMin(float value)
        {
            return value > Min || value == 0 && Min == 0;
        }

        #endregion
    }
}
