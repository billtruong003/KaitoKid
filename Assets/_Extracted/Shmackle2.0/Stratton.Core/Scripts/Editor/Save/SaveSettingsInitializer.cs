using System.IO;
using Stratton.Core;
using UnityEditor;
using UnityEngine;

namespace Stratton.Save.Editor
{
    [InitializeOnLoad]
    public class SaveSettingsInitializer
    {
        static SaveSettingsInitializer()
        {
            Initialize();
        }

        public static void Initialize()
        {
            var path = Application.dataPath + "/" + SaveSettings.SettingsDirectory;
            if (!File.Exists(path + "/" + SaveSettings.SettingsFileName + ".asset"))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                SaveSettings settings = ScriptableObject.CreateInstance(typeof(SaveSettings)) as SaveSettings;
                AssetDatabase.CreateAsset(settings, $"Assets/{SaveSettings.SettingsDirectory}/{SaveSettings.SettingsFileName}.asset");
                AssetDatabase.SaveAssets();

                Log.Message(BaseLogChannel.Save, "SaveSettings Created");
            }
        }
    }
}