using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    public class TrashFilesClear
    {
        #region Private Methods

        [MenuItem("Tools/Clear Empty Folders")]
        private static void ClearEmptyFoldersLoop()
        {
            for (int i = 0; i < 10; i++)
            {
                if (!RemoveEmptyFolders())
                {
                    break;
                }
            }
        }

        [MenuItem("Tools/Clear .orig Files")]
        private static void RemoveOrigs()
        {
            EditorUtility.DisplayProgressBar("Deleting origs...", "Wait...", 0f);
            try
            {
                string dataPackagesDir = "Assets/";

#if UNITY_EDITOR_WIN
                dataPackagesDir = dataPackagesDir.Replace('/', '\\');
#else
			dataPackagesDir = dataPackagesDir.Replace('\\', '/');
#endif
                DirectoryInfo dir = new DirectoryInfo(dataPackagesDir);

                FileInfo[] info = dir.GetFiles("*.orig", SearchOption.AllDirectories);

                foreach (FileInfo f in info)
                {
                    string path = f.Directory + "/" + f.Name;
                    Log.Message(BaseLogChannel.Core, "Remove: " + path);
                    f.Delete();
                }
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Log.Error(BaseLogChannel.Core, e.ToString());
            }
            EditorUtility.ClearProgressBar();
        }

        private static bool RemoveEmptyFolders()
        {
            EditorUtility.DisplayProgressBar("Deleting empty folders...", "Wait...", 0f);
            string[] dirsPaths = Directory.GetDirectories(Application.dataPath, "*", SearchOption.AllDirectories);
            int counter = 0;
            bool removed = false;
            foreach (string dirPath in dirsPaths)
            {
                counter++;
                float progress = (float)counter / dirsPaths.Length;
                try
                {
                    EditorUtility.DisplayProgressBar("Deleting empty folders...",
                        "Progress: " + ((int)(progress * 100)) + "%", progress);
                    if (Directory.GetDirectories(dirPath, "*", SearchOption.TopDirectoryOnly).Length == 0
                        && Directory.GetFiles(dirPath, "*", SearchOption.TopDirectoryOnly).Length == 0)
                    {
                        DirectoryInfo di = new DirectoryInfo(dirPath);
                        FileInfo metaFile = di.Parent.GetFiles(di.Name + ".meta")[0];
                        metaFile.Delete();
                        Directory.Delete(dirPath);
                        Log.Message(BaseLogChannel.Core, "Delete " + dirPath);
                        removed = true;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(BaseLogChannel.Core, "File error: " + e);
                }
            }
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            return removed;
        }

        #endregion
    }
}