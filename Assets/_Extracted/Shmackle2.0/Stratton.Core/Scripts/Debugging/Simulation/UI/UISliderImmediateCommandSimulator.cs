using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Opencoding.CommandHandlerSystem;
using Stratton.Core;
using System.Globalization;
using System.Reflection;

namespace Stratton.Debugging.UI
{
    /// <summary>
    /// Uses the first argument of a SimulatedCommandInfo as a fieldName to init a slider from a ScriptableObject field, and then adjust that field via the slider. 
    /// Other fixed arguments can still follow.
    /// </summary>
    public class UISliderImmediateCommandSimulator : MonoBehaviour, ICommandSimulator
    {
        private const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public;

        #region Fields

        [SerializeField]
        private TextMeshProUGUI _displayTextLabel;
        [SerializeField]
        private Slider _slider;
        [SerializeField]
        private TextMeshProUGUI _valueLabel;
        [SerializeField]
        private bool _isInteger;

        protected SimulatedCommandInfo _simulatedCommandInfo;

        #endregion

        #region Methods

        private void InitSlider(float newValue)
        {
            _slider.wholeNumbers = _isInteger;
            string valueStr = GetValueString(newValue);
            _slider.value = newValue;
            _valueLabel.text = valueStr;
            _slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        protected virtual void Awake()
        {
            if (!_slider)
            {
                _slider = GetComponentInChildren<Slider>();
            }
            if (!_valueLabel)
            {
                _valueLabel = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        protected void OnSliderValueChanged(float newValue)
        {
            string valueStr = GetValueString(newValue);
            _valueLabel.text = valueStr;
            Execute();
        }

        protected string GetValueString(float value) => value.ToString(value % 1 == 0 ? "F0" : "F2"); // Remove decimal if whole number

        #endregion

        #region ICommandSimulator

        public virtual void Init(SimulatedCommandInfo simulatedCommandInfo)
        {
            _simulatedCommandInfo = simulatedCommandInfo;

            if (_displayTextLabel)
            {
                _displayTextLabel.SetText(_simulatedCommandInfo.DisplayName);
            }

            var type = _simulatedCommandInfo.ScriptableObject.GetType();

            if (_simulatedCommandInfo.Arguments == null || _simulatedCommandInfo.Arguments.Length == 0)
            {
                Log.Error(BaseLogChannel.Debug, $"No argument supplied, Arguments[0] needs to be the name of a public field (float) in '{type.Name}'.");
                string s = "[";
                var publicFields = type.GetFields(BINDING_FLAGS);
                foreach (var f in publicFields)
                {
                    s += f.Name + ", ";
                }
                s = s.TrimEnd(',', ' ');
                s += "].";
                Log.Message(BaseLogChannel.Debug, $"ScriptableObject '{type.Name}' Public Fields: " + s);
                return;
            }
            _slider.minValue = _simulatedCommandInfo.Min;
            _slider.maxValue = _simulatedCommandInfo.Max;

            var fieldName = _simulatedCommandInfo.Arguments[0].Trim();
            var field = type.GetField(fieldName, BINDING_FLAGS);
            if (field == null)
            {
                Log.Error(BaseLogChannel.Debug, $"field '{type.Name}.{fieldName}' does not exist.");
                return;
            }
            var fieldValue = field.GetValue(_simulatedCommandInfo.ScriptableObject);
            if (!float.TryParse(fieldValue.ToString(), out float floatValue))
            {
                Log.Error(BaseLogChannel.Debug, $"Failed to parse '{type.Name}.{field.Name}' into float.");
                return;
            }
                
            InitSlider(floatValue);
        }

        public string GetCommand()
        {
            var commandInfo = _simulatedCommandInfo;
            commandInfo.Arguments = new string[]
            {
                _simulatedCommandInfo.Arguments[0], 
                _slider.value.ToString(CultureInfo.InvariantCulture),
                _simulatedCommandInfo.ScriptableObject.name
            };
            return commandInfo.ToString();
        }

        public void Execute()
        {
            CommandHandlers.HandleCommand(GetCommand());
        }

        #endregion
    }
}