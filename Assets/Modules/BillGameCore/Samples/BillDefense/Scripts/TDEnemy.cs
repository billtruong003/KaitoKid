using UnityEngine;
using System.Collections.Generic;
using BillGameCore;

namespace BillSamples.TowerDefense
{
    /// <summary>
    /// Enemy entity. Follows waypoints, takes damage, has special abilities.
    /// </summary>
    public class TDEnemy : MonoBehaviour
    {
        [Header("Set by spawner")]
        public EnemyDefinition def;
        public float currentHP;
        public float maxHP;
        public int currentArmor;
        public int shieldHitsRemaining;
        public int goldReward;

        [Header("State")]
        public bool alive;
        public bool hasResurrected;
        public float distToExit; // For targeting priority

        [Header("Visuals — assign 3D model")]
        public Transform modelRoot;
        public Transform hpBarFill;
        public Renderer[] renderers;

        // Pathfinding
        private Vector3[] _waypoints;
        private int _waypointIndex;
        private float _currentSpeed;
        private float _slowTimer;
        private float _slowMultiplier = 1f;

        // DoT
        private struct DotEffect { public float dps; public float remaining; }
        private List<DotEffect> _dots = new List<DotEffect>(5);

        // Heal aura
        private float _healTimer;

        // ─── Init ────────────────────────────────

        public void Setup(EnemyDefinition definition, Vector3[] waypoints, int wave)
        {
            def = definition;

            // Scale with wave
            float hpScale = 1f + wave * 0.08f;
            int armorBonus = wave / 10;

            maxHP = def.baseHP * hpScale;
            currentHP = maxHP;
            currentArmor = def.armor + armorBonus;
            goldReward = def.goldReward + (wave / 5);
            shieldHitsRemaining = def.shieldHits;
            _currentSpeed = def.speed;
            alive = true;
            hasResurrected = false;
            _slowTimer = 0;
            _slowMultiplier = 1f;
            _dots.Clear();

            _waypoints = waypoints;
            _waypointIndex = 0;

            transform.position = waypoints[0];
            UpdateHPBar();
        }

        // ─── Update ──────────────────────────────

        void Update()
        {
            if (!alive) return;

            // Movement
            MoveAlongPath(Time.deltaTime);

            // Slow decay
            if (_slowTimer > 0)
            {
                _slowTimer -= Time.deltaTime;
                if (_slowTimer <= 0) _slowMultiplier = 1f;
            }

            // DoT tick
            ProcessDots(Time.deltaTime);

            // Heal aura
            if (def.healAuraRadius > 0) ProcessHealAura(Time.deltaTime);

            // Distance to exit (for targeting)
            float d = 0;
            for (int i = _waypointIndex; i < _waypoints.Length; i++)
            {
                Vector3 from = (i == _waypointIndex) ? transform.position : _waypoints[i - 1];
                d += Vector3.Distance(from, _waypoints[i]);
            }
            distToExit = d;
        }

        void MoveAlongPath(float dt)
        {
            if (_waypoints == null || _waypointIndex >= _waypoints.Length) return;

            Vector3 target = _waypoints[_waypointIndex];
            float speed = _currentSpeed * _slowMultiplier;

            Vector3 dir = (target - transform.position).normalized;
            float dist = speed * dt;
            float remaining = Vector3.Distance(transform.position, target);

            if (dist >= remaining)
            {
                transform.position = target;
                _waypointIndex++;

                if (_waypointIndex >= _waypoints.Length)
                {
                    // Reached exit
                    ReachExit();
                    return;
                }
            }
            else
            {
                transform.position += dir * dist;
            }

            // Face movement direction
            if (modelRoot && dir.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                modelRoot.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        // ─── Damage ──────────────────────────────

        /// <summary>
        /// Apply damage. Returns actual damage dealt.
        /// </summary>
        public float TakeDamage(float rawDamage, bool ignoreArmor = false, TowerType source = TowerType.Arrow)
        {
            if (!alive) return 0;

            // Ghost dodge
            if (def.dodgeChance > 0 && Random.value < def.dodgeChance)
            {
                // Show "DODGE" text
                SpawnFloatingText("DODGE", Color.gray);
                return 0;
            }

            // Shield blocks
            if (shieldHitsRemaining > 0)
            {
                shieldHitsRemaining--;
                SpawnFloatingText("BLOCKED", Color.cyan);
                if (shieldHitsRemaining <= 0)
                {
                    // Shield broken, remove armor
                    currentArmor = 0;
                }
                return 0;
            }

            // Armor reduction
            float actualDamage;
            if (ignoreArmor || source == TowerType.Sniper)
                actualDamage = rawDamage;
            else
                actualDamage = Mathf.Max(1f, rawDamage - currentArmor);

            currentHP -= actualDamage;
            UpdateHPBar();

            // Hurt flash
            FlashWhite(0.1f);

            // Floating damage number
            SpawnFloatingText($"-{Mathf.RoundToInt(actualDamage)}", Color.white);

            if (currentHP <= 0)
            {
                if (def.canResurrect && !hasResurrected)
                {
                    Resurrect();
                }
                else
                {
                    Die();
                }
            }

            return actualDamage;
        }

        public void ApplySlow(float percent, float duration)
        {
            if (def.immuneToSlow) return;
            _slowMultiplier = 1f - percent;
            _slowTimer = duration;
            // Blue tint
            TintColor(new Color(0.5f, 0.7f, 1f, 0.5f));
        }

        public void ApplyDot(float dps, float duration)
        {
            if (def.immuneToPoison) return;

            // Check max stacks (simplified: just add)
            _dots.Add(new DotEffect { dps = dps, remaining = duration });
            // Green tint
            TintColor(new Color(0.3f, 0.8f, 0.2f, 0.3f));
        }

        void ProcessDots(float dt)
        {
            float totalDps = 0;
            for (int i = _dots.Count - 1; i >= 0; i--)
            {
                var dot = _dots[i];
                totalDps += dot.dps;
                dot.remaining -= dt;
                if (dot.remaining <= 0)
                    _dots.RemoveAt(i);
                else
                    _dots[i] = dot;
            }

            if (totalDps > 0)
            {
                currentHP -= totalDps * dt;
                UpdateHPBar();
                if (currentHP <= 0 && alive) Die();
            }
        }

        void ProcessHealAura(float dt)
        {
            _healTimer -= dt;
            if (_healTimer > 0) return;
            _healTimer = 0.5f; // Heal tick every 0.5s

            var allies = Physics.OverlapSphere(transform.position, def.healAuraRadius);
            foreach (var col in allies)
            {
                var enemy = col.GetComponent<TDEnemy>();
                if (enemy != null && enemy != this && enemy.alive && enemy.currentHP < enemy.maxHP)
                {
                    enemy.currentHP = Mathf.Min(enemy.maxHP, enemy.currentHP + def.healPerSecond * 0.5f);
                    enemy.UpdateHPBar();
                }
            }
        }

        // ─── Death / Resurrect ───────────────────

        void Die()
        {
            alive = false;
            Bill.Audio.Play("sfx_enemy_die");
            Bill.Events.Fire(new EnemyKilledEvent { Type = def.displayName, GoldReward = goldReward, Position = transform.position });

            // Gold popup
            SpawnFloatingText($"+{goldReward}g", Color.yellow);

            // Death tween
            if (modelRoot)
            {
                BillTween.Scale(modelRoot, 0.5f, 0.3f);
                BillTween.Fade(GetComponentInChildren<SpriteRenderer>(), 0f, 0.3f);
            }

            Bill.Timer.Delay(0.4f, () => Bill.Pool.Return(gameObject));
        }

        void Resurrect()
        {
            hasResurrected = true;
            currentHP = maxHP * 0.5f;
            UpdateHPBar();

            // Resurrect glow
            if (modelRoot)
            {
                BillTween.Scale(modelRoot, 1.3f, 0.2f)
                    .SetEase(EaseType.OutBack)
                    .OnComplete(() => BillTween.Scale(modelRoot, 1f, 0.15f));
            }
            SpawnFloatingText("REVIVE!", new Color(0.5f, 1f, 0.5f));
        }

        void ReachExit()
        {
            alive = false;
            Bill.Events.Fire(new EnemyLeakedEvent { Type = def.displayName, LivesLeft = -1 }); // Manager handles lives
            Bill.Pool.Return(gameObject);
        }

        // ─── Visual Helpers ──────────────────────

        void UpdateHPBar()
        {
            if (hpBarFill != null)
            {
                float ratio = Mathf.Clamp01(currentHP / maxHP);
                hpBarFill.localScale = new Vector3(ratio, 1, 1);
            }
        }

        void FlashWhite(float duration)
        {
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var origColor = r.material.color;
                r.material.color = Color.white;
                Bill.Timer.Delay(duration, () => { if (r != null) r.material.color = origColor; });
            }
        }

        void TintColor(Color tint)
        {
            // Simplified: just tint the material temporarily
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>();
            // This is a visual hint; a proper implementation would use a shader property
        }

        void SpawnFloatingText(string text, Color color)
        {
            // Spawn from pool
            var go = Bill.Pool.Spawn("ui_float_text");
            if (go == null) return;

            go.transform.position = transform.position + Vector3.up * 1.2f;
            var tm = go.GetComponentInChildren<TextMesh>();
            if (tm != null)
            {
                tm.text = text;
                tm.color = color;
            }

            BillTween.MoveY(go.transform, go.transform.position.y + 0.8f, 0.5f);
            Bill.Pool.Return(go, 0.6f);
        }
    }
}
