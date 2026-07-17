using Fusion;
using System;
using Teabag.Core;
using UnityEngine;

namespace Teabag.Player
{
    [RequireComponent(typeof(VelocityTracker))]
    public class GorillaHand : NetworkBehaviour
    {
        const float _grabReleaseRange = 0.2f;

        public bool isLeftHand;
        public MonoBehaviour finger; // FingerPresser (Assembly-CSharp)

        [NonSerialized] public VelocityTracker tracker;
        [NonSerialized] public Grabber grabber;

        public Gorilla gorilla;
        public bool isGrabbed;
        public bool isHoldingToggleGrab;

        private Grabber lastGrabber;
        private bool wasGripHeld;
        private Grabber[] cachedGrabbers;
        private ITwoHandGrabbable currentTwoGrabbable;
        private void Awake()
        {
            finger.gameObject.SetActive(false);
            tracker = GetComponent<VelocityTracker>();
            grabber = GetComponent<Grabber>();
            gorilla = GetComponentInParent<Gorilla>();
            cachedGrabbers = transform.root.GetComponentsInChildren<Grabber>();
        }

        public override void Spawned()
        {
            base.Spawned();
            finger.gameObject.SetActive(Object.HasStateAuthority);
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            bool isGripHeld = VRInputHandler.GetInputDown(isLeftHand, InputType.Grip);
            bool isGripPressed = isGripHeld && !wasGripHeld;

            if (isGripPressed)
            {
                if (!isGrabbed)
                {
                    Grab();
                    if (currentTwoGrabbable != null && currentTwoGrabbable.SecondaryGrabber == grabber)
                    {
                        var g = currentTwoGrabbable as Grabbable;
                        isHoldingToggleGrab = g != null && g.toggleGrab;
                    }
                    else
                    {
                        isHoldingToggleGrab = grabber.grabbable != null && grabber.grabbable.toggleGrab;
                    }
                }
                else if (isHoldingToggleGrab)
                {
                    Release();
                    isGrabbed = false;
                    isHoldingToggleGrab = false;
                }
            }
            else if (!isGripHeld)
            {
                if (isGrabbed && !isHoldingToggleGrab)
                {
                    Release();
                    isGrabbed = false;
                }
            }

            wasGripHeld = isGripHeld;

            if (isGrabbed && currentTwoGrabbable != null && currentTwoGrabbable.SecondaryGrabber == grabber)
            {
                if (currentTwoGrabbable.SecondaryHandPosition != null)
                {
                    float dist = Vector3.Distance(transform.position, currentTwoGrabbable.SecondaryHandPosition.position);
                    
                    if (dist > _grabReleaseRange)
                    {
                        Release();
                        isGrabbed = false;
                        isHoldingToggleGrab = false;
                    }
                }
            }

            if (grabber.grabbable != null)
            {
                Grabber h = ClosestHolster(grabber.grabbable, true);

                if (h != lastGrabber)
                {
                    if (h != null)
                        h.Preview(grabber.grabbable);

                    if (lastGrabber != null)
                        lastGrabber.StopPreview();

                    lastGrabber = h;
                }
            }
            else if (lastGrabber != null)
            {
                lastGrabber.StopPreview();
                lastGrabber = null;
            }
        }

        public void Grab()
        {
            if (grabber.grabbable != null)
            {
                isGrabbed = true;
                return;
            }

            Grabbable royaleObject = ClosestGrabbable();

            if(royaleObject == null) // No grabbable found
            {
                grabber.Grab(null);
                return;
            }

            currentTwoGrabbable = royaleObject as ITwoHandGrabbable; // check if the grabbable is two hand grabbable
            if (currentTwoGrabbable == null || !currentTwoGrabbable.IsAbleUseTwoHand || royaleObject.grabber == null) // Handle One Hand Grab
            {
                grabber.Grab(royaleObject);
                isGrabbed = true;
                return;
            }

            currentTwoGrabbable.SetSecondaryGrabber(grabber);
            isGrabbed = true;
        }

        public void Release()
        {
            if (currentTwoGrabbable != null && currentTwoGrabbable.SecondaryGrabber == grabber)
            {
                currentTwoGrabbable.SetSecondaryGrabber(null);
                currentTwoGrabbable = null;
                return;
            }

            if (grabber.grabbable == null)
                return;

            Grabbable obj = grabber.grabbable;
            if(currentTwoGrabbable != null)
            {
                if (obj.grabber == grabber)
                {
                    if (currentTwoGrabbable.SecondaryGrabber != null)
                    {
                        GorillaHand secHand = currentTwoGrabbable.SecondaryGrabber.GetComponent<GorillaHand>();
                        if (secHand != null)
                        {
                            secHand.isGrabbed = false;
                            secHand.isHoldingToggleGrab = false;
                            secHand.currentTwoGrabbable = null;
                        }
                        currentTwoGrabbable.SetSecondaryGrabber(null);
                    }
                }
                currentTwoGrabbable = null;
            }

            grabber.Release();

            Grabber h = ClosestHolster(obj);
            if (h != null)
            {
                if (Vector3.Distance(obj.transform.position, h.GrabPoint.position) < 0.15f)
                {
                    h.Grab(obj);
                }
            }
        }

        public Grabbable ClosestGrabbable()
        {
            Grabbable closest = null;
            float distance = 0;
            foreach (Grabbable royaleObject in Grabbable.allGrabbables)
            {
                ITwoHandGrabbable _twoGrabbable = royaleObject as ITwoHandGrabbable;
                if(_twoGrabbable == null || !_twoGrabbable.IsAbleUseTwoHand) // Handle One Hand Grab
                {
                    if (royaleObject.CanGrab(grabber))
                    {
                        float td = Vector3.Distance(transform.position, royaleObject.transform.position);
                        if (distance == 0 || td < distance)
                        {
                            closest = royaleObject;
                            distance = td;
                        }
                    }
                    continue;
                }

                // Handle Two Hand Grab
                if (_twoGrabbable.IsTwoHandMode)
                {
                    continue;
                }

                if (_twoGrabbable != null)
                {
                    if (royaleObject.CanGrab(grabber))
                    {
                        float td = Vector3.Distance(transform.position, royaleObject.transform.position);
                        if (distance == 0 || td < distance)
                        {
                            closest = royaleObject;
                            distance = td;
                        }
                    }
                    else
                    {
                        float td = Vector3.Distance(transform.position, _twoGrabbable.SecondaryHandPosition.position);
                        if (td <= royaleObject.grabRange)
                        {
                            if (distance == 0 || td < distance)
                            {
                                closest = royaleObject;
                                distance = td;
                            }
                        }
                    }
                }
            }

            return closest;
        }

        public Grabber ClosestHolster(Grabbable requirement, bool preview = false)
        {
            Grabber hol = null;
            float distance = 0;
            foreach (Grabber h in cachedGrabbers)
            {
                bool b = false;
                if (!preview)
                    b = requirement.CanGrab(h);
                else
                    b = requirement.CanHolsterPreview(h);

                if (b)
                    b = h.CanGrab(requirement);

                bool d = Vector3.Distance(requirement.transform.position, h.GrabPoint.position) < 0.15f;

                if (h.hand == null && b && d)
                {
                    float td = Vector3.Distance(requirement.transform.position, h.GrabPoint.position);
                    if (distance == 0 || td < distance)
                    {
                        hol = h;
                        distance = td;
                    }
                }
            }
            return hol;
        }
    }
}
