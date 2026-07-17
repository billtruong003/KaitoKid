using Stratton.Core;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace Stratton.Debugging.UI
{
    /// <summary>
    /// A non-abstract command simulator component to trigger whatever simulation info with its default values.
    /// </summary>
    public class UIToggleCommandSimulator : UICommandSimulatorBase
    {
        private const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public;
        
        [SerializeField] private TextMeshProUGUI _valueText;

        private bool _internalToggle;
        
        public override void Init(SimulatedCommandInfo simulatedCommandInfo)
        {
            base.Init(simulatedCommandInfo);
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
            
            var fieldName = _simulatedCommandInfo.Arguments[0].Trim();
            var field = type.GetField(fieldName, BINDING_FLAGS);
            if (field == null)
            {
                Log.Error(BaseLogChannel.Debug, $"field '{type.Name}.{fieldName}' does not exist.");
                return;
            }
            var fieldValue = field.GetValue(_simulatedCommandInfo.ScriptableObject);

            if (!bool.TryParse(fieldValue.ToString(), out bool boolValue))
            {
                Log.Error(BaseLogChannel.Debug, $"Failed to parse '{type.Name}.{field.Name}' into bool.");
                return;
            }
            
            SetToggle(boolValue);
        }

        private void SetToggle(bool value)
        {
            _internalToggle = value;
            _valueText.SetText(value ? "On" : "Off");
        }
        
        public override string GetCommand()
        {
            SetToggle(!_internalToggle);

            var commandInfo = _simulatedCommandInfo;
            commandInfo.Arguments = new string[]
            {
                _simulatedCommandInfo.Arguments[0], 
                _internalToggle.ToString(CultureInfo.InvariantCulture), 
                _simulatedCommandInfo.ScriptableObject.name
            };
            return commandInfo.ToString();
        }
    }
}
