using System;
using UnityEngine;

namespace Teabag.Player
{
    public enum CosmeticSlotType
    {
        Attachment,
        Replacement
    }

    /// <summary>
    /// Represents a cosmetic slot in the VR rig.
    /// </summary>
    [Serializable]
    public struct VRRigCosmeticSlot
    {
        public CosmeticSlot slot;
        public CosmeticSlotType type;
        public Transform slotParent;
        public SkinnedMeshRenderer[] renderers;
        public Cosmetic currentCosmetic;
    }
}
