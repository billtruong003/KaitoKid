using UnityEngine;
using System.Collections.Generic;
using BillGameCore;

namespace BillSamples.TowerDefense
{
    /// <summary>
    /// Tower entity. Auto-targets enemies, fires projectiles, upgradeable.
    /// </summary>
    public class TDTower : MonoBehaviour
    {
        [Header("Tower Info")]
        public TowerType towerType;
        public int level; // 0-based (0=base, 1=lv2, 2=lv3)
        public TargetMode targetMode;
        public Vector2Int gridPosition;

        [Header("Visuals — assign 3D model")]
        public Transform modelRoot;
        public Transform turretPivot; // Rotates toward target
        public Transform firePoint;   // Where projectiles spawn
        public GameObject rangeIndicator;

        // Data
        private TowerDefinition _def;
        private TowerLevelData _levelData;
        private float _attackTimer;
        private int _totalInvested;
        private TDEnemy _currentTarget;

        // Cached enemies list
        private static Collider[] _overlapBuffer = new Collider[64];

        // ─── Init ────────────────────────────────

        public void Setup(TowerType type, Vector2Int pos)
        {
            towerType = type;
            gridPosition = pos;
            level = 0;
            _def = TDDatabase.GetTower(type);
            targetMode = _def.defaultTarget;
            _totalInvested = _def.levels[0].cost;
            RefreshStats();

            // Color the tower
            if (modelRoot)
            {
                foreach (var r in modelRoot.GetComponentsInChildren<Renderer>())
                    r.material.color = _def.color;
            }

            // Place tween
            if (modelRoot)
            {
                modelRoot.localScale = Vector3.zero;
                BillTween.Scale(modelRoot, 1.2f, 0.2f)
                    .SetEase(EaseType.OutBack)
                    .OnComplete(() => BillTween.Scale(modelRoot, 1f, 0.1f));
            }

            Bill.Audio.Play("sfx_tower_place");
        }

        void RefreshStats()
        {
            _levelData = _def.levels[level];
            _attackTimer = 0;

            // Update range indicator
            if (rangeIndicator)
            {
                float diameter = _levelData.range * 2;
                rangeIndicator.transform.localScale = new Vector3(diameter, diameter, 1);
            }
        }

        // ─── Upgrade / Sell ──────────────────────

        public bool CanUpgrade => level < _def.levels.Length - 1;
        public int UpgradeCost => CanUpgrade ? _def.levels[level + 1].cost : 0;
        public int SellPrice => Mathf.RoundToInt(_totalInvested * 0.7f);
        public string DisplayName => _def.displayName;
        public string LevelString => $"Lv{level + 1}";
        public string BonusDesc => _levelData.bonusDesc ?? "";

        public void Upgrade()
        {
            if (!CanUpgrade) return;
            level++;
            _totalInvested += _def.levels[level].cost;
            RefreshStats();

            // Upgrade tween
            if (modelRoot)
            {
                BillTween.Scale(modelRoot, 1.3f, 0.15f)
                    .OnComplete(() => BillTween.Scale(modelRoot, 1f, 0.1f));
            }

            Bill.Audio.Play("sfx_tower_upgrade");
            Bill.Events.Fire(new TowerUpgradedEvent { TowerType = _def.displayName, NewLevel = level + 1 });
        }

        public void Sell()
        {
            Bill.Audio.Play("sfx_tower_sell");
            Bill.Events.Fire(new TowerSoldEvent { TowerType = _def.displayName, Refund = SellPrice });

            // Sell tween
            if (modelRoot)
            {
                BillTween.Scale(modelRoot, 0f, 0.2f)
                    .OnComplete(() => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void CycleTargetMode()
        {
            targetMode = (TargetMode)(((int)targetMode + 1) % 5);
        }

        public void ShowRange(bool show)
        {
            if (rangeIndicator) rangeIndicator.SetActive(show);
        }

        // ─── Combat Loop ─────────────────────────

        void Update()
        {
            if (!Bill.State.IsInState<TDWaveActiveState>() && !Bill.State.IsInState<TDBuildPhaseState>())
                return;

            _attackTimer -= Time.deltaTime;

            // Find target
            _currentTarget = FindTarget();

            // Rotate turret
            if (_currentTarget != null && turretPivot != null)
            {
                Vector3 dir = (_currentTarget.transform.position - turretPivot.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                turretPivot.rotation = Quaternion.Lerp(turretPivot.rotation, Quaternion.Euler(0, 0, angle), 10f * Time.deltaTime);
            }

            // Attack
            if (_currentTarget != null && _attackTimer <= 0)
            {
                _attackTimer = _levelData.attackSpeed;
                Fire(_currentTarget);
            }
        }

        TDEnemy FindTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _levelData.range, _overlapBuffer);
            TDEnemy best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var enemy = _overlapBuffer[i].GetComponent<TDEnemy>();
                if (enemy == null || !enemy.alive) continue;

                float score = targetMode switch
                {
                    TargetMode.First => enemy.distToExit,
                    TargetMode.Last => -enemy.distToExit,
                    TargetMode.Strongest => -enemy.currentHP,
                    TargetMode.Weakest => enemy.currentHP,
                    TargetMode.Nearest => Vector3.Distance(transform.position, enemy.transform.position),
                    _ => enemy.distToExit,
                };

                if (score < bestScore) { bestScore = score; best = enemy; }
            }

            return best;
        }

        void Fire(TDEnemy target)
        {
            string sfxKey = towerType switch
            {
                TowerType.Arrow => "sfx_arrow",
                TowerType.Cannon => "sfx_cannon",
                TowerType.Ice => "sfx_ice",
                TowerType.Lightning => "sfx_lightning",
                TowerType.Sniper => "sfx_sniper",
                TowerType.Poison => "sfx_poison",
                _ => "sfx_arrow"
            };
            Bill.Audio.Play(sfxKey);

            Vector3 spawnPos = firePoint ? firePoint.position : transform.position + Vector3.up * 0.5f;

            switch (towerType)
            {
                case TowerType.Arrow:
                    SpawnProjectile("proj_arrow", spawnPos, target, _levelData.damage);
                    // Lv3: double arrow
                    if (level >= 2)
                    {
                        var second = FindSecondTarget(target);
                        if (second != null) SpawnProjectile("proj_arrow", spawnPos, second, _levelData.damage);
                    }
                    break;

                case TowerType.Cannon:
                    SpawnProjectile("proj_cannonball", spawnPos, target, _levelData.damage, isAOE: true, aoeRadius: _levelData.aoeRadius);
                    break;

                case TowerType.Ice:
                    SpawnProjectile("proj_ice", spawnPos, target, _levelData.damage, applySlow: true,
                        slowPercent: _levelData.slowPercent, slowDur: _levelData.slowDuration);
                    // Lv3: freeze chance
                    if (level >= 2 && Random.value < 0.15f)
                    {
                        // Stun handled as 100% slow for 1s
                        target.ApplySlow(1f, 1f);
                    }
                    break;

                case TowerType.Lightning:
                    FireLightningChain(target, _levelData.damage, _levelData.chainCount);
                    break;

                case TowerType.Sniper:
                    // Instant hit (no projectile)
                    float dmg = _levelData.damage;
                    bool crit = Random.value < (level >= 2 ? 0.35f : level >= 1 ? 0.25f : 0.2f);
                    if (crit) dmg *= 2f;
                    // Lv3: kill shot
                    if (level >= 2 && target.currentHP / target.maxHP < 0.15f)
                        dmg = target.currentHP + 1; // instant kill
                    target.TakeDamage(dmg, ignoreArmor: true, source: TowerType.Sniper);
                    // Visual: line flash
                    DrawLine(spawnPos, target.transform.position, Color.red, 0.1f);
                    break;

                case TowerType.Poison:
                    // AOE poison cloud
                    var enemies = FindEnemiesInRadius(target.transform.position, _levelData.aoeRadius);
                    foreach (var e in enemies)
                    {
                        e.TakeDamage(_levelData.damage, source: TowerType.Poison);
                        e.ApplyDot(_levelData.dotPerSecond, _levelData.dotDuration);
                    }
                    break;
            }
        }

        void SpawnProjectile(string poolKey, Vector3 from, TDEnemy target, float damage,
            bool isAOE = false, float aoeRadius = 0, bool applySlow = false, float slowPercent = 0, float slowDur = 0)
        {
            var projGO = Bill.Pool.Spawn(poolKey, from, Quaternion.identity);
            if (projGO == null) return;

            var proj = projGO.GetComponent<TDProjectile>();
            if (proj == null) proj = projGO.AddComponent<TDProjectile>();

            proj.Init(target, damage, isAOE, aoeRadius, applySlow, slowPercent, slowDur, towerType);
        }

        void FireLightningChain(TDEnemy first, float damage, int chainCount)
        {
            var hit = new List<TDEnemy> { first };
            TDEnemy current = first;
            current.TakeDamage(damage, source: TowerType.Lightning);
            DrawLine(transform.position, current.transform.position, Color.yellow, 0.15f);

            for (int i = 1; i < chainCount; i++)
            {
                var next = FindNearestEnemy(current.transform.position, 2.5f + level * 0.25f, hit);
                if (next == null) break;

                float chainDmg = damage * 0.8f; // 80% per chain
                // Lv3: 10% overcharge
                if (level >= 2 && Random.value < 0.1f) chainDmg *= 3f;

                DrawLine(current.transform.position, next.transform.position, Color.yellow, 0.12f);
                next.TakeDamage(chainDmg, source: TowerType.Lightning);
                hit.Add(next);
                current = next;
            }
        }

        TDEnemy FindSecondTarget(TDEnemy exclude)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _levelData.range, _overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                var e = _overlapBuffer[i].GetComponent<TDEnemy>();
                if (e != null && e.alive && e != exclude) return e;
            }
            return null;
        }

        TDEnemy FindNearestEnemy(Vector3 pos, float radius, List<TDEnemy> exclude)
        {
            int count = Physics.OverlapSphereNonAlloc(pos, radius, _overlapBuffer);
            TDEnemy best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var e = _overlapBuffer[i].GetComponent<TDEnemy>();
                if (e == null || !e.alive || exclude.Contains(e)) continue;
                float d = Vector3.Distance(pos, e.transform.position);
                if (d < bestDist) { bestDist = d; best = e; }
            }
            return best;
        }

        List<TDEnemy> FindEnemiesInRadius(Vector3 pos, float radius)
        {
            var list = new List<TDEnemy>();
            int count = Physics.OverlapSphereNonAlloc(pos, radius, _overlapBuffer);
            for (int i = 0; i < count; i++)
            {
                var e = _overlapBuffer[i].GetComponent<TDEnemy>();
                if (e != null && e.alive) list.Add(e);
            }
            return list;
        }

        void DrawLine(Vector3 from, Vector3 to, Color color, float duration)
        {
            // Simple line renderer from pool
            var lineGO = Bill.Pool.Spawn("vfx_line");
            if (lineGO == null) return;

            var lr = lineGO.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.SetPosition(0, from);
                lr.SetPosition(1, to);
                lr.startColor = color;
                lr.endColor = color;
            }
            Bill.Pool.Return(lineGO, duration);
        }
    }

    /// <summary>
    /// Projectile that flies toward target enemy.
    /// </summary>
    public class TDProjectile : MonoBehaviour
    {
        public float speed = 12f;

        private TDEnemy _target;
        private float _damage;
        private bool _isAOE;
        private float _aoeRadius;
        private bool _applySlow;
        private float _slowPercent, _slowDur;
        private TowerType _source;
        private bool _alive;

        public void Init(TDEnemy target, float damage, bool isAOE, float aoeRadius,
            bool applySlow, float slowPercent, float slowDur, TowerType source)
        {
            _target = target;
            _damage = damage;
            _isAOE = isAOE;
            _aoeRadius = aoeRadius;
            _applySlow = applySlow;
            _slowPercent = slowPercent;
            _slowDur = slowDur;
            _source = source;
            _alive = true;
        }

        void Update()
        {
            if (!_alive) return;

            if (_target == null || !_target.alive)
            {
                Bill.Pool.Return(gameObject);
                _alive = false;
                return;
            }

            Vector3 dir = (_target.transform.position - transform.position).normalized;
            transform.position += dir * speed * Time.deltaTime;

            // Face direction
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Hit check
            if (Vector3.Distance(transform.position, _target.transform.position) < 0.3f)
            {
                OnHit();
            }
        }

        void OnHit()
        {
            _alive = false;

            if (_isAOE)
            {
                // Damage all in radius
                var colliders = Physics.OverlapSphere(transform.position, _aoeRadius);
                foreach (var col in colliders)
                {
                    var enemy = col.GetComponent<TDEnemy>();
                    if (enemy != null && enemy.alive)
                    {
                        enemy.TakeDamage(_damage, source: _source);
                        if (_applySlow) enemy.ApplySlow(_slowPercent, _slowDur);
                    }
                }

                // AoE VFX
                var vfx = Bill.Pool.Spawn("vfx_explosion", transform.position, Quaternion.identity);
                if (vfx) Bill.Pool.Return(vfx, 0.5f);
            }
            else
            {
                _target.TakeDamage(_damage, source: _source);
                if (_applySlow) _target.ApplySlow(_slowPercent, _slowDur);
            }

            Bill.Pool.Return(gameObject);
        }
    }
}
