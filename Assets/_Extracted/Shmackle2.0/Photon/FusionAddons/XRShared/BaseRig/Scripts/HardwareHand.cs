using Fusion.XR.Shared.Core;

namespace Fusion.XR.Shared.Base
{
    public class HardwareHand : BaseLateralizedHardwareRigPart, IHardwareHand
    {
        public override RigPartKind Kind => RigPartKind.Hand;
    }
} 

