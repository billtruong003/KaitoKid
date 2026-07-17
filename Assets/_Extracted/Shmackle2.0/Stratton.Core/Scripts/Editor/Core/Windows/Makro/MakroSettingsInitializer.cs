using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    [InitializeOnLoad]
    public class MakroSettingsInitializer
    {
        static MakroSettingsInitializer()
        {
            Initialize();
        }

        public static void Initialize()
        {
            var rootPath = Directory.GetParent(Application.dataPath).FullName;
            var filePath = Path.Combine(rootPath, MakroSettings.MakroSettingsPath);
            if (!File.Exists(filePath))
            {
                var dirPath = Path.Combine(rootPath, MakroSettings.SettingsDirectory);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                MakroSettings settings = ScriptableObject.CreateInstance(typeof(MakroSettings)) as MakroSettings;
                AssetDatabase.CreateAsset(settings, MakroSettings.MakroSettingsPath);
                AssetDatabase.SaveAssets();
                MakroToolBarGenerator.Refresh();

                Debug.Log("MakroSettings Created");
            }
        }
    }
}
