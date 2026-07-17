using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Stratton.Debugging.UI
{
    /// <summary>
    /// Uses the first argument of a SimulatedCommandInfo as a float to send a command and can be adjusted with a +/- button.
    /// Other fixed arguments can still follow.
    /// </summary>
    public class UINumberCommandSimulator : UICommandSimulatorBase
    {
        #region Serialized Fields

        [SerializeField]
        private TextMeshProUGUI _valueLabel;
        [SerializeField]
        private Button _addButton;
        [SerializeField]
        private Button _subtractButton;

        #endregion

        #region Protected Methods

        protected override void Awake()
        {
            base.Awake();
            _addButton.onClick.AddListener(OnAdd);
            _subtractButton.onClick.AddListener(OnSubtract);
        }

        protected void OnAdd()
        {
            AdjustValue(_simulatedCommandInfo.Adjustment);
        }

        protected void OnSubtract()
        {
            AdjustValue(-_simulatedCommandInfo.Adjustment);
        }

        #endregion

        #region Private Methods

        private void AdjustValue(float increment)
        {
            float currentValue = float.Parse(_simulatedCommandInfo.Arguments[0]);
            currentValue += increment;

            if (_simulatedCommandInfo.Min != _simulatedCommandInfo.Max)
            {
                currentValue = Mathf.Clamp(currentValue, _simulatedCommandInfo.Min, _simulatedCommandInfo.Max);
            }

            string newValueStr = currentValue.ToString();
            _simulatedCommandInfo.Arguments[0] = newValueStr;
            _valueLabel.text = newValueStr;
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
            float currentValue;
            if (!float.TryParse(_simulatedCommandInfo.Arguments[0], out currentValue))
            {
                currentValue = _simulatedCommandInfo.Min;
                _simulatedCommandInfo.Arguments[0] = currentValue.ToString();
            }
            _valueLabel.SetText(_simulatedCommandInfo.Arguments[0]);

            bool canAdjustValue = _simulatedCommandInfo.Adjustment > 0;
            _addButton.gameObject.SetActive(canAdjustValue);
            _subtractButton.gameObject.SetActive(canAdjustValue);
        }

        #endregion
    }
}
