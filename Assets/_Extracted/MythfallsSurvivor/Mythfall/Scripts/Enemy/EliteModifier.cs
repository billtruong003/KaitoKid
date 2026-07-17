using UnityEngine;

namespace Mythfall.Enemy
{
    /// <summary>
    /// Promote a regular enemy to "elite": x3 HP, x2 damage, +30% scale, red emission glow.
    /// Attach BEFORE the enemy's OnSpawn runs (i.e. on the prefab, OR add via WaveSpawner
    /// when rolling elite chance and spawn together with the enemy).
    ///
    /// Stat multipliers are applied via <see cref="EnemyBase.SetStatMultipliers"/> which
    /// must be called before OnSpawn re-initializes HP. We do this in Awake so order
    /// is guaranteed regardless of who calls OnSpawn first.
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    public class EliteModifier : MonoBehaviour
    {
        [SerializeField] float hpMultiplier = 3f;
        [SerializeField] float damageMultiplier = 2f;
        [SerializeField] float scaleMultiplier = 1.3f;
        [SerializeField] Color emissionColor = new Color(1f, 0.2f, 0.2f) * 2f;

        EnemyBase _enemy;
        bool _applied;

        void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            Apply();
        }

        void OnEnable()
        {
            // Pool-spawned enemies may have Awake fired once but OnEnable fires every
            // respawn — reapply visuals (scale persists from Awake; only emission may
            // be stomped by other systems).
            ApplyVisual();

            // Stat multipliers must reapply before OnSpawn so the new HP scales.
            if (_enemy != null) _enemy.SetStatMultipliers(hpMultiplier, damageMultiplier);
        }

        void Apply()
        {
            if (_applied || _enemy == null) return;
            _applied = true;

            _enemy.SetStatMultipliers(hpMultiplier, damageMultiplier);
            transform.localScale *= scaleMultiplier;
            ApplyVisual();
        }

        void ApplyVisual()
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", emissionColor);
            renderer.SetPropertyBlock(mpb);

            // URP Lit needs the emission keyword enabled on the SHARED material for the
            // GI / postFX pipeline to recognise emission. Safe to call repeatedly.
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null) continue;
                mat.EnableKeyword("_EMISSION");
            }
        }
    }
}
