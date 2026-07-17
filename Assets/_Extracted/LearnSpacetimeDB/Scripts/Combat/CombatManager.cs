#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using UnityEngine;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline
{
    /// <summary>
    /// Singleton that handles combat visual effects.
    /// Listens to the damage_event table for inserts and spawns floating damage
    /// numbers, hit VFX/SFX, and triggers screen shake when the local player takes damage.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        // -------------------------------------------------------
        // Singleton
        // -------------------------------------------------------

        public static CombatManager Instance { get; private set; }

        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("VFX Pools")]
        [Tooltip("Pool key for the floating damage number prefab.")]
        [SerializeField] private string damagePopupPoolKey = "DamagePopup";

        [Tooltip("Pool key for the hit effect VFX prefab.")]
        [SerializeField] private string hitVfxPoolKey = "HitVFX";

        [Header("Screen Shake")]
        [SerializeField] private float shakeIntensity = 0.15f;
        [SerializeField] private float shakeDuration = 0.2f;

        [Header("Audio")]
        [SerializeField] private string hitSfxKey = "hit_impact";
        [SerializeField] private string critSfxKey = "hit_crit";

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        private Camera _mainCamera;
        private Vector3 _cameraOriginalPos;
        private float _shakeTimer;
        private float _currentShakeIntensity;

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            RegisterCallbacks();

            if (Bill.IsReady)
            {
                Bill.Events.Subscribe<LocalPlayerDamagedEvent>(OnLocalPlayerDamaged);
            }
        }

        private void OnDisable()
        {
            UnregisterCallbacks();

            if (Bill.IsReady)
            {
                Bill.Events.Unsubscribe<LocalPlayerDamagedEvent>(OnLocalPlayerDamaged);
            }
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            RegisterDamagePopupPool();
        }

        private void Update()
        {
            UpdateScreenShake();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // -------------------------------------------------------
        // Pool Setup
        // -------------------------------------------------------

        private void RegisterDamagePopupPool()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.DamagePopupPrefab == null) return;

            if (Bill.IsReady && Bill.Pool != null)
            {
                Bill.Pool.Register(damagePopupPoolKey, gm.DamagePopupPrefab, 10);
            }
        }

        // -------------------------------------------------------
        // Callback Registration
        // -------------------------------------------------------

        private void RegisterCallbacks()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            gm.Connection.Db.DamageEvent.OnInsert += OnDamageEventInsert;
        }

        private void UnregisterCallbacks()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            gm.Connection.Db.DamageEvent.OnInsert -= OnDamageEventInsert;
        }

        // -------------------------------------------------------
        // Damage Event Handler
        // -------------------------------------------------------

        private void OnDamageEventInsert(EventContext ctx, DamageEvent dmgEvent)
        {
            Vector3 worldPos = new Vector3(dmgEvent.PosX, dmgEvent.PosY, 0f);

            // Spawn floating damage number (no IsHeal field in generated bindings)
            SpawnDamagePopup(worldPos, dmgEvent.Damage, dmgEvent.IsCrit, false);

            // Spawn hit VFX
            SpawnHitVFX(worldPos);

            // Play hit SFX
            PlayHitSFX(dmgEvent.IsCrit);
        }

        // -------------------------------------------------------
        // Damage Popup
        // -------------------------------------------------------

        private void SpawnDamagePopup(Vector3 position, int amount, bool isCrit, bool isHeal)
        {
            if (!Bill.IsReady || Bill.Pool == null) return;

            // Slight random offset so overlapping hits don't stack exactly
            Vector3 offset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0f, 0.2f),
                0f
            );

            GameObject popupObj = Bill.Pool.Spawn(damagePopupPoolKey, position + offset, Quaternion.identity);
            if (popupObj == null) return;

            var popup = popupObj.GetComponent<DamagePopup>();
            if (popup != null)
            {
                DamagePopupType popupType;
                if (isHeal) popupType = DamagePopupType.Heal;
                else if (isCrit) popupType = DamagePopupType.Crit;
                else popupType = DamagePopupType.Normal;

                popup.Initialize(amount, popupType);
            }
        }

        // -------------------------------------------------------
        // Hit VFX
        // -------------------------------------------------------

        private void SpawnHitVFX(Vector3 position)
        {
            if (!Bill.IsReady || Bill.Pool == null) return;

            GameObject vfx = Bill.Pool.Spawn(hitVfxPoolKey, position, Quaternion.identity);
            if (vfx != null)
            {
                // Auto-return after a short duration
                Bill.Pool.Return(vfx, 0.5f);
            }
        }

        // -------------------------------------------------------
        // Audio
        // -------------------------------------------------------

        private void PlayHitSFX(bool isCrit)
        {
            if (!Bill.IsReady || Bill.Audio == null) return;

            string sfxKey = isCrit ? critSfxKey : hitSfxKey;
            Bill.Audio.Play(sfxKey);
        }

        // -------------------------------------------------------
        // Screen Shake
        // -------------------------------------------------------

        private void OnLocalPlayerDamaged(LocalPlayerDamagedEvent evt)
        {
            TriggerScreenShake(shakeIntensity, shakeDuration);
        }

        /// <summary>
        /// Trigger a camera screen shake effect.
        /// </summary>
        public void TriggerScreenShake(float intensity, float duration)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            _currentShakeIntensity = intensity;
            _shakeTimer = duration;
            _cameraOriginalPos = _mainCamera.transform.position;
        }

        private void UpdateScreenShake()
        {
            if (_shakeTimer <= 0f) return;

            _shakeTimer -= Time.deltaTime;

            if (_shakeTimer <= 0f)
            {
                // Restore camera position (camera follow will handle re-centering)
                _shakeTimer = 0f;
                return;
            }

            // Apply random offset
            float dampening = _shakeTimer / shakeDuration;
            float offsetX = Random.Range(-1f, 1f) * _currentShakeIntensity * dampening;
            float offsetY = Random.Range(-1f, 1f) * _currentShakeIntensity * dampening;

            if (_mainCamera != null)
            {
                Vector3 pos = _mainCamera.transform.position;
                _mainCamera.transform.position = new Vector3(
                    pos.x + offsetX,
                    pos.y + offsetY,
                    pos.z
                );
            }
        }
    }
}

#endif // STDB_BINDINGS
