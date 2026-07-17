using UnityEngine;
using BillGameCore;
using Mythfall.Events;

namespace Mythfall.Gameplay
{
    /* ==================== UNITY HIERARCHY SETUP ====================
     *
     * Scene: GameplayScene
     * Attach to: Main Camera GameObject
     *
     * Designer authors the top-down view by positioning + rotating Main Camera
     * in the scene the way they want it. This script reads that authored
     * camera-to-player offset on first bind, then maintains it via smoothed
     * Lerp in LateUpdate as the player moves.
     *
     * Serialized fields:
     *   - smoothing (float, default 8) — higher = snappier, lower = more glide.
     *
     * No drag-drop refs required: target binding is event-driven via
     * CharacterSpawnedEvent (covers retry flow), with a fallback scan for
     * GameObject tagged "Player" if the camera enables after spawn.
     *
     * ============================================================ */

    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] float smoothing = 8f;

        Transform _target;
        Vector3 _offset;
        bool _subscribed;

        void OnEnable()
        {
            if (Bill.IsReady) Subscribe();
            else Bill.Events?.SubscribeOnce<GameReadyEvent>(_ => Subscribe());
        }

        void OnDisable()
        {
            // Gate on IsReady — Bill.Events getter logs SERVICE NOT FOUND if accessed
            // after teardown (stop Play / scene unload), even when null-checked.
            if (_subscribed && Bill.IsReady)
            {
                Bill.Events.Unsubscribe<CharacterSpawnedEvent>(OnSpawn);
            }
            _subscribed = false;
        }

        void Subscribe()
        {
            if (_subscribed) return;
            Bill.Events.Subscribe<CharacterSpawnedEvent>(OnSpawn);
            _subscribed = true;

            // Late-bind if player already exists (GameplaySpawner may have fired event
            // before this camera enabled — common on scene load timing).
            var existing = GameObject.FindGameObjectWithTag("Player");
            if (existing != null) Bind(existing.transform);
        }

        void OnSpawn(CharacterSpawnedEvent e)
        {
            if (e.transform != null) Bind(e.transform);
        }

        void Bind(Transform t)
        {
            _target = t;
            _offset = transform.position - t.position;
        }

        void LateUpdate()
        {
            if (_target == null) return;
            var desired = _target.position + _offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothing * Time.deltaTime);
        }
    }
}
