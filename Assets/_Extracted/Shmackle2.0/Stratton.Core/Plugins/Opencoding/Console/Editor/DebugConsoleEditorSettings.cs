using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Opencoding.Console.Editor
{
    internal static class DebugConsoleEditorSettings
    {
        // Change this if you move the Opencoding directory to a different location
        private static string _opencodingDirectoryLocation = "Packages/com.Stratton.framework.core/Plugins/Opencoding";

        public static bool AutomaticallyLoadConsoleInEditor { get; private set; }

        public static string OpencodingDirectoryLocation { get { return _opencodingDirectoryLocation; } }

        static DebugConsoleEditorSettings()
        {
            AutomaticallyLoadConsoleInEditor = EditorPrefs.GetBool("TouchConsolePro/AutomaticallyLoadConsoleInEditor", false);
        }

        [SettingsProvider]
        public static SettingsProvider SettingsOnGUI()
        {
            return new SettingsProvider("Preferences/TouchConsole Pro", SettingsScope.User)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "TouchConsole Pro",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    bool value = GUILayout.Toggle(AutomaticallyLoadConsoleInEditor, "Automatically load in editor");
                    if (AutomaticallyLoadConsoleInEditor != value)
                    {
                        AutomaticallyLoadConsoleInEditor = value;
                        EditorPrefs.SetBool("TouchConsolePro/AutomaticallyLoadConsoleInEditor",
                            AutomaticallyLoadConsoleInEditor);
                    }

                    GUILayout.Label(
                        "This places the DebugConsole prefab into the scene automatically for you when you play in the editor. This has no effect on built versions of your game.",
                        "helpbox");
                },
                keywords = new HashSet<string>() { "console", "TouchConsole Pro", "TouchConsolePro", "opencoding" }
            };
        }
    }

}