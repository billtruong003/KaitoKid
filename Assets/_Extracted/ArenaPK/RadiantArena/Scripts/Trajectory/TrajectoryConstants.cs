#nullable enable
using UnityEngine;

namespace RadiantArena.Trajectory
{
    /// <summary>
    /// Central constants for trajectory playback. Adjust here, not in TrajectoryRenderer.
    /// </summary>
    public static class TrajectoryConstants
    {
        // Sim is 0..1000 in both axes; mapped to 10×10 world units centered at origin.
        public const float SimToWorldScale = 0.01f;
        public const float SimCenter = 500f;

        // Visual: placeholder sphere ball.
        public const float BallRadius = 0.18f;
        public static readonly Color BallColor = new Color(0.55f, 0.95f, 1.0f, 1.0f);
        public static readonly Color BallEmission = new Color(0.3f, 0.7f, 0.8f, 1.0f);

        // Trail line (placeholder).
        public const float TrailWidth = 0.08f;
        public const float TrailFadeTime = 0.35f;

        // Lifecycle.
        /// <summary>Empty-trajectory short-circuit delay (sec) — gives the player a beat before ack.</summary>
        public const float EmptyTrajectoryDelay = 0.30f;
        /// <summary>Grace period before destroying the renderer GO after onComplete.</summary>
        public const float DestroyGrace = 0.15f;

        // Event-grammar string prefixes (server's TrajectoryPointSchema.event).
        public const string EvtEmpty        = "";
        public const string EvtWallBounce   = "wall_bounce";
        public const string EvtPiercePlayer = "pierce_player";
        public const string EvtStop         = "stop";
        public const string EvtHitPrefix    = "hit:";
        public const string EvtCritPrefix   = "crit:";

        /// <summary>Server sim coord → Unity world coord. Y-up, sim-Y maps to world-Z.</summary>
        public static Vector3 WorldFromSim(float simX, float simY)
        {
            return new Vector3(
                (simX - SimCenter) * SimToWorldScale,
                0f,
                (simY - SimCenter) * SimToWorldScale);
        }
    }
}
