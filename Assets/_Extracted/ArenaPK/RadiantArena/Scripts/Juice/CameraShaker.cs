#nullable enable
using BillGameCore;
using UnityEngine;

namespace RadiantArena.Juice
{
    /// <summary>
    /// Hand-rolled camera shake — BillTween envelope drives random offset
    /// on Camera.main.transform.position. Restores origin on tween complete.
    /// Static so subscribers can fire from anywhere without holding a reference.
    /// </summary>
    public static class CameraShaker
    {
        static Vector3 _origin;
        static bool _shaking;
        static Tween? _activeTween;

        /// <summary>
        /// Trigger a punch-shake on the camera. Restricted to the X/Z plane
        /// (top-down ortho gameplay — Y offset is invisible under orthographic
        /// projection and only adds seizure-like jitter). Uses a decaying
        /// sinusoidal oscillation in a fixed random 2D direction per impact,
        /// reading as "punch from a direction" rather than "drunk camera".
        /// </summary>
        public static void Shake(float intensity, float duration)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Juice.Shake] Camera.main null — skip");
                return;
            }

            // First shake captures origin; subsequent shakes within the window
            // re-use the same origin to avoid drift on rapid hits.
            if (!_shaking) _origin = cam.transform.position;
            _shaking = true;

            // Kill any in-flight shake so we don't double-oscillate.
            if (_activeTween != null) BillTween.Kill(_activeTween);

            // Fixed random direction per impact = punch feel.
            var dir2D = Random.insideUnitCircle.normalized;
            if (dir2D.sqrMagnitude < 0.001f) dir2D = Vector2.right;

            var t = cam.transform;
            const float OscFreqHz = 22f; // ~5-6 cycles over a 0.25s hit shake
            _activeTween = BillTween.Float(1f, 0f, duration, env =>
            {
                if (cam == null || t == null) return;
                // env decays 1→0; phase grows as env shrinks → punch-in then settle.
                float phase = (1f - env) * (OscFreqHz * Mathf.PI * 2f) * duration;
                float wave  = Mathf.Sin(phase) * intensity * env;
                t.position = _origin + new Vector3(dir2D.x * wave, 0f, dir2D.y * wave);
            });
            _activeTween?.OnComplete(() =>
            {
                _activeTween = null;
                _shaking = false;
                if (cam != null && t != null) t.position = _origin;
            });
        }
    }
}
