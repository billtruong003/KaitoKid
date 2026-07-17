#nullable enable
using BillGameCore;
using UnityEngine;

namespace RadiantArena.Juice
{
    /// <summary>
    /// Drops Time.timeScale to FreezeScale for a short window, restores via
    /// unscaled Bill.Timer.Delay. Re-entry extends the deadline (Max);
    /// pending restore callbacks no-op if a newer Trigger pushed the deadline.
    /// </summary>
    public static class HitStop
    {
        const float FreezeScale = 0.05f;
        const float NormalScale = 1.0f;

        static float _restoreAtUnscaled;
        static bool _pending;

        public static void Trigger(int durationMs)
        {
            float now = Time.unscaledTime;
            float dur = Mathf.Max(0.01f, durationMs / 1000f);
            float deadline = now + dur;

            if (deadline <= _restoreAtUnscaled)
            {
                Debug.Log($"[Juice.HitStop] reentry shorter than pending ({dur:F2}s) — skip extend");
                return;
            }
            _restoreAtUnscaled = deadline;

            Time.timeScale = FreezeScale;

            // Schedule a TryRestore for this new deadline. Earlier pending
            // callbacks (if any) will short-circuit because _restoreAtUnscaled
            // has grown past their captured time.
            Bill.Timer.Delay(dur, TryRestore, unscaled: true);
            if (!_pending)
            {
                _pending = true;
                Debug.Log($"[Juice.HitStop] Time.timeScale={FreezeScale}, restore in {dur:F2}s");
            }
            else
            {
                Debug.Log($"[Juice.HitStop] extended freeze, new deadline in {dur:F2}s");
            }
        }

        static void TryRestore()
        {
            if (Time.unscaledTime + 0.0001f < _restoreAtUnscaled)
            {
                // A newer Trigger pushed the deadline; let that callback restore.
                return;
            }
            Time.timeScale = NormalScale;
            _pending = false;
            _restoreAtUnscaled = 0f;
            Debug.Log("[Juice.HitStop] Time.timeScale restored to 1.0");
        }
    }
}
