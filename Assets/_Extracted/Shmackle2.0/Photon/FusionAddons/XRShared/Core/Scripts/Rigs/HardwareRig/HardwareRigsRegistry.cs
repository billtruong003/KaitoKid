using System.Collections.Generic;
using UnityEngine;

namespace Fusion.XR.Shared.Core
{
    public static class HardwareRigsRegistry
    {
        #region Hardware rig discoverability
        // When enabled, when using Fusion.XR.Shared.Core's NetworkRig and NetworkRigPart subclasses,
        //  an IHardwareRig is expected to register with HardwareRigsRegistry.RegisterAvailableHardwareRig(this), and unregister when not with HardwareRigsRegistry.UnregisterAvailableHardwareRig(this)
        static List<IHardwareRig> AvailableHardwareRigs = new List<IHardwareRig>();

        public static void RegisterAvailableHardwareRig(IHardwareRig hardwareRig)
        {
            if (AvailableHardwareRigs.Contains(hardwareRig) == false)
            {
                AvailableHardwareRigs.Add(hardwareRig);
            }
        }

        public static void UnregisterAvailableHardwareRig(IHardwareRig hardwareRig)
        {
            AvailableHardwareRigs.Remove(hardwareRig);
        }

        public static List<IHardwareRig> GetAvailableHardwareRigs()
        {
            return AvailableHardwareRigs;
        }
        
        public static IHardwareRig GetHardwareRig(NetworkRunner runner = null)
        {
            if(runner == null)
            {
                if (AvailableHardwareRigs.Count > 0)
                {
                    return AvailableHardwareRigs[0];
                }
            } 
            else
            {
                foreach(var hardwareRig in AvailableHardwareRigs)
                {
                    // If several rig are present (multi peer scenario), we use the runner to differenciate
                    if (hardwareRig.Runner == runner || AvailableHardwareRigs.Count == 1)
                    {
                        return hardwareRig;
                    }
                }
            }
            return null;
        }
        #endregion
    }
}

