using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Stratton.Debugging.UI
{
    public class UIDropdownCommandSimulator : UICommandSimulatorBase
    {
        [SerializeField]
        protected TMP_Dropdown _dropdown;

        #region ICommandSimulator

        public override void Init(SimulatedCommandInfo simulatedCommandInfo)
        {
            base.Init(simulatedCommandInfo);

            if (_simulatedCommandInfo.Arguments == null || _simulatedCommandInfo.Arguments.Length == 0)
            {
                if (_simulatedCommandInfo.Options == null || _simulatedCommandInfo.Options.Length == 0)
                {
                    _simulatedCommandInfo.Arguments = new string[0];
                }
                else
                {
                    _simulatedCommandInfo.Arguments = new string[]
                    {
                        _simulatedCommandInfo.Options[0]
                    };
                }
            }
            var dropdownOptions = new List<TMP_Dropdown.OptionData>();
            foreach (var option in _simulatedCommandInfo.Options)
            {
                dropdownOptions.Add(new() { text = option });
            }
            _dropdown.options = dropdownOptions;
        }

        #endregion

        #region Private Methods

        protected override void Awake()
        {
            base.Awake();
            _dropdown.onValueChanged.AddListener(OnValueChanged);
        }

        protected virtual void OnDestroy()
        {
            base.Awake();
            _dropdown.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(int index)
        {
            _simulatedCommandInfo.Arguments = new string[]
            {
                _simulatedCommandInfo.Options[index]
            };
        }

        #endregion
    }
}