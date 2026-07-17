using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    [InitializeOnLoad]
    public static class MakroToolBarGenerator
    {
        #region Constructors

        static MakroToolBarGenerator()
        {
            EditorApplication.update += RunOnce;
        }

        #endregion

        #region Public Methods

        public static void Refresh()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MakroSettings>(MakroSettings.MakroSettingsPath);
            if (settings != null)
            {
                string filePath = Path.Combine(Application.dataPath, settings.MakroToolBarScriptPath);
                StringBuilder text =
                    new StringBuilder(
                        "using System; \nusing System.Collections.Generic; \nusing System.Text; \nusing UnityEngine; \nusing UnityEditor;\n\n");
                text.AppendLine("namespace Stratton.Core.Editor\n{\n\npublic class MakroToolBar\n{");

                var dataList = settings.Data;
                for (int i = 0; i < dataList.Count; i++)
                {
                    string buttonClearName = dataList[i].ButtonName.Replace(" ", "_");
                    text.AppendLine("[MenuItem(\"Makro/" + dataList[i].ButtonName + "\")]");
                    text.AppendLine("public static void Makro_" + buttonClearName + "()\n{");
                    text.AppendLine("var makroData = MakroToolBarGenerator.LoadMakroData(" + i + ");");
                    text.AppendLine("MakroWindowController.UseMakroData(makroData);");
                    text.AppendLine("\n}");
                }

                text.AppendLine("[MenuItem(\"Makro/Refresh\")]");
                text.AppendLine("public static void Makro_Refresh()\n{");
                text.AppendLine("MakroToolBarGenerator.Refresh();");
                text.AppendLine("\n}");

                text.AppendLine("\n}\n}");

                var directoryPath = Directory.GetParent(filePath);
                if (!Directory.Exists(directoryPath.FullName))
                {
                    Directory.CreateDirectory(directoryPath.FullName);
                }

                File.WriteAllText(filePath, text.ToString());
                AssetDatabase.Refresh();
            }
        }

        public static MakroData LoadMakroData(int id)
        {
            var settings = AssetDatabase.LoadAssetAtPath<MakroSettings>(MakroSettings.MakroSettingsPath);
            if (settings != null)
            {
                return settings.Data[id];
            }
            return null;
        }

        #endregion

        #region Private Methods

        private static void RunOnce()
        {
            EditorApplication.update -= RunOnce;
            var settings = AssetDatabase.LoadAssetAtPath<MakroSettings>(MakroSettings.MakroSettingsPath);
            if (settings != null)
            {
                string filePath = Path.Combine(Application.dataPath, settings.MakroToolBarScriptPath);
                if (File.Exists(filePath))
                {
                    return;
                }
                Refresh();
            }
        }

        #endregion
    }
}