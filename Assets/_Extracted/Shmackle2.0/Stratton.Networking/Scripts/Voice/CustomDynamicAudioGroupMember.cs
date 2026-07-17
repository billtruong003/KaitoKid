using Fusion.Addons.DynamicAudioGroup;
using UnityEngine.Events;
using UnityEngine;
using MessagePipe;
using Fusion;

namespace Stratton.Networking.Voice
{
    public class CustomDynamicAudioGroupMember : DynamicAudioGroupMember
    {
        private DynamicRecorderConfig _dynamicRecorderConfig;
        private IPublisher<PlayerJoinedAudioGroupEvent> _playerJoinedAudioGroupEventPublisher;
        private IPublisher<PlayerLeftAudioGroupEvent> _playerLeftAudioGroupEventPublisher;

        protected override void Awake()
        {
            base.Awake();
            _playerJoinedAudioGroupEventPublisher = GlobalMessagePipe.GetPublisher<PlayerJoinedAudioGroupEvent>();
            _playerLeftAudioGroupEventPublisher = GlobalMessagePipe.GetPublisher<PlayerLeftAudioGroupEvent>();
        }

        public override void Spawned()
        {
            base.Spawned();
            if (recorder)
            {
                _dynamicRecorderConfig = recorder.GetComponent<DynamicRecorderConfig>();
            }
        }

        protected override bool ListenToMember(DynamicAudioGroupMember member)
        {
            bool justListened = base.ListenToMember(member);
            int memberCount = listenedtoMembers.Count;
            if (_dynamicRecorderConfig)
            {
                _dynamicRecorderConfig.ApplyBestConfig(memberCount);
            }
            if (justListened)
            {
                Core.Log.Message(NetworkingLogChannel.Voice, "A player just started listening!");
                _playerJoinedAudioGroupEventPublisher.Publish(new()
                {
                    SourcePlayer = Object.InputAuthority,
                    TargetPlayer = member.Object.InputAuthority
                });
            }
            return justListened;
        }

        protected override bool StopListeningToMember(DynamicAudioGroupMember member)
        {
            bool didStopListening = base.StopListeningToMember(member);
            int memberCount = listenedtoMembers.Count;
            if (_dynamicRecorderConfig)
            {
                _dynamicRecorderConfig.ApplyBestConfig(memberCount);
            }
            if (didStopListening)
            {
                Core.Log.Message(NetworkingLogChannel.Voice, "A player stopped listening");
                _playerLeftAudioGroupEventPublisher.Publish(new()
                {
                    SourcePlayer = Object.InputAuthority,
                    TargetPlayer = member && member.Object ? member.Object.InputAuthority : PlayerRef.None
                });
            }
            return didStopListening;
        }
    }
}
