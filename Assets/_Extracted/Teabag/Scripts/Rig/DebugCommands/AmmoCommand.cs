#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Teabag.Core;

namespace Teabag.Player
{
    public sealed class AmmoCommand : IDebugCommand
    {
        public string Name => "ammo";
        public string Usage => "ammo | ammo <type> [amount]";

        private static readonly Dictionary<string, string> AliasToType = new()
        {
            { "pistol", "PistolAmmo" },
            { "auto", "AutoAmmo" },
            { "shotgun", "ShotgunAmmo" },
            { "sniper", "SniperAmmo" },
        };

        public string Execute(string[] args)
        {
            if (GameServices.AddBackpackAmmo is null)
                return "No backpack available";

            int amount = 999;
            string alias = null;

            if (args.Length >= 2)
                alias = args[1].ToLowerInvariant();

            if (args.Length >= 3 && int.TryParse(args[2], out int parsed))
                amount = UnityEngine.Mathf.Clamp(parsed, 1, 9999);

            if (alias is null)
            {
                foreach (var kvp in AliasToType)
                    GameServices.AddBackpackAmmo(kvp.Value, amount);

                return $"Added {amount} of all ammo types";
            }

            if (AliasToType.TryGetValue(alias, out string typeName) is false)
                return $"Unknown ammo type '{alias}'. Valid types: pistol, auto, shotgun, sniper";

            GameServices.AddBackpackAmmo(typeName, amount);

            return $"Added {amount} {typeName}";
        }
    }
}
#endif
