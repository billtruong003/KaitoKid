using Fusion.XR.Shared.Core;
using MessagePipe;
using Shmackle.Events;
using UnityEngine;

namespace Shmackle.Pad
{
    [RequireComponent(typeof(Collider))]
    public class ShmacklePadSpawnTrigger : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private bool _filterSameRoot = true;

        #endregion
        #region Private Fields

        private IPublisher<ShmacklePadSpawnTriggeredEvent> _spawnTriggeredEventPublisher;
        private ShmacklePadSpawnTriggeredEvent _triggeredEvent;

        #endregion

        #region Private Methods

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            _triggeredEvent = new ShmacklePadSpawnTriggeredEvent();
            _spawnTriggeredEventPublisher = GlobalMessagePipe.GetPublisher<ShmacklePadSpawnTriggeredEvent>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_filterSameRoot || transform.root == other.transform.root)
            {
                IGrabbingProvider grabber = other.GetComponentInParent<IGrabbingProvider>();
                if (grabber != null && !grabber.IsGrabbing)
                {
                    _triggeredEvent.Side = grabber.GrabSide;
                    _triggeredEvent.IsTriggering = true;
                    _spawnTriggeredEventPublisher.Publish(_triggeredEvent);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_filterSameRoot || transform.root == other.transform.root)
            {
                IGrabbingProvider grabber = other.GetComponentInParent<IGrabbingProvider>();
                if (grabber != null)
                {
                    _triggeredEvent.Side = grabber.GrabSide;
                    _triggeredEvent.IsTriggering = false;
                    _spawnTriggeredEventPublisher.Publish(_triggeredEvent);
                }
            }
        }

        #endregion
    }
}