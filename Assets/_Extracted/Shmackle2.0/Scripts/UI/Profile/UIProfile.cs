using Shmackle.User;
using Stratton.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Shmackle.UI
{
    public class UIProfile : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private TMP_InputField _playerNameInput;
        [SerializeField]
        private GameObject _errorPanel;
        [SerializeField]
        private Button _confirmPlayerNameButton;

        #endregion

        #region Private Fields

        private UserSystem _userSystem;

        #endregion

        #region Private Methods

        private void Awake()
        {
            _userSystem = GameSystemsManager.Instance.Get<UserSystem>();

            _playerNameInput.onValueChanged.AddListener(OnPlayerNameChanged);
            _playerNameInput.onEndEdit.AddListener(OnPlayerNameEndEdit);
            _confirmPlayerNameButton.onClick.AddListener(OnConfirmPlayerName);
        }

        private void OnEnable()
        {
            _errorPanel.SetActive(false);
            _confirmPlayerNameButton.gameObject.SetActive(false);
            _playerNameInput.SetTextWithoutNotify(_userSystem.LocalUserInfo.DisplayName);
        }

        private void OnPlayerNameChanged(string newPlayerName)
        {
            newPlayerName = _playerNameInput.text = newPlayerName.Replace("\n", "").Replace("\r", "");
            _errorPanel.SetActive(false);
            _confirmPlayerNameButton.gameObject.SetActive(newPlayerName.IsNotNullOrEmpty() && newPlayerName != _userSystem.LocalUserInfo.DisplayName);
        }

        private void OnPlayerNameEndEdit(string newPlayerName)
        {
            if (newPlayerName.IsNullOrEmpty())
            {
                // Revert to original player name if they erased on end edit
                _playerNameInput.SetTextWithoutNotify(_userSystem.LocalUserInfo.DisplayName);
                _confirmPlayerNameButton.gameObject.SetActive(false);
            }
        }

        private async void OnConfirmPlayerName()
        {
            _confirmPlayerNameButton.gameObject.SetActive(false);
            bool isSuccessful = await _userSystem.UpdateUserName(_playerNameInput.text);
            if (!isSuccessful)
            {
                _errorPanel.SetActive(true);
            }
        }

        #endregion
    }
}