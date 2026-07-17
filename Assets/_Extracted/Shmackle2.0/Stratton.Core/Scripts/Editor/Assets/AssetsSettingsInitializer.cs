using System.IO;
using Stratton.Core;
using UnityEditor;
using UnityEngine;

namespace Stratton.Assets.Editor
{
    [InitializeOnLoad]
    public class AssetsSettingsInitializer
    {
        static AssetsSettingsInitializer()
        {
            Initialize();
        }

        public static void Initialize()
        {
            var path = Application.dataPath + "/" + AssetsSettings.SettingsDirectory;
            if (!File.Exists(path + "/" + AssetsSettings.SettingsFileName + ".asset"))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                AssetsSettings settings = ScriptableObject.CreateInstance(typeof(AssetsSettings)) as AssetsSettings;
                AssetDatabase.CreateAsset(settings, $"Assets/{AssetsSettings.SettingsDirectory}/{AssetsSettings.SettingsFileName}.asset");
                AssetDatabase.SaveAssets();

                Log.Message(BaseLogChannel.Assets, "AssetsSettings Created");
            }
        }
    }
}