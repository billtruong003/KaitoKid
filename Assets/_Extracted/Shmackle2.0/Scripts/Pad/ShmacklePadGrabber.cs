using Fusion.XR.Shared.Core;
using MessagePipe;
using System;
using Shmackle.Player.Grab;
using UnityEngine;

namespace Shmackle.Pad
{
    [RequireComponent(typeof(ShmackleGrabber))]
    public class ShmacklePadGrabber : MonoBehaviour
    {
        #region Private Fields

        private IDisposable _padLoadingFinishedSubscription;
        private ShmackleGrabber _grabber;

        #endregion

        #region Private Methods

        private void Awake()
        {
            _grabber = GetComponent<ShmackleGrabber>();
            _padLoadingFinishedSubscription = GlobalMessagePipe.GetSubscriber<ShmacklePadLoadingFinishedEvent>()
                                                    .Subscribe(OnPadLoadingFinishedEvent);
        }

        private void OnDestroy()
        {
            _padLoadingFinishedSubscription?.Dispose();
        }

        private void OnPadLoadingFinishedEvent(ShmacklePadLoadingFinishedEvent finishedEvent)
        {
            if (finishedEvent.Side != _grabber.RigPart.GrabSide)
            {
                return;
            }
            if (!finishedEvent.IsCancelled)
            {
                finishedEvent.Pad.Activate(finishedEvent.Side);
                if (_grabber.RigPart is IOverridableGrabbingProvider grabProvider)
                {
                    // signal force grab to let the hands "grab" without pressing grab button (including the animations)
                    grabProvider.OverrideGrabbing(true);
                }
                ShmackleGrabbable padGrabbable = finishedEvent.Pad.Grabbable;
                if (padGrabbable != null)
                {
                    _grabber.TryGrab(padGrabbable);
                }
            }
        }

        #endregion
    }
}
