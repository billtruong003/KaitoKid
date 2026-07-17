using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Networking;
using Teabag.Core;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Gameplay
{
    public class SupplyDrop : RoyaleObject
    {
        [SerializeField] private bool disableTrySpawn = false;
        private const float MIN_CHEST_DISTANCE = 1.5f;
        private const int MAX_SPAWN_ATTEMPTS = 50;
        private static readonly HashSet<Vector3> s_ReservedPositions = new();

        public Chest chest;

        [Header("Data")]
        public float t;
        [Networked]
        public Vector3 startPosition { get; set; }
        [Networked]
        public Vector3 endPosition { get; set; }
        [Networked]
        public float yRotation { get; set; }
        [Networked, OnChangedRender(nameof(OnFloatChanged))]
        public float f { get; set; }

        public bool touchingGround = false;

        [Header("References")]
        public GameObject parachute;
        public GameObject target;
        public Health health;
        public ParticleSystem particles;

        [Header("Audio")]
        public AdvancedAudioClip landingClip;

        private Vector3 reservedPosition = Vector3.zero;

        public INetworkManager NetworkManager
        {
            get
            {
                if (_networkManager == null)
                {
                    _networkManager = ServiceLocator.Get<INetworkManager>();
                }
                return _networkManager;
            }
        }
        private INetworkManager _networkManager;
        private bool _isSpawned = false;
        private static readonly WaitForSeconds _waitDestroyDelay = new WaitForSeconds(1f);

        public override void SpawnedRoyale()
        {
            base.SpawnedRoyale();

            NetworkObjectsManager.OnLeft += OnPlayerLeft;

            InitializeChest();
            OnFloatChanged();

            if (!disableTrySpawn && Object.HasStateAuthority)
                TrySpawnChest();

            if (Object == null || !Object.IsValid) return;

            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            _isSpawned = true;
        }

        public override void DespawnedRoyale(NetworkRunner runner, bool hasState)
        {
            NetworkObjectsManager.OnLeft -= OnPlayerLeft;
            ReleaseReservedPosition();
            base.DespawnedRoyale(runner, hasState);
        }

        /// <summary>
        /// Reclaims shared-mode authority for an in-flight drop when the previous authority owner leaves.
        /// </summary>
        private void OnPlayerLeft(PlayerRef player)
        {
            if (!Object || Runner == null || Runner.IsShutdown)
                return;

            // Only airborne drops need recovery; landed drops hand off to the chest interaction flow instead.
            if (touchingGround || Object.HasStateAuthority || !Runner.IsSharedModeMasterClient)
                return;

            RequestStateAuthority();
        }

        private void InitializeChest()
        {
            chest.followNetworkPosition = false;
            chest.canOpen = false;
        }

        private void TrySpawnChest()
        {
            int attempts = 0;
            bool hasValidPosition = false;

            while (!hasValidPosition && attempts < MAX_SPAWN_ATTEMPTS)
            {
                GenerateRandomStartPosition();
                hasValidPosition = ProcessGameModeSpawning();

                if (!hasValidPosition)
                    hasValidPosition = TryRaycastSpawn();

                attempts++;
            }

            if (!hasValidPosition)
                CleanupAndDespawn();
        }

        private void GenerateRandomStartPosition()
        {
            yRotation = Random.Range(0, 360);
            var mapService = ServiceLocator.Get<IMapService>();
            startPosition = mapService?.OnGetRandomMapPoint?.Invoke(50) ?? (Vector3.up * 50);
        }

        private bool ProcessGameModeSpawning()
        {
            switch (NetworkManager.CurrentGameMode)
            {
                /*case GameModeSo.Ids.Deathmatch:
                    return HandleDeathmatchSpawning();
                case GameModeSo.Ids.DeathmatchSpace:
                    SetInstantSpawn();
                    return true;*/

                default:
                    GameLogger.Info("No special spawning for battle royale");GameLogger.Info("No special spawning for battle royale");
                    return false;
            }
        }

        private bool HandleDeathmatchSpawning()
        {
            float spawnRoll = Random.Range(0, 100);

            if (spawnRoll < 30)
            {
                // Regular supply drop - continue to raycast
                return false;
            }
            else if (spawnRoll < 80)
                return TryChestSpawn();
            return TryChestFallSpawn();
        }

        private bool TryChestSpawn()
        {
            GameObject spawnMark = Mark.FindRandomMark("ChestSpawn");
            if (spawnMark == null)
            {
                GameLogger.Info("No ChestSpawn mark found - skipping");
                return false;
            }

            Vector3 candidatePosition = spawnMark.transform.position;
            if (!IsPositionAvailable(candidatePosition))
            {
                GameLogger.Info("Position taken or reserved - skipping");
                return false;
            }

            // reserve and set position
            ReservePosition(candidatePosition);
            SetChestSpawnTransform(spawnMark, candidatePosition);
            SetInstantSpawn();
            return true;
        }

        private bool TryChestFallSpawn()
        {
            GameObject fallMark = Mark.FindRandomMark("ChestFallSpawn");
            if (fallMark == null)
            {
                GameLogger.Info("No ChestFallSpawn mark found - skipping");
                return false;
            }

            SetChestFallTransform(fallMark);
            SetChestFallHealth();
            return false; // We return false - because we still want to raycast
        }

        private bool TryRaycastSpawn()
        {
            if (endPosition != Vector3.zero) return true;
            if (!Physics.Raycast(new Ray(startPosition, Vector3.down), out RaycastHit hit,
                Mathf.Infinity, LayerMask.GetMask("Default", "Chest", "Terrain")))
            {
                return false;
            }

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Chest"))
                return false;

            Vector3 candidateEndPosition = hit.point;
            if (!IsPositionAvailable(candidateEndPosition))
            {
                return false;
            }

            // reserve and set end position
            ReservePosition(candidateEndPosition);
            endPosition = candidateEndPosition;
            return true;
        }

        private bool IsPositionAvailable(Vector3 position)
        {
            // check existing chests
            foreach (Chest existingChest in FindObjectsOfType<Chest>())
            {
                if (Vector3.Distance(position, existingChest.transform.position) < MIN_CHEST_DISTANCE)
                {
                    return false;
                }
            }

            // check reserved positions
            foreach (Vector3 reservedPos in s_ReservedPositions)
            {
                if (Vector3.Distance(position, reservedPos) < MIN_CHEST_DISTANCE)
                {
                    return false;
                }
            }

            return true;
        }

        private void ReservePosition(Vector3 position)
        {
            s_ReservedPositions.Add(position);
            reservedPosition = position;
        }

        private void SetChestSpawnTransform(GameObject spawnMark, Vector3 position)
        {
            yRotation = spawnMark.transform.eulerAngles.y;
            startPosition = position;
            endPosition = position;
        }

        private void SetChestFallTransform(GameObject fallMark)
        {
            yRotation = fallMark.transform.eulerAngles.y;
            startPosition = fallMark.transform.position;
        }

        private void SetChestFallHealth()
        {
            health.CurrentHealthAmount = 0;
            health.MaxHealth = 0;
        }

        private void SetInstantSpawn()
        {
            t = 1;
            f = 1;
        }

        private void CleanupAndDespawn()
        {
            if (reservedPosition != Vector3.zero)
            {
                s_ReservedPositions.Remove(reservedPosition);
            }
            StartCoroutine(IEDestroying());
        }



        private IEnumerator IEDestroying()
        {
            yield return _waitDestroyDelay;
            Runner.Despawn(Object);
        }

        public void Update()
        {
            if (!Object || Runner == null)
                return;

            if (!_isSpawned) return;

            if (t < 1)
            {
                UpdateFalling();
            }
            else
                HandleLanding();

            UpdateTransform();
        }

        private void UpdateFalling()
        {
            float fallSpeed = health.CurrentHealthAmount > 0 ? 0.05f : 0.25f;
            float heightFactor = 50f / (startPosition.y - endPosition.y);
            t += Time.deltaTime * fallSpeed * heightFactor;
            t = Mathf.Clamp01(t);

            parachute.SetActive(health.CurrentHealthAmount > 0);

            if (Object.HasStateAuthority && Mathf.Abs(t - f) > 0.05f)
            {
                f = t;
            }
        }

        private void HandleLanding()
        {
            if (Object.HasStateAuthority)
            {
                f = 1;
            }

            if (!touchingGround)
            {
                ExecuteLandingEffects();
                ReleaseReservedPosition();
                touchingGround = true;
            }
        }

        private void ExecuteLandingEffects()
        {
            if (particles != null)
                particles.Play();

            target.SetActive(false);
            parachute.SetActive(false);

            var audioService = ServiceLocator.Get<IAudioService>();
            audioService.Play(landingClip, transform.position);

            chest.canOpen = true;
        }

        private void UpdateTransform()
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            target.transform.position = endPosition + Vector3.up * 0.1f;
        }

        private void ReleaseReservedPosition()
        {
            if (reservedPosition != Vector3.zero)
            {
                s_ReservedPositions.Remove(reservedPosition);
                reservedPosition = Vector3.zero;
            }
        }

        void OnFloatChanged()
        {
            t = f;
        }

    }
}
