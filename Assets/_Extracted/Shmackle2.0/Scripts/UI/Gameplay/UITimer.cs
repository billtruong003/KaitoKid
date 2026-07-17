using Shmackle.Networking;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Shmackle.UI
{
    public class UITimer : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField, Tooltip("Text to display when the timer reaches its target")]
        private string _finishedString = "Finished!";
        [SerializeField, Tooltip("If false, the timer will show the elapsed time.")] 
        private bool _showRemainingTime = true;
        [SerializeField]
        private NetworkTimer _timer;
        [SerializeField]
        private TMP_Text _timerText;

        [Header("Events")] 
        [SerializeField, Tooltip("Trigger callbacks (OnFinished, OnStarted) on already running timers when the UI subscribed?")]
        private bool _triggerCallbacksIfRunninng = true;
        [SerializeField]
        private UnityEvent _timerStart;
        [SerializeField]
        private UnityEvent _timerFinished;
        
        #endregion
        
        #region Private Methods

        private void Awake()
        {
            if (_timerText == null)
            {
                _timerText = GetComponentInChildren<TMP_Text>();
            }
            SetTimer(_timer);
        }

        private void Update()
        {
            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            if (_timer && _timer.HasStarted && !_timer.IsFinished)
            {
                float time = _showRemainingTime ? _timer.RemainingTime : _timer.ElapsedTime;
                int hours = Mathf.FloorToInt(time / 3600);
                int minutes = Mathf.FloorToInt((time % 3600) / 60);
                int seconds = Mathf.FloorToInt(time % 60);

                if (hours > 0)
                {
                    _timerText.text = $"{hours:00}:{minutes:00}:{seconds:00}";
                }
                else
                {
                    _timerText.text = $"{minutes:00}:{seconds:00}";
                }
            }
        }
        
        private void OnTimerStarted()
        {
            _timerStart?.Invoke();
        }

        private void OnTimerFinished()
        {
            _timerText.text = _finishedString;
            _timerFinished?.Invoke();
        }
        
        #endregion
        
        #region Public Methods

        public void SetTimer(NetworkTimer timer)
        {
            if (timer == null)
            {
                return;
            }
            if (_timer != null)
            {
                _timer.Finished -= OnTimerFinished;
            }
            _timer = timer;
            _timer.Started += OnTimerStarted;
            _timer.Finished += OnTimerFinished;
            if (_triggerCallbacksIfRunninng)
            {
                if (_timer.HasStarted)
                {
                    OnTimerStarted();
                }
                else if (_timer.IsFinished)
                {
                    OnTimerFinished();
                }
            }
        }
        
        #endregion
    }
}