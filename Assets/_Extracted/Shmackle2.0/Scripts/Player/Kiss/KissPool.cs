using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.XR.Shared.Core;
using MessagePipe;
using Shmackle.Utilities;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Shmackle.Player.Kiss
{
    /// <summary>
    /// Manages a pool of kiss decal projectors that can be spawned and despawned.
    /// Handles both local and networked kiss decal visualization.
    /// </summary>
    public class KissPool : MonoBehaviour
    {
        [SerializeField] private DecalProjector _kissPrefab;
        [SerializeField] private int _poolSize = 15;
        [SerializeField] private float _decalLifetime = 10f;

        private SimpleObjectPool<DecalProjector> _kissPool;
        private ISubscriber<KissNetworkEventRelay> _playerKissEventSubscriber;
        private IDisposable _kissEventBagDisposable;

        private NetworkObject _networkObject;
        private IHardwareRig _localHardwareRig;
        private KissPool _localHardwareKissPool;

        private WaitForSeconds _decalLifetimeWaiter;
        private readonly Dictionary<DecalProjector, Coroutine> _despawnRoutines = new(); // Tracks active despawn routines

        private void Awake()
        {
            if (!_networkObject)
                _networkObject = GetComponentInParent<NetworkObject>();

            if (_localHardwareRig == null)
                _localHardwareRig = HardwareRigsRegistry.GetHardwareRig();

            _playerKissEventSubscriber = GlobalMessagePipe.GetSubscriber<KissNetworkEventRelay>();

            // Cache the wait time for decal lifetime
            _decalLifetimeWaiter = new WaitForSeconds(_decalLifetime);
        }

        private void Start()
        {
            // Initialize the pool when:
            // - No NetworkObject found -> likely the local HARDWARE rig
            // OR
            // - NetworkObject has NO Input Authority -> remote player's NETWORK rig
            // We don't initialize pool to the local player's NETWORK rig since it uses the (InvisibleForLocalPlayer) layer.
            if (!_networkObject || !_networkObject.HasInputAuthority)
                InitializePool();
        }

        /// <summary>
        /// Initializes the object pool if not already created
        /// </summary>
        private void InitializePool()
        {
            if (_kissPool == null && _kissPrefab != null)
                _kissPool = new SimpleObjectPool<DecalProjector>(_kissPrefab, _poolSize, transform);
        }

        private void OnEnable()
        {
            if (_playerKissEventSubscriber == null) return;

            _kissEventBagDisposable = _playerKissEventSubscriber.Subscribe(e =>
            {
                if (_networkObject && e.TargetId == _networkObject.Id)
                {
                    // Convert the local offset back to world space based on this object's current position
                    Vector3 worldPosition = transform.TransformPoint(e.ContactPosition);
                    Quaternion worldRotation = transform.rotation * e.ContactRotation;

                    if (_networkObject.HasInputAuthority)
                    {
                        // For authority objects, find and use the local hardware kiss pool
                        if (!_localHardwareKissPool && _localHardwareRig != null)
                            _localHardwareKissPool = _localHardwareRig.transform.GetComponentInChildren<KissPool>(true);

                        if (_localHardwareKissPool)
                            _localHardwareKissPool.Spawn(worldPosition, worldRotation);
                    }
                    else
                        Spawn(worldPosition, worldRotation);
                }
            });
        }

        private void OnDisable()
        {
            _kissEventBagDisposable?.Dispose();
            _kissEventBagDisposable = null;
        }

        /// <summary>
        /// Spawns a kiss decal at the specified position and rotation
        /// </summary>
        public void Spawn(Vector3 position, Quaternion rotation)
        {
            if (_kissPool == null) return;

            DecalProjector decal = _kissPool.Get();
            if (decal)
            {
                decal.transform.SetPositionAndRotation(position, rotation);

                // Cancel existing despawn routine if any
                if (_despawnRoutines.TryGetValue(decal, out Coroutine routine) && routine != null)
                    StopCoroutine(routine);

                // Start new despawn routine
                _despawnRoutines[decal] = StartCoroutine(DespawnAfterSeconds(decal));
            }
        }

        /// <summary>
        /// Returns a decal to the pool
        /// </summary>
        public void Despawn(DecalProjector decal)
        {
            if (!decal || _kissPool == null) return;

            // Cancel any active despawn routine
            if (_despawnRoutines.TryGetValue(decal, out Coroutine routine) && routine != null)
                StopCoroutine(routine);

            _despawnRoutines.Remove(decal);
            _kissPool.Release(decal);
        }

        /// <summary>
        /// Coroutine to automatically despawn a decal after the lifetime expires
        /// </summary>
        private IEnumerator DespawnAfterSeconds(DecalProjector decal)
        {
            yield return _decalLifetimeWaiter;
            Despawn(decal);
        }
    }
}