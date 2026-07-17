using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using Microsoft.Extensions.Logging;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;
using IAudioService = Teabag.Core.IAudioService;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Random = UnityEngine.Random;


namespace Teabag.Gameplay
{
    public enum ChestState { Uninitialized, Closed, Opening, Opened }

    public class Chest : RoyaleObject
    {
        [Header("Lid Opening")]
        [SerializeField] private Lock _lock;
        [SerializeField] private AnimationClip _openingAnimation;
        [SerializeField] private float _spawnDelay = 1f;

        [Header("Spawning")]
        [SerializeField] private SpawnChanceDataObject _spawnChanceData;
        [SerializeField] private int _bulletAmountPerChest = 2;
        [SerializeField] private float _spawnArcHeight = 1f;
        [SerializeField] private float _spawnTravelDuration = 0.5f;
        [SerializeField] private float _minDistBetweenSpawned = 0.2f;
        [SerializeField] private Vector3 _spawnBoxSize;
        [SerializeField] private Vector3 _spawnBoxCenter;

        [Header("FX")]
        [SerializeField] private AdvancedAudioClip _openSfx;
        [SerializeField] private AdvancedAudioClip _unlockSfx;
        [SerializeField] private ParticleSystem _particleOpenChest;

        [Header("Networked")]
        [Networked] public NetworkBool canOpen { get; set; }
        [Networked] public ChestState currentState { get; private set; }

        private List<Vector3> _lootSpawned;
        private IAudioService _audioService;
        private Animation _animation;
        private AnimationState _openState;
        private static readonly ILogger _logger = JungleXRLogger.GetLogger();
        private WaitForSeconds _waitDelay;

        public override void Spawned()
        {
            base.Spawned();

            _lock = transform.GetComponentInChildren<Lock>(true);
            _audioService = ServiceLocator.Get<IAudioService>();
            _animation = GetComponent<Animation>();

            if (_lock != null)
            {
                _lock.onStateUpdated += OnLockUpdated;
            }

            if (_animation != null)
            {
                _openState = _animation[_openingAnimation.name];
                _openState.enabled = true;
                _openState.weight = 1f;
                _openState.wrapMode = WrapMode.ClampForever;
                _animation.Play();
            }

            _lootSpawned = new List<Vector3>();

            if (Object.HasStateAuthority && currentState == ChestState.Uninitialized)
            {
                currentState = ChestState.Closed;
                canOpen = true;
            }
            else if (currentState != ChestState.Closed && _lock != null)
            {
                _lock.enabled = false;
                _lock.gameObject.SetActive(false);
            }

            ApplyStateVisuals();
        }


        private void ApplyStateVisuals()
        {
            if (_openState == null)
                return;

            if (currentState != ChestState.Closed)
            {
                _openState.time = _openState.length;
                _openState.speed = 0f;
            }
            else
            {
                _openState.time = 0f;
                _openState.speed = 0f;
            }
        }

        private void OnLockUpdated(Lock.LockState state, string playerID)
        {
            try
            {
                if (state == Lock.LockState.StartedUnlocking)
                {
                    _audioService?.Play(_unlockSfx, transform.position);
                }
                else if (state == Lock.LockState.Unlocked)
                {
                    // Notify all clients that the lock was unlocked
                    // Only the state authority holder will actually open the chest
                    RPC_RequestOpen(playerID);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        private void RPC_RequestOpen(string playerID)
        {
            _ = TryOpenChestAsync(playerID);
        }

        /// <summary>
        /// Acquires chest authority when needed, then opens the chest exactly once so loot spawns on the authority owner.
        /// </summary>
        private async UniTask TryOpenChestAsync(string playerID)
        {
            if (currentState != ChestState.Closed || !canOpen || Object == null || Runner == null)
                return;

            if (!Object.HasStateAuthority)
            {
                // Let the new shared master recover orphaned chest authority before opening.
                if (Runner.IsSharedModeMasterClient)
                    await RequestStateAuthorityAsync();

                if (!Object || !Object.IsValid || !Object.HasStateAuthority)
                    return;

                // The state may have changed while waiting for authority, so validate again before opening.
                if (currentState != ChestState.Closed || !canOpen || Object == null || Runner == null)
                    return;
            }

            currentState = ChestState.Opening;
            canOpen = false;
            RPC_OpenLid();
            StartCoroutine(OpenChestCoroutine(playerID));
        }


        IEnumerator OpenChestCoroutine(string playerID)
        {
            if (_waitDelay == null) _waitDelay = new WaitForSeconds(_spawnDelay);
            yield return _waitDelay;
            _particleOpenChest?.Play();
            RPCSpawnRandomizedLoot(playerID);
            currentState = ChestState.Opened;
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        private void RPC_OpenLid()
        {
            _lock?.DetachLock();

            _audioService?.Play(_openSfx, transform.position);

            if (_openState != null)
            {
                _openState.time = 0f;
                _openState.speed = 1f;
            }
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        private void RPCSpawnRandomizedLoot(string playerID)
        {
            //get luck from playerID here, for now, hard coded to 0
            float playerLuck = 0;
            RarityChance[] rarityChances = _spawnChanceData?.RarityChances;
            ItemTypeChance[] typeChances = _spawnChanceData?.ItemTypeChances;
            ObjectTypeChance[] ObjectChances = _spawnChanceData?.ObjectTypeChances;

            if (rarityChances == null || typeChances == null)
            { return; }

            List<float> typeValues = new List<float>();
            float sumOfTypeChances = 0;

            for (int i = 0; i < typeChances.Length; i++)
            {
                float value = typeChances[i].Chance;
                typeValues.Add(value);
                sumOfTypeChances += value;
            }

            List<float> rarityValues = new List<float>();
            float sumOfRarityChances = 0;

            for (int i = 0; i < rarityChances.Length; i++)
            {
                float value = rarityChances[i].Chance;
                rarityValues.Add(value);
                sumOfRarityChances += value;
            }

            List<float> objectValues = new List<float>();
            float sumOfObjectChances = 0;

            for (int i = 0; i < ObjectChances.Length; i++)
            {
                float value = ObjectChances[i].Chance;
                objectValues.Add(value);
                sumOfObjectChances += value;
            }

            float typeRoll = Random.Range(0, sumOfTypeChances) + playerLuck;
            float rarityRoll = Random.Range(0, sumOfRarityChances) + playerLuck;
            float objectRoll = Random.Range(0, sumOfObjectChances); //not adding player luck because all objects are same chance
            int wpnTypeIdx = GetIdxFromLuck(typeRoll, ref typeValues);
            int rarityIdx = GetIdxFromLuck(rarityRoll, ref rarityValues);
            int objIdx = GetIdxFromLuck(objectRoll, ref objectValues);

            string weaponTypeID = typeChances[wpnTypeIdx].ID;
            string objectTypeID = ObjectChances[objIdx].ID;
            Rarity weaponRarity = rarityChances[rarityIdx].Rarity;

            GameObject[] availableWeapons = GameServices.GetWeaponData((weaponTypeID, weaponRarity));
            GameObject wpn = availableWeapons[Random.Range(0, availableWeapons.Length)];
            GameObject bullet = GameServices.GetAmmoFromType(weaponTypeID);
            GameObject obj = GameServices.GetObjectData(objectTypeID);

            SpawnItem(wpn);
            SpawnItem(obj);

            for (int i = 0; i < _bulletAmountPerChest; i++)
            {
                SpawnItem(bullet);
            }
        }

        private int GetIdxFromLuck(float score, ref List<float> list, float currentValue = 0, int currentIdx = 0)
        {
            if (currentIdx < 0 || currentIdx >= list.Count)
                return -1;

            currentValue += list[currentIdx];
            if (score < currentValue)
                return currentIdx;

            else return GetIdxFromLuck(score, ref list, currentValue, currentIdx + 1);
        }

        private bool SpawnItem(GameObject obj)
        {
            if (!GameServices.GetRunner() || obj == null)
            {
                return false;
            }

            NetworkObject nObj = GameServices.GetRunner().Spawn(obj);
            var spawner = nObj.GetComponent<SpawnFromChest>();

            if (spawner != null)
            {
                int tries = 20;
                bool canSpawn = false;
                Vector3 random = new Vector3();
                Vector3 endPos = new Vector3();

                while (!canSpawn && tries > 0)
                {
                    tries--;
                    random.x = Random.Range(-0.5f * _spawnBoxSize.x, 0.5f * _spawnBoxSize.x);
                    random.y = Random.Range(-0.5f * _spawnBoxSize.y, 0.5f * _spawnBoxSize.y);
                    random.z = Random.Range(-0.5f * _spawnBoxSize.z, 0.5f * _spawnBoxSize.z);
                    endPos = transform.position + transform.TransformDirection(_spawnBoxCenter + random);

                    if (_lootSpawned.Count == 0) break;

                    for (int i = 0; i < _lootSpawned.Count; i++)
                    {
                        if (Vector3.Distance(endPos, _lootSpawned[i]) < _minDistBetweenSpawned)
                        {
                            canSpawn = false;
                            break;
                        }
                        canSpawn = true;
                    }
                }
                spawner.Initialize(transform.position, endPos, _spawnArcHeight, _spawnTravelDuration);
                _lootSpawned.Add(endPos);
                return true;
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            //tool to see the spawn box
            Vector3 center = transform.position + _spawnBoxCenter;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.red;
            Gizmos.DrawCube(_spawnBoxCenter, _spawnBoxSize);
        }
    }
}
