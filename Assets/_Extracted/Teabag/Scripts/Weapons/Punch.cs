using System;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Player;
using UnityEngine;
using Voxoun.Engine;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Gameplay
{
    public class Punch : NetworkBehaviour
    {
        [SerializeField] private PunchData _data;
        private GorillaHand hand;
        private Collider _collider;
        private DateTime _lastHit;
        private Vector3 lastPosition;
        private SwingFrameData _frame;
        private IAudioService _audioService;
        private bool IsLeftHand => hand?.isLeftHand ?? false;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            hand = GetComponent<GorillaHand>();
            _audioService = ServiceLocator.Get<IAudioService>();
            _lastHit = DateTime.UtcNow;
        }

        public override void Spawned()
        {
            base.Spawned();
            lastPosition = _collider.bounds.center;
        }

        private void Update()
        {
            if (!HasStateAuthority)
                return;
            
            if (!IsMakingFist())
                return;

            if (IsInCooldown())
                return;

            if (hand.isGrabbed)
                return;

            UpdateSwingFrame();

            if (_frame.isSwinging)
                TryHit();

            lastPosition = _collider.bounds.center;
        }

        private void UpdateSwingFrame()
        {
            Vector3 v = hand?.tracker?.velocity ?? Vector3.zero;
            v -= hand?.gorilla?.GetVelocity ?? Vector3.zero;

            float speed = v.magnitude;

            _frame.velocity = v;
            _frame.speed = speed;
            _frame.isSwinging = speed >= _data.MinSpeed;
        }

        private bool IsInCooldown()
        {
            bool inCooldown = (DateTime.UtcNow - _lastHit).TotalMilliseconds < _data.CooldownMs;
            return inCooldown;
        }

        private bool IsMakingFist()
        {
            bool isMakingFist = VRInputHandler.GetInputDown(IsLeftHand, InputType.Grip) &&
            VRInputHandler.GetInputDown(IsLeftHand, InputType.Trigger);
            return isMakingFist;
        }

        private bool IsValidTarget(IHittable hittable)
        {
            Gorilla g = hittable as Gorilla;

            if (g == null)
                return true;

            if (g == hand.gorilla)
                return false;

            if (GameServices.SharesTeam?.Invoke(g) ?? false)
                return false;

            return true;
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

        private void TryHit()
        {
            Vector3 currentPosition = _collider.bounds.center;
            Vector3 lastPos = lastPosition;
            Vector3 direction = currentPosition - lastPos;

            float distance = direction.magnitude;
            float radius = _collider.bounds.extents.x;

            if (distance <= 0.001f)
                return;

            int hitCount = Physics.SphereCastNonAlloc(
                lastPos,
                radius,
                direction.normalized,
                PhysicsBuffers.RaycastResults,
                distance,
                _data.WhatIsPunchable,
                QueryTriggerInteraction.Collide
            );

            if (hitCount > 0)
            {
                ProcessHits(hitCount);
            }
        }

        private void ApplyHit(IHittable hittable, RaycastHit hit)
        {
            var attacker = Object.StateAuthority;
            Gorilla hitGorilla = hittable as Gorilla;

            float ratio = Mathf.InverseLerp(_data.MinSpeed, _data.MaxSpeed, _frame.speed);
            float damage = Mathf.Lerp(_data.MinDamage, _data.MaxDamage, ratio);

            hittable.OnHit((byte)damage, _frame.speed, hit, hit.point, attacker);
            VRInputHandler.VibrateController(hand.isLeftHand, 0.2f, 0.1f);
            RPCPlayDamage(hit.point, attacker);

            _lastHit = DateTime.UtcNow;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPCPlayDamage(Vector3 position = default, PlayerRef sender = default)
        {
            Vector3 pos = position == default ? transform.position : position;

            float normalized = Mathf.InverseLerp(_data.MinDamage, _data.MaxDamage, _frame.speed);
            float volumeMultiplier = Mathf.Lerp(0.5f, 2f, normalized);
            float pitch = Mathf.Lerp(0.75f, 1.5f, normalized);

            _audioService.Play(_data.HitClip, _data.HitClip.Volume * volumeMultiplier, pitch, pos);

                if (_data.OnHitParticle != null)
                    PoolObject.Get(_data.OnHitParticle, pos, Quaternion.identity);
        }
        private void OnDrawGizmos()
        {
            if(_collider != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(_collider.bounds.center, _collider.bounds.extents.x);
            }
        }
    }
}
