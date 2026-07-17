using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using TMPro;
using Teabag.Player;
using UnityEngine;
using IAudioService = Teabag.Core.IAudioService;
using Fusion;
using Teabag.Networking;

namespace Teabag.Gameplay
{
        public class DummyTarget : MonoBehaviour, IHittable
        {
            [Header("-- READ ONLY --")]
            [SerializeField] private byte _currentHealthDisplay;

        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private List<AdvancedAudioClip> _hitClips;
        [SerializeField] private AdvancedAudioClip _killClip;
        [SerializeField] private AdvancedAudioClip _shieldHitClip;
        [SerializeField] private float _respawnDelay = 5f;

        private IAudioService _audioService;
        private byte _currentHealth;
        private bool _isDead;
        private Collider[] _colliders;
        private Renderer[] _renderers;
        private WaitForSeconds _waitRespawn;

        public bool IsDead => _isDead;

        private void Awake()
        {
            _audioService = ServiceLocator.Get<IAudioService>();

            _colliders = GetComponentsInChildren<Collider>();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        private void OnEnable()
        {
            if (ServiceLocator.TryGet<IDamageableRegistry>(out var damageableRegistry))
                damageableRegistry.RegisterDummyTarget(this);
        }

        private void OnDisable()
        {
            if (ServiceLocator.TryGet<IDamageableRegistry>(out var damageableRegistry))
                damageableRegistry.UnregisterDummyTarget(this);
        }

        private void Start()
        {
            ResetHealth();
        }

        public bool Damage(byte damage, HitType hitType, float crit = 1.5f)
        {
            if (_isDead is true)
                return false;

            bool isCritical = hitType == HitType.Head || hitType == HitType.Nut;
            if (isCritical)
                damage = (byte)(damage * (crit <= 0 ? 1.5f : crit));

            Vector3 headPos = transform.position + Vector3.up * 1.8f;

            if (!isCritical)
                GameServices.DisplayPopupColored?.Invoke(damage.ToString(), headPos, Color.white, 0.5f);
            else
                GameServices.DisplayPopupColored?.Invoke(damage.ToString(), headPos, Color.yellow, 0.6f);

            int newHealth = _currentHealth - damage;
            _currentHealth = (byte)Mathf.Max(newHealth, 0);
            _currentHealthDisplay = _currentHealth;

            if (_hpText != null)
                _hpText.text = _currentHealth.ToString();

            _audioService.Play(_hitClips, headPos);

            if (_currentHealth == 0)
            {
                Die();
                return true;
            }

            return false;
        }

        private void Die()
        {
            _isDead = true;

            Vector3 headPos = transform.position + Vector3.up * 1.8f;

            GameServices.DisplayPopupColored?.Invoke("Kill", headPos, Color.red, 1f);
            _audioService.Play(_killClip, headPos);

            if (_colliders != null)
            {
                foreach (Collider col in _colliders)
                {
                    if (col.name == "Finger") continue;
                    col.enabled = false;
                }
            }

            if (_renderers != null)
            {
                foreach (Renderer rend in _renderers)
                    rend.enabled = false;
            }

            StartCoroutine(WaitAndRespawn());
        }


        private IEnumerator WaitAndRespawn()
        {
            if (_waitRespawn == null) _waitRespawn = new WaitForSeconds(_respawnDelay);
            yield return _waitRespawn;
            Respawn();
        }

        private void Respawn()
        {
            ResetHealth();

            if (_colliders != null)
            {
                foreach (Collider col in _colliders)
                {
                    if (col.name == "Finger") continue;
                    col.enabled = true;
                }
            }

            if (_renderers != null)
            {
                foreach (Renderer rend in _renderers)
                    rend.enabled = true;
            }

            _isDead = false;
        }

        private void ResetHealth()
        {
            _currentHealth = 100;
            _currentHealthDisplay = _currentHealth;

            if (_hpText != null)
                _hpText.text = _currentHealth.ToString();
        }

        public void OnHit(byte damage, float bulletSpeed, RaycastHit hit, Vector3 source, PlayerRef? killer = null)
        {
            if (_isDead) return;

            HitType hitType = HitType.Normal;
            if (hit.transform.CompareTag(Gorilla.TAG_HEAD)) hitType = HitType.Head;
            else if (hit.transform.CompareTag(Gorilla.TAG_NUT)) hitType = HitType.Nut;

            Damage(damage, hitType);
        }
    }
}
