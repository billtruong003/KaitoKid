using System.IO;
using SQLite4Unity3d;
using UnityEditor;
using UnityEngine;

namespace Stratton.Save.Editor
{
    public class SaveKeyCheckerEditor : EditorWindow
    {
        private enum SaveService
        {
            SQLite,
            PlayerPrefs
        }
        private string _keyToCheck = "";
        private string _resultMessage = "";
        private SQLiteConnection _connection;
        private MessageType _messageType;
        private SaveService _saveService = SaveService.SQLite;

        [MenuItem("Tools/Save Key Checker")]
        public static void ShowWindow()
        {
            GetWindow<SaveKeyCheckerEditor>("Save Key Checker");
        }

        private void Awake()
        {
            CheckKey();
        }

        private void OnDestroy()
        {
            _connection?.Dispose();
            _connection?.Close();
        }

        private void OnGUI()
        {
            GUILayout.Label("Save Key Checker", EditorStyles.boldLabel);

            _saveService = (SaveService)EditorGUILayout.EnumPopup("Save Service", _saveService);
            _keyToCheck = EditorGUILayout.TextField("Key", _keyToCheck);

            if (_saveService == SaveService.SQLite)
            {
                if (GUILayout.Button("Log all keys to console"))
                {
                    GetAllKeys();
                }
            }
            if (GUILayout.Button("Check Key"))
            {
                CheckKey();
            }

            EditorGUILayout.HelpBox(_resultMessage, _messageType);
        }

        private void CheckKey()
        {
            if (string.IsNullOrEmpty(_keyToCheck))
            {
                _resultMessage = "Please enter a key.";
                _messageType = MessageType.Info;
                return;
            }

            switch (_saveService)
            {
                case SaveService.SQLite:
                    if (_connection == null)
                    {
                        var dbPath = Path.Combine(Application.persistentDataPath, SaveSettings.Instance.DatabaseName + ".db");
                        _connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly | SQLiteOpenFlags.Create);
                    }

                    var result = _connection.Find<PersistentValueRecord>(_keyToCheck);
                    if (result != null)
                    {
                        var value = result.BlobValue != null ? result.BlobValue.ToString() : result.StringValue;
                        _resultMessage = $"The key '{_keyToCheck}' exists in SQLite database. \nValue - {value}";
                        _messageType = MessageType.Error;
                    }
                    else
                    {
                        _resultMessage = $" The key '{_keyToCheck}' does not exist in SQLite database.";
                        _messageType = MessageType.Info;
                    }
                    break;
                case SaveService.PlayerPrefs:
                    if (PlayerPrefs.HasKey(_keyToCheck))
                    {
                        _resultMessage = $"The key '{_keyToCheck}' exists in PlayerPrefs. \nValue - {PlayerPrefs.GetString(_keyToCheck)}";
                        _messageType = MessageType.Error;
                    }
                    else
                    {
                        _resultMessage = $"The key '{_keyToCheck}' does not exist in PlayerPrefs.";
                        _messageType = MessageType.Info;
                    }
                    break;
            }        
        }

        private void GetAllKeys()
        {
            if (_connection == null)
            {
                var dbPath = Path.Combine(Application.persistentDataPath, SaveSettings.Instance.DatabaseName + ".db");
                _connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly | SQLiteOpenFlags.Create);
            }
            var results = _connection.Table<PersistentValueRecord>();
            foreach (var record in results)
            {
                Debug.Log($"Key: '{record.Key}'");
            }
        }

    }
}


