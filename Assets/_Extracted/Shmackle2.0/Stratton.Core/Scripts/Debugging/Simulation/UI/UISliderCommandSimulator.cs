using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Stratton.Debugging.UI
{
    /// <summary>
    /// Uses the first argument of a SimulatedCommandInfo as a float to send a command and can be adjusted as a slider.
    /// Other fixed arguments can still follow.
    /// </summary>
    public class UISliderCommandSimulator : UICommandSimulatorBase
    {
        #region Serialized Fields

        [SerializeField]
        private Slider _slider;
        [SerializeField]
        private TextMeshProUGUI _valueLabel;

        #endregion

        #region Protected Methods

        protected override void Awake()
        {
            base.Awake();
            if (!_slider)
            {
                _slider = GetComponentInChildren<Slider>();
            }
            _slider.onValueChanged.AddListener(OnSliderValueChanged);
            if (!_valueLabel)
            {
                _valueLabel = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        protected void OnSliderValueChanged(float newValue)
        {
            string valueStr = newValue.ToString(newValue % 1 == 0 ? "F0" : "F2"); // Remove decimal if whole number
            _valueLabel.text = valueStr;
            _simulatedCommandInfo.Arguments[0] = valueStr;
        }

        #endregion

        #region ICommandSimulator

        public override void Init(SimulatedCommandInfo simulatedCommandInfo)
        {
            base.Init(simulatedCommandInfo);
            if (_simulatedCommandInfo.Arguments == null || _simulatedCommandInfo.Arguments.Length == 0)
            {
                _simulatedCommandInfo.Arguments = new string[]
                {
                    _simulatedCommandInfo.Min.ToString()
                };
            }
            _slider.minValue = _simulatedCommandInfo.Min;
            _slider.maxValue = _simulatedCommandInfo.Max;
            float currentValue;
            if (!float.TryParse(_simulatedCommandInfo.Arguments[0], out currentValue))
            {
                currentValue = _simulatedCommandInfo.Min;
                _simulatedCommandInfo.Arguments[0] = currentValue.ToString();
            }
            _slider.value = currentValue;
        }

        #endregion
    }
}