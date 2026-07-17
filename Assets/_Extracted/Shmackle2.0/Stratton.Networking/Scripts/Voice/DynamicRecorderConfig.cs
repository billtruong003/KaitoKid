using Photon.Voice.Unity;
using UnityEngine;

namespace Stratton.Networking.Voice
{
    /// <summary>
    /// Container and setter of dynamic default configs
    /// </summary>
    [RequireComponent(typeof(Recorder))]
    public class DynamicRecorderConfig : MonoBehaviour
    {
        private Recorder _recorder;
        private int _currentConfigIndex = -1;

        [SerializeField]
        private bool _applyDefault = true;
        [SerializeField]
        private RecorderConfig[] _levelOfDetail;

        private void Awake()
        {
            _recorder = GetComponent<Recorder>();
        }

        private void Start()
        {
            if (_applyDefault)
            {
                ApplyBestConfig(0);
            }
        }

        public void ApplyBestConfig(int playerCount)
        {
            if (_levelOfDetail.Length == 0)
            {
                Core.Log.Warning(NetworkingLogChannel.Voice, "No dynamic recorder config assigned");
                return;
            }
            int bestConfigIndex = 0;
            for (int i = 0; i < _levelOfDetail.Length; i++)
            {
                if (_levelOfDetail[i].MinPlayerCount == playerCount)
                {
                    bestConfigIndex = i;
                    break;
                }
                else if (playerCount > _levelOfDetail[i].MinPlayerCount)
                {
                    bestConfigIndex = i; // order is not guaranteed so don't break
                }
            }
            if (_currentConfigIndex != bestConfigIndex)
            {
                _currentConfigIndex = bestConfigIndex;
                UpdateRecorderConfig(_levelOfDetail[bestConfigIndex]);
            }
        }

        public void UpdateRecorderConfig(RecorderConfig config)
        {
            _recorder.Bitrate = config.Bitrate;
            _recorder.SamplingRate = config.SamplingRate;
            _recorder.FrameDuration = config.FrameDuration;
            Core.Log.Message(NetworkingLogChannel.Voice, "Changed recorder config");
        }
    }
}
