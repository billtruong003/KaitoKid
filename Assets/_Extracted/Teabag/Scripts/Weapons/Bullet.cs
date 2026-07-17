using System.Collections;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Networking;
using Teabag.Player;
using UnityEngine;

namespace Teabag.Gameplay
{
    public class Bullet : MonoBehaviour
    {
        public float damageOverTime;
        public GameObject onCreate;
        public GameObject onDestroy;
        public Transform visuals;
        public GameObject trail;
        public PlayerRef sender;
        Rigidbody rb;
        bool hasHit = false;
        Vector3 lastPosition;
        Vector3 startPosition;
        private float _damage;
        private float _crit;
        private INetworkManager _networkManager;
        private TrailRenderer[] cachedTrails;
        private TrailRenderer[] cachedTrailsOnTrail;
        private PoolObject cachedPoolObject;
        private static readonly WaitForSeconds _waitAutoReturnDelay = new WaitForSeconds(30f);
        private static readonly WaitForSeconds _waitTrailReturnDelay = new WaitForSeconds(2f);

        public bool isMine
        {
            get
            {
                if (_networkManager == null)
                {
                    _networkManager = ServiceLocator.Get<INetworkManager>();
                }

                if (_networkManager.Runner == null)
                {
                    return false;
                }

                if (_networkManager.Runner.GameMode == Fusion.GameMode.Single)
                    return true;
                return sender == _networkManager.Runner.LocalPlayer;
            }
        }

        public void Initialise(Vector3 pos, Vector3 dir, Vector3 velocity, byte d, float speed, Transform point, PlayerRef ply, bool effects = true, float crit = 0)
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
                cachedPoolObject = GetComponent<PoolObject>();
                cachedTrails = GetComponentsInChildren<TrailRenderer>();
                if (trail) cachedTrailsOnTrail = trail.GetComponentsInChildren<TrailRenderer>();
            }

            rb.position = pos;
            rb.rotation = Quaternion.LookRotation(dir);
            rb.linearVelocity = (dir * (20 * speed)) + velocity;

            _damage = d;
            _crit = crit;
            sender = ply;

            startPosition = pos;
            lastPosition = pos;

            // Clear trail renderers to prevent visual artifacts from instantiation position
            foreach (TrailRenderer trailRenderer in cachedTrails)
                trailRenderer.Clear();

            if (effects)
            {
                var flash = PoolObject.Get(onCreate, point.position, point.rotation);
                flash.transform.SetParent(point);
                flash.transform.localPosition = Vector3.zero;
            }

            StartCoroutine(IEAutoReturnCoroutine());
            InternalRaycast(new Ray(pos, dir), speed);
        }

        private IEnumerator IEAutoReturnCoroutine()
        {
            yield return _waitAutoReturnDelay;
            ReturnToPool();
        }

        public void ResetState()
        {
            hasHit = false;
            damageOverTime = 0;
            if (visuals) visuals.gameObject.SetActive(true);
            if (trail)
            {
                trail.transform.SetParent(transform);
                trail.transform.localPosition = Vector3.zero;
            }
            if (cachedTrails != null)
            {
                foreach (TrailRenderer rend in cachedTrails)
                {
                    rend.Clear();
                    rend.emitting = true;
                }
            }
        }

        private void ReturnToPool()
        {
            StopAllCoroutines();
            ResetState();
            if (cachedPoolObject != null)
            {
                cachedPoolObject.Return();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator IEReturnTrailAndBullet(Vector3 hitPoint)
        {
            hasHit = true;
            if (visuals) visuals.gameObject.SetActive(false);
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;

            if (trail)
            {
                trail.transform.parent = null;
                if (cachedTrailsOnTrail != null)
                {
                    foreach (TrailRenderer rend in cachedTrailsOnTrail)
                    {
                        rend.AddPosition(hitPoint);
                        rend.emitting = false;
                    }
                }
            }

            yield return _waitTrailReturnDelay;

            ReturnToPool();
            rb.isKinematic = false;
        }

        public void InternalRaycast(Ray ray, float length)
        {
            Debug.DrawRay(ray.origin, ray.direction, Color.red);
            if (Physics.Raycast(ray, out RaycastHit hit, length, LayerMask.GetMask("VRRig", "Default", "Grabbable", "WeaponBlocker", "Plane"), QueryTriggerInteraction.Collide))
                InternalHitOrDestroy(hit);
        }

        private void Update()
        {
            _damage += Time.deltaTime * damageOverTime;
            if (_damage > 1000) ReturnToPool();

            Ray ray = new Ray(lastPosition, (transform.position - lastPosition) * (9.8f * Time.deltaTime * Time.deltaTime));
            InternalRaycast(ray, Vector3.Distance(transform.position, lastPosition) + 0.1f);

            if (visuals && rb.linearVelocity.sqrMagnitude > 0.001f)
                visuals.rotation = Quaternion.LookRotation(rb.linearVelocity);

            lastPosition = transform.position;
        }

        private void InternalHitOrDestroy(RaycastHit hit)
        {
            if (hasHit) return;
            byte dmg = (byte)_damage;

            Transform hitRoot = hit.transform.root;

            // Cache the weapon lookup — reused at the end
            Weapon weapon = hit.transform.GetComponentInParent<Weapon>();
            if (weapon && weapon.grabber && weapon.grabber.Object.StateAuthority == sender)
                return;

            if (hitRoot.GetComponent<GorillaLocomotion.Player>())
                return;

            DummyTarget dummy = hit.transform.GetComponentInParent<DummyTarget>();
            if (dummy && isMine && !dummy.IsDead)
            {
                HitType hitType = HitType.Normal;
                if (hit.transform.CompareTag("Head")) hitType = HitType.Head;
                else if (hit.transform.CompareTag("Nut")) hitType = HitType.Nut;

                dummy.Damage(dmg, hitType, _crit);
                SpawnImpactVFX(hit.point);
                if (trail)
                    StartCoroutine(IEReturnTrailAndBullet(hit.point));
                else
                    ReturnToPool();

                hasHit = true;
                return;
            }

            Gorilla gorilla = hitRoot.GetComponent<Gorilla>();
            if (gorilla && isMine && gorilla.health && !gorilla.health.isDead && !(GameServices.SharesTeam?.Invoke(gorilla) ?? false))
            {
                HitType hitType = HitType.Normal;
                if (hit.transform.CompareTag("Head")) hitType = HitType.Head;
                else if (hit.transform.CompareTag("Nut")) hitType = HitType.Nut;

                gorilla.health.Damage(dmg, hitType, sender, startPosition, _crit);
                if (Vector3.Distance(startPosition, transform.position) >= 50)
                    GameServices.ScoreChallengeAsync?.Invoke(4); // 4 = HitDistance

                if ((GameServices.IsModEnabled?.Invoke("Fling") ?? false))
                    gorilla.RPCAddForce(((transform.position - lastPosition) / Time.deltaTime) + Vector3.up * 3);
            }

            if (hit.transform.TryGetComponent(out Target target))
                target.Hit();

            if (!gorilla && isMine && hit.transform.GetComponentInParent<Health>() is { } health)
                health.Damage(dmg);

            hit.transform.TryGetComponent(out Grenade grenade);
            if (grenade != null && isMine)
                grenade.Explode();

            // Reuse cached weapon from above instead of a second GetComponentInParent
            if (weapon != null && grenade == null)
                return;

            SpawnImpactVFX(hit.point);
            if (trail)
                StartCoroutine(IEReturnTrailAndBullet(hit.point));
            else
                ReturnToPool();

            hasHit = true;
        }

        private void SpawnImpactVFX(Vector3 point)
        {
            PoolObject.Get(onDestroy, point, Quaternion.identity);
        }
    }
}
