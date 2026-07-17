using MessagePipe;
using UnityEngine;

namespace Shmackle.Minimap
{
    public class MinimapDisplay : MonoBehaviour
    {

        #region Private Fields

        private IPublisher<MinimapSetEnabledEvent> _setEnabledPublisher;
        private MinimapSetEnabledEvent _setEnabledEvent;

        #endregion

        #region Private Methods

        private void Awake()
        {
            _setEnabledEvent = new MinimapSetEnabledEvent();
            _setEnabledPublisher = GlobalMessagePipe.GetPublisher<MinimapSetEnabledEvent>();    
        }

        private void OnEnable()
        {
            _setEnabledEvent.IsEnabled = true;
            _setEnabledPublisher.Publish(_setEnabledEvent);
        }

        private void OnDisable() 
        {
            _setEnabledEvent.IsEnabled = false;
            _setEnabledPublisher.Publish(_setEnabledEvent);
        }

        #endregion

    }
}