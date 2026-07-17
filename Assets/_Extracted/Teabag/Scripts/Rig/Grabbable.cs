using Fusion;
using System;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Core;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Player
{
    public class Grabbable : RoyaleNetworkBehaviour, IInterestEnter, IInterestExit
    {
        public static readonly List<Grabbable> allGrabbables = new List<Grabbable>();
        public static Action<Grabbable> onGrabbableSpawned;

        private IGorillaService _gorillaService;
        private Grabber _grabber;
        public Grabber grabber
        {
            get => _grabber;
            set // TODO Refactor... A setter shouldn't do that much things... it is suppose to set a value, not change the game state
            {
                //Debug.Log($"{gameObject.name} grabber = {value}");
                grabInterpolation = 0;
                StoreVariables();

                var audioService = ServiceLocator.Get<IAudioService>();

                if (!value && _grabber) // Release
                {
                    if (rigidbody)
                    {
                        rigidbody.kinematic = false;
                        if (_grabber.hand)
                        {
                            rigidbody.Rigidbody.linearVelocity = _grabber.hand.tracker.velocity;
                            rigidbody.Rigidbody.angularVelocity = _grabber.hand.tracker.angularVelocity;
                        }
                    }

                    //if (rarity)
                    //    RarityAssignment.AssignRarity(transform, rarity.rarity);

                    audioService.Play(pickUp, transform.position);
                    try
                    {
                        OnRelease(_grabber);
                        foreach (Renderer renderer in renderers)
                            renderer.enabled = true;
                        lastInteractTime = DateTime.UtcNow;
                    }
                    catch (Exception e) { /* ignored */ }
                }
                else if (value && value != _grabber) // Pickup
                {
                    if (_grabber)
                    {
                        if (!_grabber.hand)
                            audioService.Play(pickUp, transform.position);

                        if (_grabber.HasStateAuthority)
                            _grabber.grabbable = null;
                    }
                    else
                        audioService.Play(pickUp, transform.position);

                    //if (rarity)
                    //    RarityAssignment.DestroyRarity(transform);

                    if (value.isMine && !Object.HasStateAuthority && takeStateOnGrab)
                        RequestStateAuthority();

                    try
                    {
                        lastInteractTime = DateTime.UtcNow;
                        OnGrab(value);
                    }
                    catch (Exception e) { /* ignored */ }
                }

                _grabber = value;
            }
        }
        public GorillaHand hand
        {
            get
            {
                if (!grabber)
                    return null;

                return grabber.hand;
            }
        }
        [NonSerialized] public Vector3 defaultScale = Vector3.one;
        [Range(0.05f, 1)]
        public float grabRange = 0.5f;
        public AdvancedAudioClip pickUp;

        [NonSerialized] public List<Renderer> renderers = new List<Renderer>();
        [NonSerialized] public List<Collider> colliders = new List<Collider>();
        [NonSerialized] public new RoyaleNetworkRigidbody rigidbody;
        [NonSerialized] public bool canGrab = true;
        public bool takeStateOnGrab = true;
        [NonSerialized] public bool locked = false;
        [NonSerialized] public DateTime lastInteractTime;
        public bool cleanup = true;
        public bool toggleGrab = false;
        //new Rigidbody rigidbody;

        [Header("Points"), Tooltip("The point at which the object anchors itself when it is not held")]
        public Transform anchor;
        public Transform leftGrabPoint;
        public Transform rightGrabPoint;
        public bool invertGrabPoints = true;
        //bool invertGrabPoints = false;

        [Header("Other")]
        GrabbableRarity rarity;
        public Rarity GetRarity() 
        {
            if (rarity != null)
                return rarity.rarity;

            else 
                return Rarity.Common; 
        }

        [Header("Anti Jitter")]
        [SerializeField] private float positionMinError = 0.001f;
        [SerializeField] private float positionMaxError = 0.1f;
        [SerializeField] private float positionSmoothSpeed = 30f;
        [SerializeField] private float rotationMinError = 0.01f;
        [SerializeField] private float rotationMaxError = 10;
        [SerializeField] private float rotationSmoothSpeed = 25;

        private Vector3 smoothedPosition;
        private Quaternion smoothedRotation;
        private bool hasInitSmoothing = false;

        float grabInterpolation;
        Vector3 grabPosition;
        Quaternion grabRotation;
        Vector3 grabScale;

        public bool interacting
        {
            get
            {
                if (!grabber) return false;
                return hand && grabber.isMine;
            }
        }

        public bool otherInteracting => !grabber ? false : hand;
        public bool holstered => grabber && !hand;

        protected virtual void Awake()
        {
            rarity = GetComponent<GrabbableRarity>();
            rigidbody = GetComponent<RoyaleNetworkRigidbody>();

            lastInteractTime = DateTime.UtcNow;
            defaultScale = transform.localScale;
            grabInterpolation = 1;
            StoreVariables();

            foreach (Renderer childRenderer in GetComponentsInChildren<Renderer>())
            {
                if (childRenderer.enabled)
                    renderers.Add(childRenderer);
            }

            foreach (Collider childCollider in GetComponentsInChildren<Collider>())
            {
                if (childCollider.enabled)
                    colliders.Add(childCollider);
            }
        }

        private void OnEnable() => allGrabbables.Add(this);
        private void OnDisable() => allGrabbables.Remove(this);

        public override void Spawned()
        {
            base.Spawned();
            try
            {
                onGrabbableSpawned?.Invoke(this);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to call onGrabbableSpawned: " + e);
            }
        }

        public void StoreVariables()
        {
            grabPosition = transform.position;
            grabRotation = transform.rotation;
            grabScale = transform.localScale;
        }

        public virtual bool CanGrab(Grabber holster)
        {
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
            var localGorilla = _gorillaService?.LocalGorilla as Gorilla;
            if (localGorilla != null)
            {
                if (localGorilla.health != null)
                {
                    if (localGorilla.health.isDead)
                        return false;
                }
            }

            if (!canGrab) return false;
            if (Vector3.Distance(transform.position, holster.transform.position) > grabRange) return false;
            if (grabber == null) return true;
            return grabber.isMine && grabber.hand == null;
        }

        public virtual bool CanHolsterPreview(Grabber holster)
        {
            if (!canGrab)
                return false;

            if (Vector3.Distance(transform.position, holster.transform.position) > grabRange)
                return false;

            if (grabber != null)
            {
                if (!grabber.isMine)
                    return false;
                //return true;
            }

            return true;
        }

        public virtual void OnGrab(Grabber holster)
        {
        }

        public virtual void OnRelease(Grabber holster)
        {

        }

        public virtual void Update()
        {
            if (!Object || !Runner) return;
            grabInterpolation += Time.deltaTime * 10;
            grabInterpolation = Mathf.Clamp01(grabInterpolation);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (Object.StateAuthority == null && Runner.IsSharedModeMasterClient)
                // ReSharper disable once HeuristicUnreachableCode
                RequestStateAuthority();

            if (Object.HasStateAuthority)
            {
                _gorillaService ??= ServiceLocator.Get<IGorillaService>();
                var grabLocal = _gorillaService?.LocalGorilla as Gorilla;
                if (grabLocal && grabLocal.health && grabLocal.health.isDead)
                {
                    if (grabber)
                        grabber.Release();
                }

                if (transform.position.y < -100 && cleanup)
                {
                    Runner.Despawn(Object);
                    return;
                }

                if (!grabber && (DateTime.UtcNow - lastInteractTime).Minutes > 2 && cleanup)
                    Runner.Despawn(Object);
            }
        }

        public override void Render()
        {
            base.Render();
            UpdatePosition();
        }

        public virtual void UpdatePosition()
        {
            if (rigidbody)
                rigidbody.transmitting = true;

            if (grabber)
            {
                Vector3 targetPosition = grabber.GrabPoint.position;
                Quaternion targetRotation = grabber.GrabPoint.rotation;
                Vector3 targetScale = defaultScale;

                if (!grabber.hand)
                {
                    targetScale = defaultScale * 0.5f;
                }
                else if (leftGrabPoint && rightGrabPoint)
                {
                    Transform grabPoint = grabber.hand.isLeftHand ? leftGrabPoint : rightGrabPoint;

                    if (invertGrabPoints)
                    {
                        targetPosition = grabber.GrabPoint.TransformPoint(-grabPoint.localPosition * transform.localScale.magnitude);
                    }
                    else
                    {
                        targetPosition = grabber.GrabPoint.TransformPoint(grabPoint.localPosition * transform.localScale.magnitude);
                    }

                    targetRotation = targetRotation * grabPoint.localRotation;
                }

                #region Handle second hand grab
                ITwoHandGrabbable twoHand = this as ITwoHandGrabbable;
                if (twoHand != null && twoHand.SecondaryGrabber != null && twoHand.SecondaryGrabber.hand != null)
                {
                    Vector3 primaryPos = grabber.GrabPoint.position;
                    Vector3 secondaryPos = twoHand.SecondaryGrabber.GrabPoint.position;
                    Vector3 forwardDir = (secondaryPos - primaryPos).normalized;

                    if (forwardDir.sqrMagnitude > 0)
                    {
                        Vector3 secondaryLocal = Vector3.forward;
                        Vector3 primaryLocal = Vector3.zero;

                        if (twoHand.SecondaryHandPosition != null)
                            secondaryLocal = transform.InverseTransformPoint(twoHand.SecondaryHandPosition.position);

                        if (twoHand.PrimaryHandPosition != null)
                            primaryLocal = transform.InverseTransformPoint(twoHand.PrimaryHandPosition.position);

                        Vector3 localDir = (secondaryLocal - primaryLocal).normalized;

                        if (localDir.sqrMagnitude > 0)
                        {
                            Vector3 expectedWorldDir = targetRotation * localDir;
                            Quaternion alignmentRot = Quaternion.FromToRotation(expectedWorldDir, forwardDir);

                            targetPosition = grabber.GrabPoint.position + alignmentRot * (targetPosition - grabber.GrabPoint.position);
                            targetRotation = alignmentRot * targetRotation;
                        }
                    }
                }
                #endregion

                // ---------- ADAPTIVE SMOOTH START ----------

                if (!hasInitSmoothing)
                {
                    smoothedPosition = targetPosition;
                    smoothedRotation = targetRotation;
                    hasInitSmoothing = true;
                }

                // --- POSITION ---
                float distance = Vector3.Distance(smoothedPosition, targetPosition);

                float posFactor = AdaptiveSmooth(distance, positionMinError, positionMaxError);

                float tPos = 1f - Mathf.Exp(-(positionSmoothSpeed * posFactor) * Time.deltaTime);

                smoothedPosition = Vector3.Lerp(smoothedPosition, targetPosition, tPos);

                // --- ROTATION ---
                float angle = Quaternion.Angle(smoothedRotation, targetRotation);

                float rotFactor = AdaptiveSmooth(angle, rotationMinError, rotationMaxError);

                float tRot = 1f - Mathf.Exp(-(rotationSmoothSpeed * rotFactor) * Time.deltaTime);

                smoothedRotation = Quaternion.Slerp(smoothedRotation, targetRotation, tRot);

                // APPLY
                transform.position = smoothedPosition;
                transform.rotation = smoothedRotation;
                transform.localScale = targetScale;

                // ---------- ADAPTIVE SMOOTH END ----------

                TransformLock transformLock = GetComponent<TransformLock>();
                if (transformLock) transformLock.Limits();

                if (rigidbody)
                {
                    rigidbody.kinematic = true;
                    rigidbody.transmitting = false;
                }
            }
            else
            {
                hasInitSmoothing = false; 

                if (anchor)
                {
                    transform.position = Vector3.Slerp(grabPosition, anchor.position, grabInterpolation);
                    transform.rotation = Quaternion.Slerp(grabRotation, anchor.rotation, grabInterpolation);
                    transform.localScale = Vector3.Slerp(grabScale, anchor.localScale, grabInterpolation);
                }
                else
                {
                    transform.localScale = Vector3.Slerp(grabScale, defaultScale, grabInterpolation);
                }
            }
        }

        float AdaptiveSmooth(float error, float minError, float maxError)
        {
            float normalized = Mathf.InverseLerp(minError, maxError, error);

            return Mathf.SmoothStep(0f, 1f, normalized);
        }

        public virtual void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, grabRange / 2);
        }

        public Vector3 RotateAroundPivot(Vector3 pivot, Vector3 point, Quaternion rotation)
        {
            Vector3 difference = pivot - point;
            Vector3 dir = rotation * difference;
            return dir + pivot;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (HasStateAuthority)
            {
                Runner.AddPlayerAreaOfInterest(Runner.LocalPlayer, transform.position, 32);
            }
        }

        public void InterestEnter(PlayerRef player)
        {
            if (Runner.LocalPlayer != player)
                return;

            rigidbody.kinematic = true;
        }

        public void InterestExit(PlayerRef player)
        {
            if (Runner.LocalPlayer != player)
                return;

            rigidbody.kinematic = false;
        }

        public bool HitsComponent(Component component, Vector3 componentPosition, float maxDistance = 15)
        {
            Vector3 direction = componentPosition - transform.position;
            float distance = Mathf.Min(direction.magnitude, maxDistance);
            direction.Normalize();

            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, LayerMask.GetMask("Default", "Grabbable", "VRRig"), QueryTriggerInteraction.Collide))
            {
                Transform current = hit.collider.transform;
                while (current != null)
                {
                    if (current == component.transform)
                        return true;
                    current = current.parent;
                }
            }

            return false;
        }

        public static bool HitsComponent(Component component, Vector3 position, Vector3 componentPosition, float maxDistance = 15f)
        {
            Vector3 direction = componentPosition - position;
            float distance = Mathf.Min(direction.magnitude, maxDistance);
            direction.Normalize();

            if (Physics.Raycast(position, direction, out RaycastHit hit, distance, LayerMask.GetMask("Default", "Grabbable", "VRRig"), QueryTriggerInteraction.Collide))
            {
                Transform current = hit.collider.transform;
                while (current)
                {
                    if (current == component.transform)
                        return true;
                    current = current.parent;
                }
            }

            return false;
        }

        public static byte CalculateDamage(Vector3 a, Vector3 b, float maxDistance = 15)
        {
            float distance = Vector3.Distance(a, b);
            distance = maxDistance - Mathf.Clamp(distance, 0, maxDistance);
            byte damage = (byte)(distance * 10);
            return damage;
        }
    }
}
