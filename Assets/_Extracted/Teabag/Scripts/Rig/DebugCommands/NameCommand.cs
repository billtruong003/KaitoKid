#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Squido.JungleXRKit.Core;
using Teabag.Core;

namespace Teabag.Player
{
    public sealed class NameCommand : IDebugCommand
    {
        public string Name => "name";
        public string Usage => "name | name <newname>";

        public string Execute(string[] args)
        {
            var gorillaService = ServiceLocator.Get<IGorillaService>();
            var local = gorillaService?.LocalGorilla as Gorilla;

            if (local is null)
                return "No local gorilla found";

            if (args.Length < 2)
                return $"Current name: '{local.playerName}'";

            string newName = string.Join(" ", args, 1, args.Length - 1);
            local.playerName = newName;
            PlayerData.displayName = newName;

            return $"Name changed to '{newName}'";
        }
    }
}
#endif
