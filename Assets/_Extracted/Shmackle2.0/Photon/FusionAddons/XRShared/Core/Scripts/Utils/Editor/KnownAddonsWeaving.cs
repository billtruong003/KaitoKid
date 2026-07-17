using Fusion.XR.Shared.Utils;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fusion.XRShared.Tools
{
    [InitializeOnLoad]
    public class KnownAddonsWeaving
    {
        static KnownAddonsWeaving()
        {
            // Note: do not list here assembly not dependent of XRShared.Core. for those one, call directly AddonWeaver.AddAssemblyToWeaver in an Editor script in those addons
            string[] addonsAssembliesToWeave = new string[] {
                "BlockingContact",
                "TextureDrawing",
                "XRShared.Interaction.HardwareBasedGrabbing",
                "MXInkIntegration",
                "DataSyncHelpers.DataTools",
                "XRShared.Core.Tools",
                "TextureDrawing.Pen",
                "InteractiveMenu",
                "AudioRoom",
                "LocomotionValidation",
                "ChatBubble",
                "Magnets",
                "StructureCohesion",
                "MetaCoreIntegration",
                "Screensharing",
                "SocialDistancing",
                "Drawing",
                "UISynchronization",
                "VisionOSHelpers",
                "LineDrawing.XRShared",
                "PositionDebugging",
                "StickyNotes",
                "XRShared.RemoteBasedGrabbing",
            };
            foreach(var assemblyName in addonsAssembliesToWeave)
            {
                WeaveIfAssemblyIsAvailable(assemblyName);
            }
        }

        public static void WeaveIfAssemblyIsAvailable(string assemblyName)
        {
            bool isAssemblyPresent = AddonWeaver.CheckAssemblyPresence(assemblyName);
            if (isAssemblyPresent)
            {
                bool isWeaved = AddonWeaver.IsAddonWeaved(assemblyName);
                if (isWeaved == false)
                {
                    Debug.LogError($"{assemblyName} not yet added to assemblies to weave, adding it.");
                    AddonWeaver.AddAssemblyToWeaver(assemblyName);
                }

            }
        }
    }
}