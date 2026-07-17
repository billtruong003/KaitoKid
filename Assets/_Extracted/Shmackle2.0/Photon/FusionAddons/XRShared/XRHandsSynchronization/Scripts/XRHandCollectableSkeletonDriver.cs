
using Fusion.Addons.HandsSync;
using Fusion.Addons.XRHandsSync;

using Fusion.XR.Shared;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.XR.Hands;
using Fusion.XR.Shared.Core;

namespace Fusion.Addons.XRHandsSync
{
    /*
     * Collect XRHands bones data, and provide them through the IBonesCollecter interface,
     *  that is then accessed by the local user network rig to store finger pose data in NetworkBonesStateSync, to make them available to remote users
     */
    public class XRHandCollectableSkeletonDriver : XRHandSkeletonDriver, IBonesCollecter
    {
        public enum ControllerTrackingMode
        {
            AlwaysAvailable,
            NeverAvailable,
            UseInputAction
        }
        [Header("Controller tracking fallback")]
        public ControllerTrackingMode controllerTrackingMode = XRHandCollectableSkeletonDriver.ControllerTrackingMode.UseInputAction;
        [DrawIf(nameof(controllerTrackingMode), (long)XRHandCollectableSkeletonDriver.ControllerTrackingMode.UseInputAction, CompareOperator.Equal, Hide = true)]
#if ENABLE_INPUT_SYSTEM
        public InputActionProperty controllerAvailableAction = new InputActionProperty(new InputAction());
#endif

        [Header("Hardware hand relativepositioning")]
        public Transform handRoot;
        [Tooltip("Apply wrist pose to a parent object instead of the wrist bone")]
        public bool applyWristPoseToHandRoot = true;
        [Tooltip("Force IBonesCollecter.CurrentBonesPoses to return Pose.identity for the wrist (useful if we consider the wrist to be at the root of the hand)")]
        public bool forceWristPoseToIdentity = true;

        [Header("Collected data")]

        Dictionary<HandSynchronizationBoneId, Pose> _currentPoses = new Dictionary<HandSynchronizationBoneId, Pose>();

        public Dictionary<HandSynchronizationBoneId, Pose> CurrentBonesPoses => _currentPoses;

        Dictionary<HandSynchronizationBoneId, Quaternion> _currentBoneRotations = new Dictionary<HandSynchronizationBoneId, Quaternion>();

        public Dictionary<HandSynchronizationBoneId, Quaternion> CurrentBoneRotations => _currentBoneRotations;

        [Header("Tracking state")]
        [SerializeField]
        bool isFingerTrackingAvailable;
        [SerializeField]
        bool isControllerTrackingAvailable;

        IRig rig;

        // Wrist pose, relatively to the rig
        public Pose WristPose { 
            get
            {
                var wristIndex = XRHandJointID.Wrist.ToIndex();
                Pose pose = Pose.identity;
                // Check IsCreated, as this native array might have been disposed
                if (enabled && m_JointLocalPoses != null && wristIndex < m_JointLocalPoses.Length && m_JointLocalPoses.IsCreated)
                {
                    pose = m_JointLocalPoses[wristIndex];
                }
                return pose;
            }
        }

        public Pose WorldWristPose
        {
            get
            {
                DetectRig();
                if (rig != null)
                {
                    var localWristPose = WristPose;
                    var pose = new Pose
                    {
                        position = rig.transform.TransformPoint(localWristPose.position),
                        rotation = rig.transform.rotation * localWristPose.rotation
                    };
                    return pose;
                } 
                else
                {
                    throw new System.Exception("[XRHandCollectableSkeletonDriver] Missing rig in hierarchy");
                }
            }
        }

        public HandTrackingMode CurrentHandTrackingMode
        { 
            get
            {
                isFingerTrackingAvailable = handTrackingEvents == null || handTrackingEvents.handIsTracked;
                if (controllerTrackingMode == ControllerTrackingMode.AlwaysAvailable)
                {
                    isControllerTrackingAvailable = true;
                }
#if ENABLE_INPUT_SYSTEM
                else if (controllerTrackingMode == ControllerTrackingMode.UseInputAction && controllerAvailableAction.action != null && controllerAvailableAction.action.ReadValue<float>() == 1)
                {
                    isControllerTrackingAvailable = true;
                } 
#endif
                else
                {
                    isControllerTrackingAvailable = false;
                }
                if (isFingerTrackingAvailable)
                {
                    return HandTrackingMode.FingerTracking;
                }
                else if (isControllerTrackingAvailable)
                {
                    return HandTrackingMode.ControllerTracking;
                }
                return HandTrackingMode.NotTracked;
            }
        }

        private void Awake()
        {
            DetectRig();
#if ENABLE_INPUT_SYSTEM

            if (controllerTrackingMode == ControllerTrackingMode.UseInputAction)
            {
                if (name.Contains("Left", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    controllerAvailableAction.EnableWithDefaultXRBindings(leftBindings: new List<string> { "isTracked" });
                }
                else
                {
                    controllerAvailableAction.EnableWithDefaultXRBindings(leftBindings: new List<string> { "isTracked" });
                }
            }
#endif

            if (applyWristPoseToHandRoot && handRoot == null)
            {
                var hardwareHand = GetComponentInParent<IHardwareHand>();
                handRoot = hardwareHand.gameObject.transform;
            }
        }

        protected virtual void DetectRig()
        {
            if (rig == null)
            {
                rig = GetComponentInParent<IRig>(true);
            }
        }
        protected override void OnEnable()
        {
            DetectRig();
            if (rootTransform == null)
            {
                var handRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
                if (handRenderer != null)
                {
                    var root = handRenderer.rootBone;
                    // Initialization sequence source: HandVisualizer from XRHands HandVisualizer sample
                    jointTransformReferences = new List<JointToTransformReference>();
                    rootTransform = root;
                    XRHandSkeletonDriverUtility.FindJointsFromRoot(this);
                    InitializeFromSerializedReferences();
                }

                if (handTrackingEvents == null)
                {
                    handTrackingEvents = GetComponentInChildren<XRHandTrackingEvents>();
                }
            }
            base.OnEnable();
            if (handTrackingEvents == null)
            {
                Debug.LogError("Missing XRHandTrackingEvents: IsFingerTrackingAvailable will always return true");
            }
        }

#if UNITY_EDITOR
        [Header("Debug - Hand analysis ")]
        public XRhandAnalyser analyser = new XRhandAnalyser();

        [Header("Debug - Mirror hand")]
        public bool sendPositionsToDebugMirrorSkeleton = false;
        public XRHandRemoteSkeletonDriver debugMirrorSkeleton;

#endif

        public bool TryJointWorldPose(XRHandJointID jointId, out Pose worldPose)
        {
            worldPose = default;
            var jointIndex = jointId.ToIndex();
            if (jointIndex >= m_JointTransforms.Length) return false;
            worldPose.position = m_JointTransforms[jointIndex].position;
            worldPose.rotation = m_JointTransforms[jointIndex].rotation;
            return true;
        }

        protected override void OnRootPoseUpdated(Pose rootPose)
        {
            if (applyWristPoseToHandRoot && handRoot != null)
            {
                var wristIndex = XRHandJointID.Wrist.ToIndex();
                handRoot.localPosition = rootPose.position;
                handRoot.localRotation = rootPose.rotation;
                m_JointLocalPoses[wristIndex] = Pose.identity;
            }
        }

        protected override void OnJointsUpdated(XRHandJointsUpdatedEventArgs args)
        {
            UpdateJointLocalPoses(args);
            if (applyWristPoseToHandRoot)
            {
                var wristIndex = XRHandJointID.Wrist.ToIndex();
                m_JointLocalPoses[wristIndex] = Pose.identity;
            }
            ApplyUpdatedTransformPoses();

            _currentPoses.Clear();


#if UNITY_EDITOR
            analyser.StartBonesAnalysis(m_JointLocalPoses.Length);
#endif
            for (int i = 0; i < m_JointLocalPoses.Length; i++)
            {
                var jointId = XRHandJointIDUtility.FromIndex(i);
                var boneId = jointId.AsHandSynchronizationBoneId();
                var pose = m_JointLocalPoses[i];
                if (forceWristPoseToIdentity && boneId == HandSynchronizationBoneId.Hand_WristRoot)
                {
                    _currentPoses[boneId] = Pose.identity;
                }
                else
                {
                    _currentPoses[boneId] = pose;
                }
#if UNITY_EDITOR
                analyser.AddPose(index: i, pose, boneId);
#endif
            }

            _currentBoneRotations.Clear();
            foreach (var boneId in _currentPoses.Keys)
            {
                _currentBoneRotations[boneId] = _currentPoses[boneId].rotation;

            }

#if UNITY_EDITOR
            analyser.EndBonesAnalysis();

            if (debugMirrorSkeleton)
            {
                Dictionary<HandSynchronizationBoneId, Pose> appliedPoses = new Dictionary<HandSynchronizationBoneId, Pose>();

                foreach (var boneId in _currentPoses.Keys)
                {
                    if (sendPositionsToDebugMirrorSkeleton)
                    {
                        appliedPoses[boneId] = new Pose { position = _currentPoses[boneId].position, rotation = _currentPoses[boneId].rotation };
                    }
                    else
                    {
                        appliedPoses[boneId] = new Pose { position = Vector3.zero, rotation = _currentPoses[boneId].rotation };
                    }
                }
                debugMirrorSkeleton.ApplyPoses(appliedPoses);
            }
#endif
        }

#if BURST_PRESENT
        [BurstCompile]
#endif
        static void CalculateLocalTransformPose(in Pose parentPose, in Pose jointPose, out Pose jointLocalPose)
        {
            var inverseParentRotation = Quaternion.Inverse(parentPose.rotation);
            jointLocalPose.position = inverseParentRotation * (jointPose.position - parentPose.position);
            jointLocalPose.rotation = inverseParentRotation * jointPose.rotation;
        }

        public bool TryGetBonePose(XRHandJointID jointId, out Pose rigRelativePose, out Pose worldPose)
        {
            rigRelativePose = default;
            worldPose = default;
            DetectRig();
            
            if (m_JointTransforms == null)
            {
                Debug.Log("Join transforms not yet initialized, skip bone pose analysis");
                return false;
            }

            if (TryJointWorldPose(jointId, out worldPose))
            {
                rigRelativePose.position = rig.transform.InverseTransformPoint(worldPose.position);
                rigRelativePose.rotation = Quaternion.Inverse(rig.transform.rotation) * worldPose.rotation;
                return true;
            }
            return false;
        }

        public bool TryGetBoneRigRelativePose(XRHandJointID jointId, out Pose rigRelativePose)
        {
            return TryGetBonePose(jointId, out rigRelativePose, out _);
        }
    }

    public static class XRHandsSyncExtension
    {
        public static HandSynchronizationBoneId AsHandSynchronizationBoneId(this XRHandJointID jointId)
        {
            HandSynchronizationBoneId boneId = HandSynchronizationBoneId.Invalid;
            switch (jointId)
            {
                case XRHandJointID.Wrist:
                    boneId = HandSynchronizationBoneId.Hand_WristRoot;
                    break;
                case XRHandJointID.Palm:
                    boneId = HandSynchronizationBoneId.Hand_Palm;
                    break;
                case XRHandJointID.ThumbMetacarpal:
                    boneId = HandSynchronizationBoneId.Hand_Thumb0;
                    break;
                case XRHandJointID.ThumbProximal:
                    boneId = HandSynchronizationBoneId.Hand_Thumb1;
                    break;
                case XRHandJointID.ThumbDistal:
                    boneId = HandSynchronizationBoneId.Hand_Thumb2;
                    break;
                case XRHandJointID.ThumbTip:
                    boneId = HandSynchronizationBoneId.Hand_ThumbTip;
                    break;
                case XRHandJointID.IndexMetacarpal:
                    boneId = HandSynchronizationBoneId.Hand_Index0;
                    break;
                case XRHandJointID.IndexProximal:
                    boneId = HandSynchronizationBoneId.Hand_Index1;
                    break;
                case XRHandJointID.IndexIntermediate:
                    boneId = HandSynchronizationBoneId.Hand_Index2;
                    break;
                case XRHandJointID.IndexDistal:
                    boneId = HandSynchronizationBoneId.Hand_Index3;
                    break;
                case XRHandJointID.IndexTip:
                    boneId = HandSynchronizationBoneId.Hand_IndexTip;
                    break;
                case XRHandJointID.MiddleMetacarpal:
                    boneId = HandSynchronizationBoneId.Hand_Middle0;
                    break;
                case XRHandJointID.MiddleProximal:
                    boneId = HandSynchronizationBoneId.Hand_Middle1;
                    break;
                case XRHandJointID.MiddleIntermediate:
                    boneId = HandSynchronizationBoneId.Hand_Middle2;
                    break;
                case XRHandJointID.MiddleDistal:
                    boneId = HandSynchronizationBoneId.Hand_Middle3;
                    break;
                case XRHandJointID.MiddleTip:
                    boneId = HandSynchronizationBoneId.Hand_MiddleTip;
                    break;
                case XRHandJointID.RingMetacarpal:
                    boneId = HandSynchronizationBoneId.Hand_Ring0;
                    break;
                case XRHandJointID.RingProximal:
                    boneId = HandSynchronizationBoneId.Hand_Ring1;
                    break;
                case XRHandJointID.RingIntermediate:
                    boneId = HandSynchronizationBoneId.Hand_Ring2;
                    break;
                case XRHandJointID.RingDistal:
                    boneId = HandSynchronizationBoneId.Hand_Ring3;
                    break;
                case XRHandJointID.RingTip:
                    boneId = HandSynchronizationBoneId.Hand_RingTip;
                    break;
                case XRHandJointID.LittleMetacarpal:
                    boneId = HandSynchronizationBoneId.Hand_Pinky0;
                    break;
                case XRHandJointID.LittleProximal:
                    boneId = HandSynchronizationBoneId.Hand_Pinky1;
                    break;
                case XRHandJointID.LittleIntermediate:
                    boneId = HandSynchronizationBoneId.Hand_Pinky2;
                    break;
                case XRHandJointID.LittleDistal:
                    boneId = HandSynchronizationBoneId.Hand_Pinky3;
                    break;
                case XRHandJointID.LittleTip:
                    boneId = HandSynchronizationBoneId.Hand_PinkyTip;
                    break;
            }
            return boneId;
        }
    }

}
