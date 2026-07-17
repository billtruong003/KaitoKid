#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using UnityEngine;

namespace RadiantArena.Trajectory
{
    /// <summary>
    /// Runtime-spawned playback of a server-resolved shot. Lifecycle is single-shot:
    /// Spawn → Play(points) → Update interp → events fire at markers → onComplete → self-destruct.
    ///
    /// D.U5 ships placeholder visuals (sphere + LineRenderer trail). D.U7+ swaps to
    /// real FX prefabs via Bill.Pool. D.U8 tints by weapon hue.
    /// </summary>
    public class TrajectoryRenderer : MonoBehaviour
    {
        TrajectoryPoint[] _points = Array.Empty<TrajectoryPoint>();
        string _shooterId = "";
        int _damage;
        bool _crit;
        Action? _onComplete;

        float _startTime;
        int _idx;       // index of the NEXT point to reach (event has not yet fired)
        bool _playing;
        bool _settling; // playback done; awaiting destroy grace
        int _totalDmgFired;

        GameObject? _ball;
        LineRenderer? _trail;

        /// <summary>Factory + entry-point in one. Caller does not need to AddComponent themselves.</summary>
        public static TrajectoryRenderer Spawn()
        {
            var go = new GameObject("[TrajectoryRenderer]");
            return go.AddComponent<TrajectoryRenderer>();
        }

        void Awake()
        {
            BuildBall();
            BuildTrail();
        }

        public void Play(TrajectoryPoint[] points, string shooterId, int damage, bool crit, Action? onComplete)
        {
            _points = points ?? Array.Empty<TrajectoryPoint>();
            _shooterId = shooterId ?? "";
            _damage = damage;
            _crit = crit;
            _onComplete = onComplete;
            _idx = 0;
            _totalDmgFired = 0;

            if (_points.Length == 0)
            {
                Debug.Log("[Arena.Trajectory] empty trajectory — server physics not wired (D.U5b); auto-completing in 0.3s.");
                _playing = false;
                Bill.Timer.Delay(TrajectoryConstants.EmptyTrajectoryDelay, FinishAndDestroy);
                return;
            }

            Debug.Log($"[Arena.Trajectory] spawned renderer — {_points.Length} points, shooter={_shooterId}, dmg={_damage}, crit={_crit}");
            var first = _points[0];
            transform.position = TrajectoryConstants.WorldFromSim(first.x, first.y);
            if (_ball != null) _ball.transform.position = transform.position;
            _startTime = Time.time;
            _playing = true;
        }

        void Update()
        {
            if (!_playing || _settling) return;
            if (_points.Length == 0) return;

            float elapsedMs = (Time.time - _startTime) * 1000f;

            // Advance event index for every point whose t has been passed.
            while (_idx < _points.Length)
            {
                var pt = _points[_idx];
                if (elapsedMs < pt.t)
                {
                    // Interp ball position between previous point and this one.
                    var prev = _idx == 0 ? pt : _points[_idx - 1];
                    float span = Mathf.Max(1f, pt.t - prev.t);
                    float lerpT = Mathf.Clamp01((elapsedMs - prev.t) / span);
                    var prevWorld = TrajectoryConstants.WorldFromSim(prev.x, prev.y);
                    var currWorld = TrajectoryConstants.WorldFromSim(pt.x, pt.y);
                    var pos = Vector3.Lerp(prevWorld, currWorld, lerpT);
                    if (_ball != null) _ball.transform.position = pos;
                    return;
                }

                // Reached this point — snap + fire its event.
                var worldPos = TrajectoryConstants.WorldFromSim(pt.x, pt.y);
                if (_ball != null) _ball.transform.position = worldPos;
                HandleEvent(pt, worldPos);
                _idx++;

                if (_settling) return; // HandleEvent flipped settling (stop event)
            }

            // Past last point with no stop event — wrap up.
            FinishAndDestroy();
        }

        void HandleEvent(TrajectoryPoint pt, Vector3 worldPos)
        {
            var evt = pt.evt ?? string.Empty;
            if (evt == TrajectoryConstants.EvtEmpty) return;

            if (evt == TrajectoryConstants.EvtWallBounce)
            {
                Debug.Log($"[Arena.Trajectory] event=wall_bounce at ({worldPos.x:F2}, 0, {worldPos.z:F2})");
                Bill.Events.Fire(new WallBounceEvent { point = worldPos });
                return;
            }

            if (evt.StartsWith(TrajectoryConstants.EvtHitPrefix) ||
                evt.StartsWith(TrajectoryConstants.EvtCritPrefix))
            {
                bool isCrit = evt.StartsWith(TrajectoryConstants.EvtCritPrefix);
                int colon = evt.IndexOf(':');
                int dmg = 0;
                if (colon >= 0 && colon < evt.Length - 1)
                {
                    if (!int.TryParse(evt.Substring(colon + 1), out dmg))
                    {
                        Debug.LogWarning($"[Arena.Trajectory] could not parse dmg from '{evt}'");
                        dmg = 0;
                    }
                }
                _totalDmgFired += dmg;
                string victimId = _shooterId == ArenaContext.MyDiscordId
                    ? ArenaContext.OpponentDiscordId
                    : ArenaContext.MyDiscordId;
                Debug.Log($"[Arena.Trajectory] event={evt} dmg={dmg} isCrit={isCrit} victim={victimId} at ({worldPos.x:F2}, 0, {worldPos.z:F2})");
                Bill.Events.Fire(new PlayerHitEvent
                {
                    damage = dmg, isCrit = isCrit, victimId = victimId, point = worldPos
                });
                return;
            }

            if (evt == TrajectoryConstants.EvtPiercePlayer)
            {
                Debug.Log($"[Arena.Trajectory] event=pierce_player at ({worldPos.x:F2}, 0, {worldPos.z:F2}) — slow-mo deferred to D.U7");
                return;
            }

            if (evt == TrajectoryConstants.EvtStop)
            {
                Debug.Log("[Arena.Trajectory] event=stop — playback complete");
                FinishAndDestroy();
                return;
            }

            Debug.LogWarning($"[Arena.Trajectory] unknown event '{evt}' — ignored");
        }

        void FinishAndDestroy()
        {
            if (_settling) return;
            _settling = true;
            _playing = false;
            Bill.Events.Fire(new TrajectoryFinishedEvent { shooterId = _shooterId, totalDamage = _totalDmgFired });
            try { _onComplete?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
            Destroy(gameObject, TrajectoryConstants.DestroyGrace);
        }

        void BuildBall()
        {
            _ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _ball.name = "ball";
            _ball.transform.SetParent(transform, worldPositionStays: false);
            _ball.transform.localScale = Vector3.one * (TrajectoryConstants.BallRadius * 2f);
            // Drop the collider — purely visual, server is authoritative.
            var col = _ball.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = TrajectoryConstants.BallColor;
                var r = _ball.GetComponent<MeshRenderer>();
                if (r != null) r.sharedMaterial = mat;
            }
        }

        void BuildTrail()
        {
            _trail = gameObject.AddComponent<LineRenderer>();
            _trail.positionCount = 0;
            _trail.startWidth = TrajectoryConstants.TrailWidth;
            _trail.endWidth = TrajectoryConstants.TrailWidth * 0.3f;
            _trail.useWorldSpace = true;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = TrajectoryConstants.BallColor;
                _trail.material = mat;
            }
        }

        void LateUpdate()
        {
            // Lazy trail update — append current ball position to LineRenderer.
            // D.U7 will replace with proper TrailRenderer + fade-out.
            if (!_playing || _ball == null || _trail == null) return;
            int n = _trail.positionCount;
            _trail.positionCount = n + 1;
            _trail.SetPosition(n, _ball.transform.position);
        }
    }
}
