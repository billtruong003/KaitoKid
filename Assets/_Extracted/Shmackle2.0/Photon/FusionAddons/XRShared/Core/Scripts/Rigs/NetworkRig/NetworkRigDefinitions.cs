using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fusion.XR.Shared.Core
{
    public interface INetworkRigPart : IRigPart, INetworkObject
    {
        public const int EXECUTION_ORDER = INetworkRig.EXECUTION_ORDER + 10;
        public IHardwareRigPart LocalHardwareRigPart { get; }
    }

    public interface ILateralizedNetworkRigPart : INetworkRigPart, ILateralizedRigPart { }

    public interface INetworkHand : ILateralizedNetworkRigPart, IHand
    {
        public IHardwareHand LocalHardwareHand { get; }
    }

    public interface INetworkController : ILateralizedNetworkRigPart, IController {}

    public interface INetworkHeadset : INetworkRigPart, IHeadset { }

    public interface INetworkRig : IRig, INetworkObject
    {
        public const int EXECUTION_ORDER = 100;
        public void RegisterNetworkRigPart(INetworkRigPart rigPart);
        public void UnregisterNetworkRigPart(INetworkRigPart rigPart);
        public List<INetworkRigPart> RigParts { get; }
        /// <summary>
        /// Describe if this hardware rig (and the associated interaction stack requires a specific extrapolation timing (usually DuringFusionRender, but some stack may edit hardware rig parts position at a later timing, hence the OnBeforeRender option - whichi executes later)
        /// </summary>
        public ExtrapolationTiming RequiredExtrapolationTiming { get; }
    }

    /// <summary>
    /// Can be added on a rig part to specify that it should be offset, relatively to its normal position (useful if we want an interface to block an hand, ...)
    /// </summary>
    public interface IRigPartPositionModifier { 
        public Vector3 PositionOffset { get; }

    }

    public enum ExtrapolationTiming
    {
        NoExtrapolation,
        DuringFusionRender,
        // DuringUnityOnBeforeRender might be required as some network stacks may update position late, after Fusion's Render, on hardware rig/rig parts. To reflect those changes on the network rig/rig parts, we have to run the extrapolation later
        DuringUnityOnBeforeRender
    }

    public static class NetworkRigExtensions
    {
        /// <summary>
        /// Return the rig part if it is an hardware rig part, or the LocalHardwareRigPart, if any (if it is associated to the local user), for network rig parts
        /// </summary>
        public static IHardwareRigPart RelatedLocalHardwareRigPart(this IRigPart rigPart)
        {
            if (rigPart is IHardwareRigPart hardwareRigPart)
            {
                return hardwareRigPart;
            }
            if (rigPart is INetworkRigPart networkPart && networkPart.LocalHardwareRigPart is IHardwareRigPart localHardwareRigPart && localHardwareRigPart != null)
            {
                return localHardwareRigPart;
            }
            return null;
        }

        public static INetworkRig NetworkRig(this INetworkRigPart rigPart)
        {
            if (rigPart.Rig is INetworkRig rig)
            {
                return rig;
            }
            return null;
        }

        public static ExtrapolationTiming RequiredExtrapolationTiming(this INetworkRigPart rigPart)
        {
            var rig = rigPart.NetworkRig();
            return rig != null ? rig.RequiredExtrapolationTiming : ExtrapolationTiming.DuringFusionRender;
        }
    }
}

