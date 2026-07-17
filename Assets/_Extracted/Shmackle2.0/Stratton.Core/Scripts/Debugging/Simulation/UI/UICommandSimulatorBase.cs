using UnityEngine;
using UnityEngine.UI;
using Opencoding.CommandHandlerSystem;
using TMPro;

namespace Stratton.Debugging.UI
{
    public abstract class UICommandSimulatorBase : MonoBehaviour, ICommandSimulator
    {
        #region Serialized Fields

        [SerializeField]
        private Button _executeButton;
        [SerializeField]
        private TextMeshProUGUI _displayTextLabel;

        #endregion

        #region Fields

        protected SimulatedCommandInfo _simulatedCommandInfo;

        #endregion

        #region Protected Methods

        protected virtual void Awake()
        {
            if (!_executeButton)
            {
                _executeButton = GetComponentInChildren<Button>();
            }
            _executeButton.onClick.AddListener(Execute);
            if (!_displayTextLabel)
            {
                _displayTextLabel = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        #endregion

        #region ICommandSimulator

        public virtual void Init(SimulatedCommandInfo simulatedCommandInfo)
        {
            _simulatedCommandInfo = simulatedCommandInfo;
            if (_displayTextLabel)
            {
                _displayTextLabel.SetText(_simulatedCommandInfo.DisplayName);
            }
        }

        public virtual string GetCommand()
        {
            return _simulatedCommandInfo.ToString();
        }

        public virtual void Execute()
        {
            CommandHandlers.HandleCommand(GetCommand());
        }

        #endregion
    }
}