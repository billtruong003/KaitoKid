using Fusion.XR.Shared.Core;
using MessagePipe;
using Shmackle.Events;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Shmackle.Pad
{
    public class UIShmacklePadSpawnNotification : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private GameObject _rootGameObject;
        [SerializeField]
        private TextMeshProUGUI _label;

        [SerializeField]
        private Color _inputTextColor = Color.red;
        [SerializeField]
        private string _leftInputText = "Y";
        [SerializeField]
        private string _rightInputText = "B";

        #endregion

        #region Private Fields

        private IDisposable _padLoadingStartedSubscription;
        private IDisposable _padLoadingFinishedSubscription;
        private IDisposable _padChestTriggeredSubscription;

        // Ideally, this should be a bitmask
        private HashSet<RigPartSide> _triggeringSides = new HashSet<RigPartSide>();

        private bool _canShow = true;

        #endregion

        #region Private Methods

        private void Awake()
        {
            if (_rootGameObject == null)
            {
                _rootGameObject = gameObject;
            }
            _padLoadingStartedSubscription = GlobalMessagePipe.GetSubscriber<ShmacklePadLoadingStartedEvent>().Subscribe(e => OnPadLoadingStarted());
            _padLoadingFinishedSubscription = GlobalMessagePipe.GetSubscriber<ShmacklePadLoadingFinishedEvent>().Subscribe(e => OnPadLoadingFinished());
            _padChestTriggeredSubscription = GlobalMessagePipe.GetSubscriber<ShmacklePadSpawnTriggeredEvent>().Subscribe(OnSpawnTriggered);
            _rootGameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _padLoadingStartedSubscription?.Dispose();
            _padLoadingFinishedSubscription?.Dispose();
            _padChestTriggeredSubscription?.Dispose();
        }

        private void OnPadLoadingStarted()
        {
            _canShow = false;
            Hide();
        }

        private void OnPadLoadingFinished()
        {
            _canShow = true;
            if (_triggeringSides.Count > 0)
            {
                Show();
            }
        }

        private void OnSpawnTriggered(ShmacklePadSpawnTriggeredEvent triggerredEvent)
        {
            if (triggerredEvent.IsTriggering)
            {
                _triggeringSides.Add(triggerredEvent.Side);
            }
            else
            {
                _triggeringSides.Remove(triggerredEvent.Side);
            }
            if (_triggeringSides.Count > 0)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private string GetInputDisplayText()
        {
            int inputCount = _triggeringSides.Count;
            if (inputCount == 0)
            {
                return string.Empty;
            }
            if (inputCount > 1)
            {
                return $"{_leftInputText} or {_rightInputText}";
            }

            return _triggeringSides.Contains(RigPartSide.Left) ? _leftInputText : _rightInputText;
        }

        private void Show()
        {
            if (!_canShow)
            {
                return;
            }
            string displayString = $"Hold <color=#{ColorUtility.ToHtmlStringRGBA(_inputTextColor)}>{GetInputDisplayText()}</color> to grab phone";
            _label.SetText(displayString);
            _rootGameObject.SetActive(true);
        }

        private void Hide()
        {
            _rootGameObject.SetActive(false);
        }

        #endregion
    }
}
