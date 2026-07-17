using UnityEngine;

namespace Mythfall.Input
{
    /// <summary>
    /// Single source of truth for player locomotion input.
    /// VirtualJoystick (Sprint 1 Day 3) writes MoveVector each frame; PlayerBase reads it.
    /// Mobile players run by default — no walk modifier in slice scope.
    /// </summary>
    public static class MobileInputManager
    {
        public static Vector2 MoveVector { get; set; }
        public static bool IsRunning { get; set; } = true;

        /// <summary>Reset called by states/scenes that want a clean input baseline.</summary>
        public static void Reset()
        {
            MoveVector = Vector2.zero;
            IsRunning = true;
        }
    }
}
