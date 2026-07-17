using System;
using System.Collections.Generic;
using Fusion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Networking;
using Teabag.Player;
using UnityEngine;
using IAudioService = Teabag.Core.IAudioService;
using Random = UnityEngine.Random;
namespace Teabag.Gameplay
{
    public class Firearm : Weapon, ITwoHandGrabbable
    {
        [Header("References")]
        public Transform shootPoint;
        [SerializeField] private Transform trigger;
        [SerializeField] private ParticleSystem[] muzzleFlash;
        [SerializeField] private ParticleSystem bulletTrail;
        [SerializeField] private GameObject impactFX;
        [SerializeField, Tooltip("Can ignore assign if no need")] private ParticleSystem weaponFX;

        public delegate void AmmoUpdated(int inMag, int inBackpack);
        public AmmoUpdated delegateAmmoUpdated;

        public delegate void ToggleAmmoUI(bool toggleOn);
        public ToggleAmmoUI delegateToggleAmmoUI;

        [Header("Weapon Data")]
        [SerializeField] private FirearmData weaponData;
        private LayerMask _hitLayerMask;
        private RaycastHit[] hits = new RaycastHit[5];
        //private byte rarity;

        #region SO-Backed Properties (external scripts read these)
        public string bulletTypeName => weaponData.bulletTypeName;
        public int msBetweenShots => weaponData.msBetweenShots;
        public int msReloadTime => weaponData.msReloadTime;
        public int magCapacity => weaponData.magCapacity;
        //public int damage => weaponData.damage[(int)rarity];
        public float bulletSpeed => weaponData.speed;
        public float bulletRange => weaponData.range;
        #endregion

        private int _msReloadTime
        {
            get
            {
                if ((GameServices.IsModEnabled?.Invoke("Quick Reload") ?? false))
                    return weaponData.msReloadTime / 10;

                return weaponData.msReloadTime;
            }
        }

        [Header("Mag")]
        private int lastBulletsInMag;
        [Networked, OnChangedRender(nameof(BulletsChanged))]
        public int bulletsInMag { get; set; }

        [Header("Audio")]

        public bool isReloading
        {
            get
            {
                return (DateTime.UtcNow - reloadStart).TotalMilliseconds < _msReloadTime;
            }
        }

        public DateTime reloadStart;
        public DateTime lastShot;
        public DateTime lastFireAttempt;
        public bool ready = false;
        bool hasShot;
        bool wasReloading;
        bool reloadBuffered;
        private IGorillaService _gorillaService;
        private INetworkManager _networkManager;
        private IAudioService _audioService;

        private IHardwareRig LocalHardwareRig
        {
            get
            {
                if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                    return rigInfo.HardwareRig;
                return null;
            }
        }

        private Vector3[] trailPoints = new Vector3[2];

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
                return Object.StateAuthority == _networkManager.Runner.LocalPlayer;
            }
        }

        [SerializeField] private Transform _primaryHandPosition;
        [SerializeField] private Transform _secondaryHandPosition;
        [SerializeField] private bool _isUseTwoHandGrab;

        private Grabber _lastSecondaryGrabber;
        [Networked, OnChangedRender(nameof(OnSecondaryGrab))]
        public Grabber SecondaryGrabber { get; set; }

        public void OnSecondaryGrab()
        {
            // Mirror the same pattern as Grabber.OnGrab():
            // When the secondary grabber changes on any client, update GorillaHand state.
            if (SecondaryGrabber != null)
            {
                var hand = SecondaryGrabber.GetComponent<GorillaHand>();
                if (hand != null)
                {
                    hand.isGrabbed = true;
                }
            }

            if (_lastSecondaryGrabber != null && _lastSecondaryGrabber != SecondaryGrabber)
            {
                var hand = _lastSecondaryGrabber.GetComponent<GorillaHand>();
                if (hand != null)
                {
                    hand.isGrabbed = false;
                    hand.isHoldingToggleGrab = false;
                }
            }

            _lastSecondaryGrabber = SecondaryGrabber;
        }

        public Transform PrimaryHandPosition => _primaryHandPosition;
        public Transform SecondaryHandPosition => _secondaryHandPosition;
        public bool IsAbleUseTwoHand => _isUseTwoHandGrab;
        public bool IsTwoHandMode => SecondaryGrabber != null && grabber != null;
        protected override void Awake()
        {
            base.Awake();
            _hitLayerMask = LayerMask.GetMask("VRRig", "Default", "Grabbable", "WeaponBlocker", "Plane");
            toggleGrab = true;
            lastShot = DateTime.UtcNow;
            lastFireAttempt = lastShot;
        }

        public override void SpawnedRoyale()
        {
            base.SpawnedRoyale();
            _gorillaService = ServiceLocator.Get<IGorillaService>();
            _audioService = ServiceLocator.Get<IAudioService>();
            _networkManager = ServiceLocator.Get<INetworkManager>();
            //rarity = GrabbableRarity.GetRarity(this);
            bulletsInMag = weaponData.magCapacity;
            lastBulletsInMag = weaponData.magCapacity;

            //unparent to avoid particle follow gun movement after spawn.
            //Position/Rotation is updated before Play.
            if(bulletTrail != null)
            {
                bulletTrail.transform.SetParent(null);
            }

            SetActiveWeaponEffect(true);
        }

        public override void Update()
        {
            base.Update();

            if ((DateTime.UtcNow - lastShot).TotalMilliseconds >= weaponData.msBetweenShots)
            {
                ready = true;
            }

            if (interacting)
            {
                Debug.DrawRay(shootPoint.position, shootPoint.forward);

                HandleTrigger(VRInputHandler.GetInputDownAmount(hand.isLeftHand, InputType.Trigger), hand.isLeftHand);
                if (VRInputHandler.GetInputDown(hand.isLeftHand, InputType.Secondary))
                {
                    if (!wasReloading && !isReloading)
                    {
                        reloadBuffered = true;
                        wasReloading = true;
                    }
                }
                else
                    wasReloading = false;

                GameServices.CreateScope?.Invoke(this);
            }
            else
            {

                GameServices.RemoveScope?.Invoke(this);
            }

            if (reloadBuffered && Object.HasStateAuthority)
            {
                reloadBuffered = false;

                // Only reload (and play sound) if there is ammo available in the backpack
                bool hasAmmo = Backpack.myBackpack == null
                    || Backpack.myBackpack.infiniteAmmo
                    || (GameServices.IsModEnabled?.Invoke("Infinite Ammo") ?? false)
                    || Backpack.myBackpack.GetNonGrabbable(bulletTypeName).amount > 0;

                if (!hasAmmo)
                    return;

                RPCReload();
            }
        }

        public override void UpdatePosition()
        {
            base.UpdatePosition();
            if (grabber == null)
                return;

            if (grabber.hand == null)
                return;

            if ((GameServices.IsModEnabled?.Invoke("Auto Aim") ?? false))
            {
                Gorilla closestGorilla = null;
                float closestDistance = 0;
                var gorillas = _gorillaService?.Gorillas;
                if (gorillas != null)
                {
                    for (int i = 0; i < gorillas.Count; i++)
                    {
                        var gorilla = (Gorilla)gorillas[i];
                        if (gorilla.health != null)
                        {
                            if (gorilla.health.isDead)
                                continue;
                        }

                        if (gorilla != grabber.hand.gorilla)
                        {
                            float distance = Vector3.Distance(transform.position, gorilla.headTransform.position);
                            if (closestDistance == 0 || distance < closestDistance)
                            {
                                closestGorilla = gorilla;
                                closestDistance = distance;
                            }
                        }
                    }
                }

                if (closestGorilla != null)
                    transform.LookAt(closestGorilla.headTransform.position);
            }
        }

        public override void OnGrab(Grabber holster)
        {
            base.OnGrab(holster);
            ready = false;
            lastShot = DateTime.UtcNow;
            if (holster.hand != null && holster.isMine)
            {
                if(Backpack.myBackpack != null)
                {
                    Backpack.myBackpack.DelegateAmmoAdded += OnNonGrabbableAdded;
                }
                InvokeToggleAmmoUI(true);
                InvokeAmmoUpdated();
            }
        }

        public override void OnRelease(Grabber holster)
        {
            base.OnRelease(holster);
            hasShot = false;
            wasReloading = false;

            if (holster.hand != null && holster.isMine && Backpack.myBackpack != null)
            {
                Backpack.myBackpack.DelegateAmmoAdded -= OnNonGrabbableAdded;
            }
            InvokeToggleAmmoUI(false);
        }

        public void BulletsChanged()
        {
            if (bulletsInMag < lastBulletsInMag && ((DateTime.UtcNow - lastShot).TotalMilliseconds * 1.1f) > weaponData.msBetweenShots && !Object.HasStateAuthority)
            {
                //SpawnBullet();
                lastShot = DateTime.UtcNow;
            }
        }

        private void OnDestroy()
        {
            GameServices.RemoveScope?.Invoke(this);
        }

        public void ViewTrigger(float f)
        {
            if (trigger != null)
                trigger.localEulerAngles = new Vector3(f * 45, 0, 0);
        }

        public void HandleTrigger(float f, bool isLeftHand)
        {
            ViewTrigger(f);

            if (f > 0.8f)
            {
                if (!hasShot || weaponData.auto)
                {
                    bool b = Shoot();

                    if (b)
                    {
                        // Increased controller vibrations from 0.2 to 0.25
                        VRInputHandler.VibrateController(isLeftHand, 0.25f, 0.1f);

                        Recoil(weaponData.recoil);
                    }
                }

                if (!hasShot)
                {
                    hasShot = true;
                }
            }
            else
            {
                if (hasShot)
                {
                    hasShot = false;
                }
            }
        }

        public void Recoil(float recoil)
        {
            if (!interacting)
                return;
            var rig = LocalHardwareRig;
            if (rig == null) return;
            Transform controller = hand.isLeftHand ? rig.LeftHand.HandTransform : rig.RightHand.HandTransform;

            float controllerRecoil = Mathf.Clamp(recoil, 0, 3);
            controller.localPosition += controller.localRotation * (Vector3.back / 20) * controllerRecoil;
            controller.localRotation *= Quaternion.Euler(-10 * controllerRecoil, 0, 0);

            float multiplier = 1;
            if ((GameServices.IsModEnabled?.Invoke("High Recoil") ?? false))
                multiplier = 10;

            rig.LocomotionController.PlayerRigidbody.AddForce(controller.up * 10 * recoil * multiplier);
        }

        public bool Shoot()
        {
            DateTime now = DateTime.UtcNow;
            if ((now - lastFireAttempt).TotalMilliseconds < msBetweenShots)
            {
                if (!weaponData.auto && shootPoint)
                    _audioService?.Play(weaponData.jammedClip, shootPoint.position);
                return false;
            }

            lastFireAttempt = now;

            if (!shootPoint || !weaponData.bulletPrefab)
                return false;

            if (bulletsInMag > 0)
            {
                if ((DateTime.UtcNow - lastShot).TotalMilliseconds > weaponData.msBetweenShots && (DateTime.UtcNow - reloadStart).TotalMilliseconds > _msReloadTime)
                {
                    if (GameServices.IsInBlimp?.Invoke() ?? false)
                    {
                        _audioService?.Play(weaponData.jammedClip, shootPoint.position);
                        return false;
                    }

                    RPCSpawnMuzzleVFX();
                    SpawnBullet();

                    GameServices.ScopeFire?.Invoke(this);

                    if (grabber.isMine && Object.HasStateAuthority)
                    {
                        bulletsInMag--;
                        InvokeAmmoUpdated();
                    }

                    lastShot = DateTime.UtcNow;
                    return true;
                }
                else
                {
                    if (!weaponData.auto)
                    {
                        _audioService.Play(weaponData.jammedClip, shootPoint.position);
                    }
                    return false;
                }
            }
            else if (bulletsInMag <= 0 && !isReloading)
            {
                reloadBuffered = true;
                return false;
            }

            if (!weaponData.auto)
            {

                _audioService.Play(weaponData.jammedClip, shootPoint.position);
            }
            return false;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        void RPCReload()
        {
            if (Object.HasStateAuthority)
            {
                if (Backpack.myBackpack)
                    bulletsInMag += Backpack.myBackpack.UseNonGrabbable(bulletTypeName, Mathf.Clamp(weaponData.magCapacity - bulletsInMag, 0, int.MaxValue));
                else
                    bulletsInMag = weaponData.magCapacity;

                InvokeAmmoUpdated();
            }

            reloadStart = DateTime.UtcNow;
            _audioService.Play(weaponData.reloadClip, transform.position);
        }

        public void InternalRaycast(Ray ray, float length)
        {
            Debug.DrawRay(ray.origin, ray.direction, Color.red, 2f);
            int hitCount = Physics.RaycastNonAlloc(ray, hits, length, _hitLayerMask, QueryTriggerInteraction.Collide);
            if (hitCount <= 0)
                return;

            for (int i = 0; i < hitCount; i++)
            {
                if (InternalHitOrDestroy(hits[i]))
                {
                    SpawnImpactVFX(hits[i].point);
                    break;
                }
            }
        }

        private bool InternalHitOrDestroy(RaycastHit hit)
        {
            byte dmg = weaponData.damage[(byte)GetRarity()];

            IHittable hittable = hit.transform.GetComponentInParent<IHittable>();
            if (hittable == null)
            {
                SpawnImpactVFX(hit.point);
                return false;
            }

            if (hittable is Gorilla hitGorilla)
            {
                if (GameServices.SharesTeam?.Invoke(hitGorilla) ?? false)
                {
                    return false;
                }

                if(hitGorilla == grabber.hand.gorilla)
                {
                    return false;
                }

                if (Vector3.Distance(shootPoint.position, transform.position) >= 50)
                {
                    GameServices.ScoreChallengeAsync?.Invoke(4); // 4 = HitDistance
                }
            }

            PlayerRef localPlayer = _networkManager.Runner.LocalPlayer;
            hittable.OnHit(dmg, bulletSpeed, hit, shootPoint.position, localPlayer);
            SpawnImpactVFX(hit.point);
            return true;
        }

        private void SpawnImpactVFX(Vector3 point)
        {
            if(impactFX != null)
                PoolObject.Get(impactFX, point, Quaternion.identity);
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPCSpawnTrailVFX()
        {
            if (bulletTrail != null)
            {
                bulletTrail.transform.position = shootPoint.position;
                bulletTrail.transform.rotation = shootPoint.rotation;
                bulletTrail.Play();
            }
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPCSpawnMuzzleVFX()
        {
            if (muzzleFlash == null)
                return;

            for (int i = 0; i < muzzleFlash.Length; i++)
            {
                muzzleFlash[i]?.Play();
            }
            _audioService.Play(weaponData.fireClip, shootPoint.position);
        }

        public void SpawnBullet()
        {
            if (!shootPoint || !weaponData.bulletPrefab)
                return;

            List<Vector3> vectors = GetShootingDirections();
            for (int i = 0; i < vectors.Count; i++)
            {
                Vector3 vector = vectors[i];
                Vector3 velocity = Vector3.zero;

                if (Object.HasStateAuthority)
                {
                    var rig = LocalHardwareRig;

                    if (rig != null && rig.LocomotionController.PlayerRigidbody != null)
                        velocity = rig.LocomotionController.PlayerRigidbody.linearVelocity;
                }
                Ray ray = new Ray(shootPoint.position, vector);
                InternalRaycast(ray, bulletRange);
            }
            RPCSpawnTrailVFX();
        }

        public void InvokeAmmoUpdated()
        {
            int inBackpack = 0;

            if(Backpack.myBackpack != null)
                inBackpack = Backpack.myBackpack.GetNonGrabbable(bulletTypeName).amount;

            delegateAmmoUpdated?.Invoke(bulletsInMag, inBackpack);
        }

        private void OnNonGrabbableAdded(NonGrabbableBackpackItem item)
        {
            if (item.name == weaponData.bulletTypeName)
            {
                InvokeAmmoUpdated();
            }
        }

        private void InvokeToggleAmmoUI(bool toggleOn)
        {
            delegateToggleAmmoUI?.Invoke(toggleOn);
        }

        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            if (shootPoint != null)
            {
                Gizmos.color = Color.red;
                List<Vector3> vectors = GetShootingDirections();
                foreach (Vector3 vector in vectors)
                {
                    Gizmos.DrawLine(shootPoint.position, shootPoint.position + vector);
                }
            }
        }

        public List<Vector3> GetShootingDirections()
        {
            List<Vector3> vectors = new List<Vector3>();
            if (weaponData.bulletsInShot < 2)
            {
                vectors.Add(shootPoint.forward);
                return vectors;
            }

            for (int i = 0; i < weaponData.bulletsInShot; i++)
            {
                float x = UnityEngine.Random.Range(-weaponData.bulletSpread, weaponData.bulletSpread);
                float y = UnityEngine.Random.Range(-weaponData.bulletSpread, weaponData.bulletSpread);
                float z = UnityEngine.Random.Range(-weaponData.bulletSpread, weaponData.bulletSpread);
                vectors.Add(shootPoint.forward + new Vector3(x, y, z));
            }

            return vectors;
        }

        public void SetSecondaryGrabber(Grabber grabber)
        {
            SecondaryGrabber = grabber;
        }
        
        public void SetActiveWeaponEffect(bool isActive)
        {
             if (weaponFX == null)
                return;
            if (isActive)
                weaponFX.Play();
            else
                weaponFX.Stop();
        }
}
}
