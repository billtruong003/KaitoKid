using System;
using System.Collections.Generic;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Player;
using UnityEngine;
using Voxoun.Engine;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Gameplay
{
    public struct SwingFrameData
    {
        public Vector3 velocity;
        public float speed;
        public bool isSwinging;
    }

    public class Melee : Weapon
    {
        public static float MIN_HIT_SPEED = 2f;
        public static float MAX_HIT_SPEED = 5f;
        private SwingFrameData _frame;

        [Header("Weapon Data")] [SerializeField]
        private MeleeData weaponData;

        [Header("References")] public Transform tip;
        Quaternion lastRotation;
        Vector3 lastPosition;
        bool hasHit = false;
        bool isStriking = false;
        DateTime lastHit;
        DateTime strikingStart;

        private IAudioService _audioService;
        GrabbableRarity rarityVisual;
        private int rarity => GrabbableRarity.GetRarity(this);
        private LayerMask _hitLayerMask;
        private byte _finalDamage;
        private float _finalCrit;
        private IPerkService _perkService;


        protected override void Awake()
        {
            base.Awake();
            lastHit = DateTime.UtcNow;

            _audioService = ServiceLocator.Get<IAudioService>();
            rarityVisual = GetComponent<GrabbableRarity>();
            _perkService = ServiceLocator.Get<IPerkService>();
            _hitLayerMask = LayerMask.GetMask("VRRig", "Default", "Grabbable", "WeaponBlocker", "Plane");
        }

        public override void Spawned()
        {
            base.Spawned();
            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }

        public override void Update()
        {
            base.Update();

            if (!CanProcessWeapon())
                return;

            if (IsInCooldown())
            {
                UpdateTransformHistory();
                return;
            }

            UpdateSwingFrame();

            if (_frame.isSwinging)
            {
                TryHit();
            }

            UpdateTransformHistory();
        }

        private bool CanProcessWeapon()
        {
            if (!weaponData || !grabber || !interacting)
            {
                hasHit = false;
                isStriking = false;
                return false;
            }

            return true;
        }

        private bool IsInCooldown()
        {
            if (!hasHit)
                return false;

            float cooldown = weaponData?.cooldownMs[rarity] ?? 1000;

            if ((DateTime.UtcNow - lastHit).TotalMilliseconds < cooldown)
                return true;

            hasHit = false;
            return false;
        }

        private void UpdateTransformHistory()
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }

        private void TryHit()
        {
            Vector3 currentPosition = transform.position;
            Quaternion currentRotation = transform.rotation;

            Vector3 lastPos = lastPosition;
            Quaternion lastRot = lastRotation;

            Vector3 direction = currentPosition - lastPos;
            float distance = direction.magnitude;

            if (distance <= 0.001f)
                return;

            Vector3 centerOffset = weaponData.checkBoxCenterOffset;
            Vector3 halfExtents = weaponData.checkBoxExtents * 0.5f;

            Vector3 worldCenter = lastPos + lastRot * centerOffset;
            Quaternion midRotation = Quaternion.Slerp(lastRot, currentRotation, 0.5f);

            int hitCount = Physics.BoxCastNonAlloc(
                worldCenter,
                halfExtents,
                direction.normalized,
                PhysicsBuffers.RaycastResults,
                midRotation,
                distance,
                _hitLayerMask,
                QueryTriggerInteraction.Collide
            );

            if (hitCount > 0)
                ProcessHits(hitCount);
        }
        private void ProcessHits(int hitCount)
        {
            for (int i = 0; i < hitCount; i++)
            {
                var hit = PhysicsBuffers.RaycastResults[i];

                if (hit.transform == transform)
                    continue;

                IHittable hittable = hit.transform.GetComponentInParent<IHittable>();
                if (hittable == null)
                    continue;

                if (!IsValidTarget(hittable))
                    continue;

                ApplyHit(hittable, hit);
                return;
            }
        }

        private bool IsValidTarget(IHittable hittable)
        {
            Gorilla g = hittable as Gorilla;

            if (g == null)
                return true;

            if (g == grabber.hand.gorilla)
                return false;

            if (GameServices.SharesTeam?.Invoke(g) ?? false)
                return false;

            return true;
        }
        private void ApplyHit(IHittable hittable, RaycastHit hit)
        {
            var attacker = grabber ? grabber.Object.StateAuthority : default;
            Gorilla hitGorilla = hittable as Gorilla;

            float speed = Mathf.Clamp(_frame.speed, MIN_HIT_SPEED, MAX_HIT_SPEED);

            float damageMultiplier =
                Mathf.InverseLerp(speed, MIN_HIT_SPEED, MAX_HIT_SPEED) * 1.5f + 0.5f;

            float damage = _finalDamage * damageMultiplier;

            Vector3 velocity = _frame.velocity * (weaponData?.hitVelocityMultiplier[rarity] ?? 1f);

            hittable.OnHit((byte)damage, speed, hit, hit.point, attacker);

            hitGorilla?.RPCAddForce(velocity);
            RPCPlayDamage(true, hit.point, attacker);

            lastHit = DateTime.UtcNow;
            hasHit = true;
            isStriking = false;
        }

        private void UpdateSwingFrame()
        {
            Vector3 v = grabber?.hand?.tracker?.velocity ?? Vector3.zero;
            v -= grabber?.hand?.gorilla?.GetVelocity ?? Vector3.zero;

            float speed = v.magnitude;

            _frame.velocity = v;
            _frame.speed = speed;
            _frame.isSwinging = speed >= MIN_HIT_SPEED;
        }

        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!weaponData)
                return;

            Gizmos.color = Color.red;

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                Vector3.one
            );

            Gizmos.matrix = rotationMatrix;

            Gizmos.DrawWireCube(
                weaponData.checkBoxCenterOffset,
                weaponData.checkBoxExtents
            );

            Gizmos.matrix = Matrix4x4.identity;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPCPlayDamage(bool hit, Vector3 position = default, PlayerRef sender = default)
        {
            Vector3 pos = position == default? transform.position : position;
            if (sender == Runner.LocalPlayer)
                VRInputHandler.VibrateController(hand.isLeftHand, 0.2f, 0.1f);

            float normalized = Mathf.InverseLerp(MIN_HIT_SPEED, MAX_HIT_SPEED, _frame.speed);
            float volumeMultiplier = Mathf.Lerp(0.5f, 2f, normalized);
            float pitch = Mathf.Lerp(0.75f, 1.5f, normalized);

            if(hit)
            {
                _audioService.Play(weaponData.hitClip, weaponData.hitClip.Volume * volumeMultiplier, pitch, pos);

                if(weaponData.onHitParticle != null)
                    PoolObject.Get(weaponData.onHitParticle, pos, Quaternion.identity);
            }
            else
                _audioService.Play(weaponData.failedHitClip, weaponData.failedHitClip.Volume * volumeMultiplier, pitch, pos);
        }

        public override void OnGrab(Grabber holster)
        {
            base.OnGrab(holster);
            ApplyPerk();
        }

        public void ApplyPerk()
        {
            byte baseDamage = weaponData.damage[GrabbableRarity.GetRarity(this)];
            _finalDamage = baseDamage;
            _finalCrit = 1.5f;
            List<BasePerkDataObject> perks = _perkService.GetAllEquipPerks();

            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i] is CharacterStateModifyPerkDataObject)
                {
                    CharacterStateModifyPerkDataObject perk = (CharacterStateModifyPerkDataObject)perks[i];
                    switch (perk.State)
                    {
                        case CharacterState.Damage:
                            {
                                _finalDamage = (byte)Mathf.Clamp(_finalDamage + (baseDamage * (perk.PercentBonus / 100f)), 0, 255);
                                break;
                            }
                        case CharacterState.Crit_Damage:
                            {
                                _finalCrit = Mathf.Max(_finalCrit + (perk.PercentBonus / 100f), 0);
                                break;
                            }
                    }
                }
            }
        }
    }
}
