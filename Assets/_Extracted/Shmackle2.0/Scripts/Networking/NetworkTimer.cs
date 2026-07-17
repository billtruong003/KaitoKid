using Fusion;
using System;
using UnityEngine;

namespace Shmackle.Networking
{
    /// <summary>
    /// A component-based wrapper for Fusion's tick timer
    /// </summary>
    public class NetworkTimer : NetworkBehaviour
    {
        #region Public Fields

        public event Action Started;
        public event Action Finished;
        public event Action Paused;
        public event Action Unpaused;
        
        #endregion
        
        #region Private Fields

        private float _pausedRemainingTime;
        
        #endregion
        
        #region Properties
        
        [Networked] 
        private TickTimer TickTimer { get; set; }
        [Networked]
        public float Duration { get; private set; }
        public bool HasStarted { get; private set; }
        public bool IsFinished { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsRunning => TickTimer.IsRunning;
        public float Progress => 1 - (RemainingTime / Duration);
        public float ElapsedTime => Math.Max(0, Duration - RemainingTime);
        public float RemainingTime => TickTimer.RemainingTime(Runner) ?? 0;
        
        #endregion
        
        #region Public Methods

        public void StartTimer(float seconds) {
            Duration = seconds;
            TickTimer = TickTimer.CreateFromSeconds(Runner, Duration);
            _pausedRemainingTime = 0;
        }

        public void PauseTimer() {
            if (TickTimer.IsRunning) {
                _pausedRemainingTime = RemainingTime;
                TickTimer = TickTimer.None;
            }
        }

        public void ResumeTimer() {
            if (!TickTimer.IsRunning && _pausedRemainingTime > 0) {
                TickTimer = TickTimer.CreateFromSeconds(Runner, _pausedRemainingTime);
                _pausedRemainingTime = 0;
            }
        }

        public void StopTimer() {
            TickTimer = TickTimer.None;
            _pausedRemainingTime = 0;
        }

        public override void Render()
        {
            base.Render();
            
            bool isRunning = TickTimer.IsRunning;
            if (!HasStarted && isRunning)
            {
                if (Duration > 0)
                {
                    HasStarted = true;
                    IsFinished = false;
                    Started?.Invoke();
                }
            }
            if (HasStarted)
            {
                if (!IsPaused && !isRunning)
                {
                    IsPaused = true;
                    Paused?.Invoke();
                }
                else if (IsPaused && isRunning)
                {
                    IsPaused = false;
                    Unpaused?.Invoke();
                }
            }

            if (TickTimer.Expired(Runner))
            {
                if (!IsFinished)
                {
                    TickTimer = TickTimer.None;
                    IsFinished = true;
                    HasStarted = false;
                    Finished?.Invoke();
                }
            }
        }

        #endregion
        
    }
}