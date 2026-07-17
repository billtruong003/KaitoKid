using MessagePipe;
using Shmackle.Events;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Shmackle.Pad
{
    public class UIShmacklePadProgress : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private Image _sliderImage;
        [SerializeField]
        private GameObject _rootGameObject;

        #endregion

        #region Private Fields

        private IDisposable _padLoadingStartedSubscription;
        private IDisposable _padLoadingProgressChangedSubscription;
        private IDisposable _padLoadingFinishedSubscription;

        #endregion

        #region Private Methods

        private void Awake ()
        {
            if (_rootGameObject == null)
            {
                _rootGameObject = gameObject;
            }
            _padLoadingStartedSubscription = GlobalMessagePipe.GetSubscriber<ShmacklePadLoadingStartedEvent>()
                                                    .Subscribe(e => OnPadLoadingStartedEvent());
            _padLoadingProgressChangedSubscription = GlobalMessagePipe.GetSubscriber<ShmacklePadLoadingProgressChangedEvent>()
                                                        .Subscribe(e => OnPadLoadingProgressChangeEvent(e.Progress));
            _padLoadingFinishedSubscription = GlobalMessagePipe.GetSubscriber<ShmacklePadLoadingFinishedEvent>()
                                                    .Subscribe(e => OnPadLoadingFinishedEvent(e.IsCancelled));

            _rootGameObject.SetActive(false);
        }

        private void OnPadLoadingStartedEvent()
        {
            _rootGameObject.SetActive(true);
        }

        private void OnPadLoadingProgressChangeEvent(float progress)
        {
            _sliderImage.fillAmount = progress;
        }

        private void OnPadLoadingFinishedEvent(bool isCancelled)
        {
            _rootGameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _padLoadingStartedSubscription?.Dispose();
            _padLoadingProgressChangedSubscription?.Dispose();
            _padLoadingFinishedSubscription?.Dispose();
        }

        #endregion
    }
}