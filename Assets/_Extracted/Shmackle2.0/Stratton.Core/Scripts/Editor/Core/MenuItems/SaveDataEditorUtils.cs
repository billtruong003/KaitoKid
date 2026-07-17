using System.IO;
using Stratton.Core;
using SQLite4Unity3d;
using UnityEditor;
using UnityEngine;

namespace Stratton.Save.Editor
{
    public class SaveDataEditorUtils
    {
        #region Private Methods

        [MenuItem("Tools/Clear Player Prefs")]
        private static void ClearPlayerPrefs()
        {
            if (EditorUtility.DisplayDialog("Clear Player Prefs", "Do you want to clear Player Prefs?", "Ok", "Cancel"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Log.Message(BaseLogChannel.Core, "Player Prefs cleared");
            }
        }

        [MenuItem("Tools/Clear SQLite Database")]
        private static void ClearSQLiteDatabase()
        {
            if (EditorUtility.DisplayDialog("Clear SQLite Database", "Do you want to clear SQLite Database?", "Ok", "Cancel"))
            {
                var dbPath = Path.Combine(Application.persistentDataPath, SaveSettings.Instance.DatabaseName + ".db");
                var connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
                connection.DeleteAll<PersistentValueRecord>();
                connection.Dispose();
                connection.Close();
                Log.Message(BaseLogChannel.Core, "SQLite Database cleared");
            }
        }

        #endregion
    }
}