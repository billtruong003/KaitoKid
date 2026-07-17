#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.UI;
using UnityEngine;

namespace RadiantArena.Juice
{
    /// <summary>
    /// MonoBehaviour spawned by ArenaBootstrap. Listens to PlayerHitEvent +
    /// WallBounceEvent and dispatches to CameraShaker / HitStop / DamageNumberLayer.
    /// Lives across scene loads (DontDestroyOnLoad). Singleton-guarded.
    /// Tunables hard-coded here for D.U7a — externalize to Bill.Config in D.U7b
    /// if Bill wants live tuning.
    /// </summary>
    public class JuicePresenter : MonoBehaviour
    {
        public static JuicePresenter? Instance { get; private set; }

        // Tunables (RADIANT_ARENA_UNITY.md §9 reference values).
        const float ShakeHit      = 0.30f;
        const float ShakeCrit     = 0.60f;
        const float ShakeWall     = 0.15f;
        const float ShakeDurHit   = 0.25f;
        const float ShakeDurWall  = 0.15f;
        const int   HitStopHitMs  = 60;
        const int   HitStopCritMs = 120;

        Action<PlayerHitEvent>? _onHit;
        Action<WallBounceEvent>? _onWall;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _onHit  = OnPlayerHit;
            _onWall = OnWallBounce;
            Bill.Events.Subscribe(_onHit);
            Bill.Events.Subscribe(_onWall);

            Debug.Log("[Juice] JuicePresenter ready");
        }

        void OnDestroy()
        {
            // Bill services may be torn down before our OnDestroy fires (Play
            // exit). Guard with IsReady to avoid SERVICE NOT FOUND noise.
            if (Bill.IsReady)
            {
                if (_onHit  != null) Bill.Events.Unsubscribe(_onHit);
                if (_onWall != null) Bill.Events.Unsubscribe(_onWall);
            }
            _onHit = null;
            _onWall = null;
            if (Instance == this) Instance = null;
        }

        void OnPlayerHit(PlayerHitEvent e)
        {
            CameraShaker.Shake(e.isCrit ? ShakeCrit : ShakeHit, ShakeDurHit);
            HitStop.Trigger(e.isCrit ? HitStopCritMs : HitStopHitMs);

            if (Bill.UI.IsOpen<DamageNumberLayer>())
            {
                // Grab the live panel instance via re-Open (idempotent — GetOrCreate + Show).
                var layer = Bill.UI.Open<DamageNumberLayer>();
                layer?.Spawn(e.point, e.damage, e.isCrit);
            }
            else
            {
                Debug.LogWarning("[Juice] PlayerHitEvent fired but DamageNumberLayer not open — skipping number");
            }
        }

        void OnWallBounce(WallBounceEvent e)
        {
            CameraShaker.Shake(ShakeWall, ShakeDurWall);
            // No hit-stop, no damage number — wall bounce isn't a damage event.
        }
    }
}
