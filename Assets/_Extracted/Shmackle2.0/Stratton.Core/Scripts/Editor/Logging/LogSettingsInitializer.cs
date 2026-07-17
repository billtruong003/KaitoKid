using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    [InitializeOnLoad]
    public class LogSettingsInitializer
    {
        static LogSettingsInitializer()
        {
            Initialize();
        }

        public static void Initialize()
        {
            var path = Application.dataPath + "/" + LogSettings.SettingsDirectory;
            if (!File.Exists(path + "/" + LogSettings.SettingsFileName + ".asset"))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                LogSettings settings = ScriptableObject.CreateInstance(typeof(LogSettings)) as LogSettings;
                settings.Refresh();
                AssetDatabase.CreateAsset(settings, $"Assets/{LogSettings.SettingsDirectory}/{LogSettings.SettingsFileName}.asset");
                AssetDatabase.SaveAssets();

                Debug.Log("LogSettings Created");
            }
        }
    }
}