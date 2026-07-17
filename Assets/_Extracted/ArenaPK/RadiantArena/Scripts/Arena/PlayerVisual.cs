#nullable enable
using RadiantArena.Net;
using RadiantArena.Trajectory;
using UnityEngine;

namespace RadiantArena.Arena
{
    /// <summary>
    /// Attached to a capsule by ArenaSceneBuilder. Each 100ms reads
    /// ArenaContext.MyPlayer/OpponentPlayer.X/Y and updates transform.
    /// Falls back to SlotAnchor when server hasn't set position (X≈Y≈0 heuristic).
    /// Capsule center Y is preserved across all updates (captured in Awake).
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        public bool IsMine;
        public Vector3 SlotAnchor;

        const float PollIntervalMs = 100f;
        const float WeaponOffsetY = 1.2f;
        float _accumMs;
        float _capsuleY;
        string _currentWeaponSlug = "";
        GameObject? _weaponGo;

        void Awake()
        {
            _capsuleY = transform.position.y;
        }

        void Update()
        {
            _accumMs += Time.unscaledDeltaTime * 1000f;
            if (_accumMs < PollIntervalMs) return;
            _accumMs = 0f;
            SyncFromContext();
        }

        void SyncFromContext()
        {
            var p = IsMine ? ArenaContext.MyPlayer : ArenaContext.OpponentPlayer;
            if (p == null)
            {
                transform.position = SlotAnchor;
                if (_weaponGo != null)
                {
                    UnityEngine.Object.Destroy(_weaponGo);
                    _weaponGo = null;
                    _currentWeaponSlug = "";
                }
                return;
            }
            // Fallback: server defaults x=y=0 (uninitialized) → use SlotAnchor.
            if (Mathf.Approximately(p.X, 0f) && Mathf.Approximately(p.Y, 0f))
            {
                transform.position = SlotAnchor;
            }
            else
            {
                // Map sim (x, y) → world (X, 0, Z), then re-apply capsule center Y.
                var world = TrajectoryConstants.WorldFromSim(p.X, p.Y);
                transform.position = new Vector3(world.x, _capsuleY, world.z);
            }

            // Weapon attachment — track LockedWeapon.Slug changes; spawn/destroy as needed.
            var locked = p.LockedWeapon;
            string newSlug = locked != null ? (locked.Slug ?? "") : "";
            if (newSlug != _currentWeaponSlug)
            {
                if (_weaponGo != null)
                {
                    UnityEngine.Object.Destroy(_weaponGo);
                    _weaponGo = null;
                }
                _currentWeaponSlug = newSlug;
                if (!string.IsNullOrEmpty(newSlug))
                {
                    _weaponGo = Weapons.WeaponPrefabRegistry.Spawn(newSlug, transform);
                    _weaponGo.transform.localPosition = new Vector3(0f, WeaponOffsetY, 0f);
                    if (locked != null) Weapons.WeaponHueApplier.Apply(_weaponGo, locked.Hue ?? "");
                    Debug.Log("[Arena.PlayerVisual] " + (IsMine ? "me" : "opp") + " spawned weapon=" + newSlug + " hue=" + (locked != null ? locked.Hue : ""));
                }
            }
        }
    }
}
