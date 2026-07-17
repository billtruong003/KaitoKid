using Fusion;
using MessagePipe;

namespace Stratton.Networking.Voice
{
    public class DynamicAudioGroupFader : NetworkBehaviour
    {
        private AudioFader _audioFader;

        private ISubscriber<PlayerJoinedAudioGroupEvent> _playerJoinedAudioGroupEventSubscriber;
        private ISubscriber<PlayerLeftAudioGroupEvent> _playerLeftAudioGroupEventSubscriber;

        private void Awake()
        {
            _audioFader = GetComponentInChildren<AudioFader>();
            if (_audioFader == null)
            {
                return;
            }

            _playerJoinedAudioGroupEventSubscriber = GlobalMessagePipe.GetSubscriber<PlayerJoinedAudioGroupEvent>();
            _playerLeftAudioGroupEventSubscriber = GlobalMessagePipe.GetSubscriber<PlayerLeftAudioGroupEvent>();

            _playerJoinedAudioGroupEventSubscriber.Subscribe(OnJoinedAudioGroup);
            _playerLeftAudioGroupEventSubscriber.Subscribe(OnLeftAudioGroup);
        }

        private void OnJoinedAudioGroup(PlayerJoinedAudioGroupEvent joinedAudioGroupEvent)
        {
            if (joinedAudioGroupEvent == null || Object == null)
            {
                return;
            }
            if (joinedAudioGroupEvent.TargetPlayer == Object.InputAuthority)
            {
                _audioFader.FadeIn();
            }
        }

        private void OnLeftAudioGroup(PlayerLeftAudioGroupEvent leftAudioGroupEvent)
        {
            if (leftAudioGroupEvent == null || Object == null)
            {
                return;
            }
            if (leftAudioGroupEvent.TargetPlayer == Object.InputAuthority)
            {
                _audioFader.FadeOut();
            }
        }
    }
}