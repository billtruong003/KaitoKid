using System.IO;
using Stratton.Core;
using UnityEditor;
using UnityEngine;

namespace Stratton.CI.Editor
{
    [InitializeOnLoad]
    public class DeploymentEditorSettingsInitializer
    {
        static DeploymentEditorSettingsInitializer()
        {
            Initialize();
        }

        public static void Initialize()
        {
            var path = Application.dataPath + "/" + DeploymentEditorSettings.SettingsDirectory;
            if (!File.Exists(path + "/" + DeploymentEditorSettings.SettingsFileName + ".asset"))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                DeploymentEditorSettings settings = ScriptableObject.CreateInstance(typeof(DeploymentEditorSettings)) as DeploymentEditorSettings;
                AssetDatabase.CreateAsset(settings, DeploymentEditorSettings.SettingsFullPath);
                AssetDatabase.SaveAssets();

                Log.Message(BaseLogChannel.Core, "DeploymentEditorSettings Created");
            }
        }
    }
}