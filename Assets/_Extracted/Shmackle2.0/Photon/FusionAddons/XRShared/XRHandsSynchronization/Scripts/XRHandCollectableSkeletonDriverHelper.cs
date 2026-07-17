using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;

namespace Fusion.Addons.XRHandsSync
{
    public static class XRHandCollectableSkeletonDriverHelper
    {
        public static XRHandCollectableSkeletonDriver SetupXRHandsboneCollector(GameObject handGameObject, bool leftHand)
        {
            return SetupXRHandsboneCollector(handGameObject, leftHand ? Handedness.Left : Handedness.Right);
        }

        public static XRHandCollectableSkeletonDriver SetupXRHandsboneCollector(GameObject handGameObject, Handedness handedness)
        {
            XRHandCollectableSkeletonDriver collectableSkeletonDriver = null;

            var xrHandTrackingsEvents = new List<XRHandTrackingEvents>(handGameObject.GetComponentsInChildren<XRHandTrackingEvents>(true));
            if (xrHandTrackingsEvents.Count == 0)
            {
                var trackingEvents = handGameObject.AddComponent<XRHandTrackingEvents>();
                trackingEvents.handedness = handedness;
                xrHandTrackingsEvents.Add(trackingEvents);
            }

            foreach (var handTrackingEvent in handGameObject.GetComponentsInChildren<XRHandTrackingEvents>(true))
            {
                var skeletonDriver = handTrackingEvent.gameObject.GetComponent<XRHandCollectableSkeletonDriver>();
                if (skeletonDriver == null)
                {
                    skeletonDriver = handTrackingEvent.gameObject.AddComponent<XRHandCollectableSkeletonDriver>();
                    var handRenderer = handGameObject.GetComponentInChildren<SkinnedMeshRenderer>();

                    if (handRenderer != null)
                    {
                        var root = handRenderer.rootBone;
                        // Initialization sequence source: HandVisualizer from XRHands HandVisualizer sample
                        skeletonDriver.jointTransformReferences = new List<JointToTransformReference>();
                        skeletonDriver.rootTransform = root;
                        XRHandSkeletonDriverUtility.FindJointsFromRoot(skeletonDriver);
                        skeletonDriver.InitializeFromSerializedReferences();
                        skeletonDriver.handTrackingEvents = handTrackingEvent;

                        skeletonDriver.applyWristPoseToHandRoot = false;

                        collectableSkeletonDriver = skeletonDriver;
                    }
                    else
                    {
                        // Automatic setup failed: warn that a setup is required
                        Debug.LogError($"Place a XRHandCollectableSkeletonDriver on XRHandTrackingEvents game object to allow synchronization of finger tracking. Also, please:" +
                            $"- set root transform" +
                            $"- press find joints" +
                            $"- uncheck apply wist pose to hand root");
                    }
                }
                else
                {
                    collectableSkeletonDriver = skeletonDriver;
                    break;
                }
            }

            return collectableSkeletonDriver;
        }
    }
}