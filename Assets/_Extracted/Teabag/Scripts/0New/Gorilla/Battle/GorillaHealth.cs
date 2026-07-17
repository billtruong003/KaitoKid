using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using GorillaLocomotion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.GameMode;
using UnityEngine;
using Teabag.Services;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Player
{
    public enum HitType
    {
        Normal,
        Head,
        Nut
    }

    public class GorillaHealth : Health
    {
        public static event Action<GorillaHealth> OnGorillaDied;

        private IGorillaService _gorillaService;
        private IPerkService _perkService;
        private ITeleportService _teleportService;

        private IHardwareRig LocalHardwareRig
        {
            get
            {
                if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                    return rigInfo.HardwareRig;
                return null;
            }
        }

        public bool AutoRespawn
        {
            get
            {
                return autoRespawn || GameServices.GetCurrentGameMode?.Invoke() == NetworkGameModeIds.SpaceStation;
            }
        }

        public bool autoRespawn = false;
        public AdvancedAudioClip killClip;
        public AdvancedAudioClip shieldHitClip;
        public AdvancedAudioClip nutHitClip;
        public List<AdvancedAudioClip> hitClips = new List<AdvancedAudioClip>();

        [Header("VFX")]
        public GameObject nutHitVFX;

        private IAudioService _audioService;

        byte lastHealth = 0;
        bool killed = false;
        bool wasDead = false;
        bool ragdoll = false;
        int teabagBonusCount = 0;

        Gorilla gorilla;
        GorillaRagdoll activeRagdoll;
        Collider[] colliders;

        Vector3 cachedDeathPos;
        Quaternion cachedDeathRot;
        Vector3 cachedDeathVelocity;

        [Header("Visuals")] public List<GameObject> deadDisableObjects = new List<GameObject>();
        public List<Renderer> deadDisableRenderers = new List<Renderer>();

        private void Awake()
        {
            gorilla = GetComponent<Gorilla>();
            if (gorilla)
                colliders = gorilla.rootBoneTransform.GetComponentsInChildren<Collider>();

            _gorillaService = ServiceLocator.Get<IGorillaService>();
            _audioService = ServiceLocator.Get<IAudioService>();
            _teleportService = ServiceLocator.Get<ITeleportService>();
        }

        private void OnEnable()
        {
            GameServices.OnGameReset += OnGameReset;
        }

        private void OnDisable()
        {
            GameServices.OnGameReset -= OnGameReset;
        }

        public override void SpawnedRoyale()
        {
            base.SpawnedRoyale();
            _perkService = ServiceLocator.Get<IPerkService>();

            ApplyHealthPerk();
            ApplyShieldPerk();
            CurrentHealthAmount = MaxHealth;
        }

        private void OnGameReset()
        {
            // Only the owner of this health object may respawn it.
            // When the game resets, respawn (even if alive, to reset position/stats).
            if (HasStateAuthority)
            {
                _ = Respawn();
            }
        }

        public override void Spawned()
        {
            base.Spawned();
            lastHealth = TotalHealth;
        }

        public override void Render()
        {
            base.Render();

            // set dead state
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
            bool isMyDead = (_gorillaService?.LocalGorilla as Gorilla)?.health?.isDead ?? false;

            // set dead disable objects and renderers
            foreach (GameObject obj in deadDisableObjects)
            {
                // preserve finger collision state
                // https://discord.com/channels/1163138896872349767/1386092998831374554
                if (obj.name == "Finger") continue;
                obj.SetActive(!isMyDead || !isDead);
            }

            foreach (Renderer rend in deadDisableRenderers)
                rend.enabled = !isMyDead || !isDead;
            foreach (Collider collider in colliders)
            {
                // preserve finger collision state
                // https://discord.com/channels/1163138896872349767/1386092998831374554
                if (collider.name == "Finger") continue;
                collider.enabled = !isDead;
            }

            if (!m_DamageAnimation)
                Refresh();

            // this causes the ragdoll to spawn uncontrollably
            // else ragdoll = false;
        }

        public bool Damage(byte damage, HitType hitType, PlayerRef? killer = null, Vector3? attackerWorldPos = null, float crit = 1.5f)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD // disable damage for local player if God Mode is enabled (debug only, this doesn't appear in production)
            if (Object && Object.StateAuthority == Runner.LocalPlayer && GameServices.GodModeEnabled)
                return false;
#endif

            if (isDead) return false;

            bool isCritical = hitType == HitType.Head || hitType == HitType.Nut;
            if (isCritical)
                damage = (byte)(damage * (crit <= 0 ? 1.5f : crit));

            GameLogger.Info($"Damaging '{gorilla.playerName}' (TargetDamage={damage}");
            bool killedNow = false;

            if (!Object.HasStateAuthority)
            {
                int estimatedHealth = CurrentHealthAmount - damage;

                if (estimatedHealth > 0)
                {
                    if ((GameServices.GetCurrentGameMode?.Invoke() ?? "") != "Horror")
                    {
                        if (!isCritical)
                            GameServices.DisplayPopupColored?.Invoke(damage.ToString(), gorilla.headTransform.position, CurrentShieldAmount < 1 ? Color.white : Color.cyan, 0.5f);
                        else
                            GameServices.DisplayPopupColored?.Invoke(damage.ToString(), gorilla.headTransform.position, CurrentShieldAmount < 1 ? Color.yellow : Color.blue, 0.6f);
                    }

                    // Notify damage direction indicator on the victim's client
                    if (attackerWorldPos.HasValue)
                        RPCDamageDirection(attackerWorldPos.Value);
                }
                else
                {
                    if (!killed)
                    {
                        killedNow = true;
                        killed = true;
                    }
                }
            }
            else
            {
                // If the victim takes damage on their own client (e.g., self damage)
                if (attackerWorldPos.HasValue && (CurrentHealthAmount - damage) > 0)
                {
                    _gorillaService ??= ServiceLocator.Get<IGorillaService>();
                    if (this.gorilla != null && this.gorilla == (_gorillaService?.LocalGorilla as Gorilla))
                    {
                        GameServices.OnDamageTaken?.Invoke(attackerWorldPos.Value);
                    }
                    else
                    {
                        RPCDamageDirection(attackerWorldPos.Value);
                    }
                }
            }

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Damage(damage, (byte)hitType, killer);
            return killedNow;
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        protected override void RPCDamage(byte damageAmount, PlayerRef explicitKiller, byte hitType)
        {
            RPCPlayDamageAnimation(hitType);
            base.RPCDamage(damageAmount, explicitKiller, hitType);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPCPlayDamageAnimation(byte hitType)
        {
            DamageAnimation((HitType)hitType);
        }

        public override void OnDeathReported(PlayerRef killer)
        {
            base.OnDeathReported(killer);
            Gorilla killerGorilla = null;
            var service = _gorillaService ?? ServiceLocator.Get<IGorillaService>();
            var gorillas = service?.Gorillas;
            if (gorillas != null)
            {
                foreach (var g in gorillas)
                {
                    var c = (Gorilla)g;
                    if (c.Object.StateAuthority == killer)
                    {
                        killerGorilla = c;
                        break;
                    }
                }
            }
            if (HasStateAuthority)
            {
                // Cache exact position BEFORE anything else (like Respawn) can move the player
                cachedDeathPos = gorilla.rootBoneTransform.position;
                cachedDeathRot = gorilla.rootBoneTransform.rotation;

                cachedDeathVelocity = LocalHardwareRig?.LocomotionController?.PlayerRigidbody?.linearVelocity ?? Vector3.zero;
            }

            if (killer == Runner.LocalPlayer && !HasStateAuthority)
            {
                GameServices.DisplayPopupColored?.Invoke("Kill", gorilla.headTransform.position, Color.red, 1f);
                _audioService.Play(killClip, gorilla.headTransform.position);
                // ChallengeType.Kill = 1
                _ = GameServices.ScoreChallengeAsync?.Invoke(1);
            }

            if (HasStateAuthority)
            {
                // Strip winner crown on death and clear the session-persisted winner state.
                if (gorilla.hasCrown)
                {
                    var authenticationService = ServiceLocator.Get<IAuthenticationService>();
                    string authPlayFabId = authenticationService?.PlayFabId;
                    string playerDataPlayFabId = PlayerData.playFabId;
                    string crownKey = !string.IsNullOrEmpty(authPlayFabId)
                        ? authPlayFabId
                        : !string.IsNullOrEmpty(playerDataPlayFabId)
                            ? playerDataPlayFabId
                            : string.Empty;

                    ServiceLocator.Get<ICrownService>()?.Revoke(crownKey);
                    gorilla.hasCrown = false;
                }

                // FadeOutAsync: fadeIn=false → alpha increases 0→1 → red overlay appears (death screen)
                _ = CameraFade.FadeOutAsync(1, 0f, Color.red);

                try
                {
                    foreach (Grabber grabber in GetComponentsInChildren<Grabber>())
                    {
                        // Skip grabbers that belong to the Backpack — items are dropped separately via BackpackDie below.
                        // Uses the IsBackpackType bridge to avoid a direct Gameplay assembly reference.
                        Grabbable parentGrabbable = grabber.GetComponentInParent<Grabbable>();
                        if (GameServices.IsBackpackType?.Invoke(parentGrabbable) ?? false)
                            continue;
                        grabber.Release();
                    }

                    // Drop all backpack items as world pickups.
                    GameServices.BackpackDie?.Invoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[GorillaHealth] Error dropping items on death: {e}");
                }

                if (AutoRespawn) WaitRespawn();
            }

            OnGorillaDied?.Invoke(this);

            if (GameServices.OnPlayerDeath != null)
            {
                // Broadcast elimination for kill feed (fires on every client)
                GameServices.OnEliminationEvent?.Invoke(
                gorilla.playerName,
                killerGorilla?.playerName ?? "Environment");

                // GorillaGameManager is in Networking assembly — Player can't reference it
                GameServices.OnPlayerDeath?.Invoke(gorilla, killerGorilla);

            }
            else if (HasStateAuthority && !AutoRespawn)
            {
                // No GameManager wired OnPlayerDeath (e.g. manager was despawned).
                // Fallback: return the dead local player to the space station after a delay.
                Debug.LogWarning("[GorillaHealth] OnPlayerDeath is NULL — using fallback return-to-station");
                _ = FallbackReturnToStation();
            }

            if (activeRagdoll)
            {
                CoinEffect coinEffect = activeRagdoll.GetComponent<CoinEffect>();
                if (coinEffect) coinEffect.gorilla = killerGorilla;
            }
        }

        private async UniTask FallbackReturnToStation()
        {
            const int fallbackReturnDelayMs = 3000;
            try
            {
                await UniTask.Delay(fallbackReturnDelayMs);

                var gameLoopService = ServiceLocator.Get<IGameLoopService>();
                if (gameLoopService?.HasManager == true)
                {
                    Debug.Log("[GorillaHealth] Fallback: using IGameLoopService.ReturnToStation");
                    gameLoopService.ReturnToStation();
                }
                else if (GameServices.JoinGameWithCode != null)
                {
                    Debug.Log("[GorillaHealth] Fallback: using JoinGameWithCode(SpaceStation)");
                    GameServices.JoinGameWithCode.Invoke(NetworkGameModeIds.SpaceStation, "");
                }
                else
                {
                    Debug.LogError("[GorillaHealth] Fallback: no return path available (OnReturnToStation and JoinGameWithCode are both null)");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GorillaHealth] FallbackReturnToStation failed: {e}");
            }
        }

        public override void OnHealthChanged()
        {
            base.OnHealthChanged();
            Refresh();

            lastHealth = TotalHealth;

            // Trigger ragdoll sync if we just died
            if (isDead && !wasDead && HasStateAuthority)
            {
                // Fallback to current if never cached (should be cached in RPCDamage/Update)
                if (cachedDeathPos == Vector3.zero)
                {
                    cachedDeathPos = gorilla.rootBoneTransform.position;
                    cachedDeathRot = gorilla.rootBoneTransform.rotation;

                    cachedDeathVelocity = LocalHardwareRig?.LocomotionController?.PlayerRigidbody?.linearVelocity ?? Vector3.zero;
                }

                RPCSyncRagdoll(cachedDeathPos, cachedDeathRot, cachedDeathVelocity);

                // Reset cache to zero so next death re-caches
                cachedDeathPos = Vector3.zero;
            }

            wasDead = isDead;
            killed = isDead;

            if (!isDead) ragdoll = false;
        }

        bool m_DamageAnimation = false;

        public async UniTaskVoid WaitRespawn()
        {
            await UniTask.Delay(5000);
            await Respawn();
        }

        public async UniTask Respawn(bool teleportedToBlimp = true)
        {
            // MapInfo-based spawn wins when present (e.g. Battle Royale scenes). Otherwise fall
            // back to SpawnPointService so modes without a MapInfo (SpaceStation, WaitingZone, or
            // a BR scene where MapInfo has already been destroyed on the final tick) still place
            // the rig at a registered AvatarXRSpawnpoint.
            var mapService = ServiceLocator.Get<IMapService>();
            var spawnPoint = mapService?.OnGetSpawnPoint?.Invoke();
            if (spawnPoint.HasValue)
                _teleportService?.TeleportToPosition(spawnPoint.Value, Quaternion.identity);
            else
                _teleportService?.TeleportToSpawnPoint();

            CurrentHealthAmount = MaxHealth;
            CurrentShieldAmount = MaxShield;
        }

        public async UniTaskVoid DamageAnimation(HitType hitType)
        {
            if (CurrentShieldAmount > 0)
                _audioService.Play(shieldHitClip, gorilla.headTransform.position);
            else if (hitType == HitType.Nut)
                _audioService.Play(nutHitClip, gorilla.headTransform.position);
            else
                _audioService.Play(hitClips, gorilla.headTransform.position);

            if (hitType == HitType.Nut && nutHitVFX != null)
                PoolObject.Get(nutHitVFX, gorilla.rootBoneTransform.position, Quaternion.identity);

            if (m_DamageAnimation) return;
            m_DamageAnimation = true;
            gorilla.material.material = CurrentShieldAmount < 1 ? 1 : 3;

            await UniTask.Delay(250);

            m_DamageAnimation = false;
            Refresh();
        }

        public void Refresh()
        {
            int targetMaterial = !isDead ? 0 : 2;
            if (gorilla.material.material != targetMaterial)
                gorilla.material.material = targetMaterial;
        }

        private void ApplyHealthPerk()
        {
            if (_perkService == null || !Object.HasStateAuthority)
            {
                return;
            }

            List<BasePerkDataObject> perks = _perkService.GetAllEquipPerks();
            MaxHealth = baseHealth;
            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i] is CharacterStateModifyPerkDataObject)
                {
                    CharacterStateModifyPerkDataObject healthPerk = (CharacterStateModifyPerkDataObject)perks[i];
                    if (healthPerk.State == CharacterState.Health)
                    {
                        MaxHealth += (byte)(baseHealth * (healthPerk.PercentBonus / 100f));
                    }
                }
            }
        }

        private void ApplyShieldPerk()
        {
            if (_perkService == null || !Object.HasStateAuthority)
            {
                return;
            }

            List<BasePerkDataObject> perks = _perkService.GetAllEquipPerks();

            byte effectiveShield = baseShield;
            CurrentShieldAmount = 0;
            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i] is CharacterStateModifyPerkDataObject)
                {
                    CharacterStateModifyPerkDataObject shieldPerk = (CharacterStateModifyPerkDataObject)perks[i];
                    if (shieldPerk.State == CharacterState.Shield)
                    {
                        effectiveShield += (byte)((float)baseShield * ((float)shieldPerk.PercentBonus / 100f));
                    }
                }
            }

            byte shieldStart = 0;
            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i] is CharacterStartShieldPerkDataObject)
                {
                    CharacterStartShieldPerkDataObject perk = (CharacterStartShieldPerkDataObject)perks[i];
                    shieldStart += (byte)((float)baseShield * ((float)perk.ShieldStartPercent / 100f));
                    break;
                }
            }

            MaxShield = effectiveShield;
            CurrentShieldAmount = shieldStart;
        }

        // ── Teabag RPCs ──────────────────────────────────────────────────────────

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        public void RPCDamageDirection(Vector3 attackerPos)
        {
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
            if (this.gorilla != null && this.gorilla == (_gorillaService?.LocalGorilla as Gorilla))
            {
                GameServices.OnDamageTaken?.Invoke(attackerPos);
            }
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        public void RPCTeabagGrab(PlayerRef deadPlayer, int index)
        {
            if (GorillaRagdoll.activeRagdolls.TryGetValue(deadPlayer, out GorillaRagdoll rag))
                rag.ShowGrab(index);
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        public void RPCTeabagPull(PlayerRef deadPlayer, int index, float progress, Vector3 handPosition)
        {
            if (GorillaRagdoll.activeRagdolls.TryGetValue(deadPlayer, out GorillaRagdoll rag))
                rag.ShowPull(index, progress, handPosition);
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        public void RPCTeabagRip(PlayerRef deadPlayer, int index, Vector3 position)
        {
            if (GorillaRagdoll.activeRagdolls.TryGetValue(deadPlayer, out GorillaRagdoll rag))
                rag.ShowRip(index, position);

            GameServices.DisplayPopupColored?.Invoke("+5% HP", position, Color.green, 1f);

            if (HasStateAuthority)
                GameServices.IncrementTeabagRipCount?.Invoke();
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
        public void RPCTeabagCancel(PlayerRef deadPlayer, int index)
        {
            if (GorillaRagdoll.activeRagdolls.TryGetValue(deadPlayer, out GorillaRagdoll rag))
                rag.ResetVisual(index);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPCSyncRagdoll(Vector3 pos, Quaternion rot, Vector3 velocity)
        {
            if (ragdoll) return;
            ragdoll = true;
            activeRagdoll = gorilla.SpawnRagdoll(pos, rot, velocity);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPCSyncFinalRagdollPosition(PlayerRef deadPlayer, Vector3 finalPos, Quaternion finalRot)
        {
            if (GorillaRagdoll.activeRagdolls.TryGetValue(deadPlayer, out GorillaRagdoll rag))
            {
                rag.ApplyFinalSync(finalPos, finalRot);
            }
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPCSyncFinalRagdollBones(PlayerRef deadPlayer, Vector3[] positions, Quaternion[] rotations, int[] indices)
        {
            if (GorillaRagdoll.activeRagdolls.TryGetValue(deadPlayer, out GorillaRagdoll rag))
            {
                rag.ApplyFinalBoneSync(positions, rotations, indices);
            }
        }

        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        public void RPCTeabagBonus()
        {
            MaxHealth += 5;
            teabagBonusCount++;
            Heal(5, false);
        }
    }
}
