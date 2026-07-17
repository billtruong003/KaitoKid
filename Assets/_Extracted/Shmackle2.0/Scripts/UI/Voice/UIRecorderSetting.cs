using Fusion;
using Photon.Voice.Unity;
using Stratton.Core;
using Stratton.Networking;

namespace Shmackle.UI
{
    public class UIRecorderSetting : VoiceComponent
    {
        #region Protected Fields

        protected NetworkRunner _networkRunner;
        protected Recorder _recorder;

        #endregion

        #region Protected Methods

        protected override void Awake()
        {
            base.Awake();
            NetworkingSystem networkingSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();
            if (networkingSystem == null)
            {
                Stratton.Core.Log.Error(BaseLogChannel.UI, "Networking system not yet initialized");
                return;
            }
            _networkRunner = networkingSystem.Runner.GetComponent<NetworkRunner>();
            if (_networkRunner == null)
            {
                Stratton.Core.Log.Error(BaseLogChannel.UI, "Network Runner not yet initialized");
                return;
            }
            _recorder = _networkRunner.GetComponentInChildren<Recorder>();
        }

        #endregion
    }
}