#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;

namespace Teabag.Player
{
    public sealed class TeleportCommand : IDebugCommand
    {
        public string Name => "tp";
        public string Usage => "tp <x> <y> <z>";

        public string Execute(string[] args)
        {
            if (args.Length < 4)
                return "Usage: tp <x> <y> <z>";

            if (!ServiceLocator.TryGet<IRigInfoService>(out var rigInfo) || rigInfo.HardwareRig == null)
                return "No local player found";

            if (float.TryParse(args[1], out float x) is false
                || float.TryParse(args[2], out float y) is false
                || float.TryParse(args[3], out float z) is false)
            {
                return "Invalid coordinates — use numbers (e.g. tp 0 10 0)";
            }

            var target = new Vector3(x, y, z);
            rigInfo.HardwareRig.Teleport(target, Quaternion.identity);
            return $"Teleported to ({x}, {y}, {z})";
        }
    }
}
#endif
