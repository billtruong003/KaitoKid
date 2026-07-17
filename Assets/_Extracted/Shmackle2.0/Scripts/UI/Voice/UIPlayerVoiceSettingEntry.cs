using Cysharp.Threading.Tasks;
using Fusion;
using Photon.Voice.Unity;
using Shmackle.Gameplay;
using Shmackle.User;
using Stratton.Core;
using Stratton.Networking.Voice;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shmackle.UI
{
    public class UIPlayerVoiceEntry : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private Slider _slider; 
        [SerializeField]
        private TMP_Text _volumeText;
        [SerializeField]
        private Toggle _muteToggle;
        [SerializeField]
        private TMP_Text _nameText;

        #endregion

        #region Private Fields

        private AudioSource _audioSource;
        private AudioSourceOverrideVolume _overrideVolume;
        private PlayerState _playerState;

        #endregion

        #region Public Methods

        public async UniTask Initialize(NetworkRunner runner, ShmacklePlayerState playerState)
        {
            if (_playerState)
            {
                _playerState.UserInfoUpdated -= OnUserInfoUpdated;
            }
            _playerState = playerState;
            do
            {
                if (runner.TryGetPlayerObject(playerState.Owner, out NetworkObject playerObject))
                {
                    // TODO: Cleanup. Cache components here as much as possible.
                    Speaker voiceSpeaker = playerObject.GetComponentInChildren<Speaker>();
                    if (voiceSpeaker != null)
                    {
                        _audioSource = voiceSpeaker.GetComponent<AudioSource>();
                        _overrideVolume = voiceSpeaker.GetComponent<AudioSourceOverrideVolume>();

                        float currentVolume = GetVolume();
                        _slider.SetValueWithoutNotify(currentVolume);
                        SyncVolumeText();
                        _muteToggle.isOn = currentVolume <= 0;
                    }
                    else
                    {
                        Stratton.Core.Log.Error(BaseLogChannel.UI, "Player object does not have a valid voice speaker component");
                    }
                    _playerState.UserInfoUpdated += OnUserInfoUpdated;
                    OnUserInfoUpdated(_playerState.NetworkUserInfo.ToUserInfo());
                    break;
                }
                // Sometimes, the player object is not yet replicated on the network runner.
                await UniTask.DelayFrame(1);
            }
            while (true);
        }

        #endregion

        #region Private Methods

        private void Awake()
        {
            _slider.onValueChanged.AddListener(OnVolumeChanged);
            _muteToggle.onValueChanged.AddListener(OnMuteChanged);
        }

        private float GetVolume()
        {
            return _overrideVolume ? 
                _overrideVolume.Volume : 
                _audioSource ? _audioSource.volume : 0.0f;
        }

        private void SetVolume(float volume)
        {
            SyncVolumeText();
            if (volume <= 0)
            {
                _muteToggle.isOn = true;
                return;
            }
            if (_overrideVolume != null)
            {
                _overrideVolume.Volume = volume;
            }
            else if (_audioSource != null)
            {
                _audioSource.volume = volume;
            }
        }

        private void OnVolumeChanged(float volume)
        {
            SetVolume(volume);
            if (volume > 0)
            {
                _muteToggle.isOn = false;
            }
        }

        private void OnMuteChanged(bool isMuted)
        {
            float finalVolume = 0;
            if (!isMuted)
            {
                finalVolume = 1.0f;
                if (_overrideVolume != null)
                {
                    finalVolume = Mathf.Max(_overrideVolume.Volume, 0.1f);
                }
            }
            // TODO: Add deny list on DynamicAudioGroupMember
            // For now, just adjust the volume to 0
            if (_audioSource != null)
            {
                _audioSource.volume = finalVolume;
            }
            _slider.SetValueWithoutNotify(finalVolume);
            SyncVolumeText();
        }

        private void OnUserInfoUpdated(UserInfo userInfo)
        {
            _nameText.SetText(userInfo.DisplayName);
        }

        private void SyncVolumeText()
        {
            _volumeText.SetText(_slider.value.ToString("F2"));
        }

        #endregion
    }
}
