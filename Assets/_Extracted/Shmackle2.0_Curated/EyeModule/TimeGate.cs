using System.Runtime.CompilerServices;
using UnityEngine;

namespace Utilities.Timers
{
    /// <summary>
    /// A lightweight utility struct for throttling high-frequency loops (Rate Limiting).
    /// Used to execute logic at specific intervals without blocking the main thread.
    /// Supports both Scaled (Time.time) and Unscaled (Time.unscaledTime) contexts.
    /// </summary>
    public sealed class TimeGate
    {
        public bool IsReady => GetTime() >= _unlockTime;
        public float Remaining => Mathf.Max(0f, _unlockTime - GetTime());
        private readonly bool _unscaled;
        private float _interval;
        private float _unlockTime;

        public TimeGate(float interval, bool unscaled = false)
        {
            _interval = interval;
            _unscaled = unscaled;
            _unlockTime = float.MinValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Throttle()
        {
            float time = GetTime();
            if (time < _unlockTime) return false;

            _unlockTime = time + _interval;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Throttle(float dynamicInterval)
        {
            float time = GetTime();
            if (time < _unlockTime) return false;

            _interval = dynamicInterval;
            _unlockTime = time + _interval;
            return true;
        }

        public void Reset() => _unlockTime = float.MinValue;

        public void Lock(float duration) => _unlockTime = GetTime() + duration;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetTime() => _unscaled ? Time.unscaledTime : Time.time;

        public static TimeGate Scaled(float interval) => new TimeGate(interval, false);
        public static TimeGate Unscaled(float interval) => new TimeGate(interval, true);
    }
}