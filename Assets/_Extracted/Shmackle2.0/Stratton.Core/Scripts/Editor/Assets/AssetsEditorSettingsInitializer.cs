using System.IO;
using Stratton.Core;
using UnityEditor;
using UnityEngine;

namespace Stratton.Assets.Editor
{
    [InitializeOnLoad]
    public class AssetsEditorSettingsInitializer
    {
        static AssetsEditorSettingsInitializer()
        {
            Initialize();
        }

        public static void Initialize()
        {
            var path = Application.dataPath + "/" + AssetsEditorSettings.SettingsDirectory;
            if (!File.Exists(path + "/" + AssetsEditorSettings.SettingsFileName + ".asset"))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                AssetsEditorSettings settings = ScriptableObject.CreateInstance(typeof(AssetsEditorSettings)) as AssetsEditorSettings;
                AssetDatabase.CreateAsset(settings, AssetsEditorSettings.SettingsFullPath);
                AssetDatabase.SaveAssets();

                Log.Message(BaseLogChannel.Assets, "AssetsEditorSettings Created");
            }
        }
    }
}