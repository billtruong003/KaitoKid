using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Stratton.Core;
using Stratton.Data;
using Newtonsoft.Json;
using SQLite4Unity3d;
using UnityEngine;

namespace Stratton.Save
{
    public class PersistentValueRecord
    {
        [PrimaryKey]
        public string Key { get; set; }
        public string StringValue { get; set; }
        public byte[] BlobValue { get; set; }
    }

    [DataHostTarget(BaseDataHostType.SQLite)]
    public class SQLiteSaveService : ISaveService
    {
        #region Fields

        private JsonSerializerSettings _serializerSettings;
        private SQLiteConnection _connection;

        #endregion

        #region Properties

        public bool IsReady { get; protected set; }

        #endregion

        public async UniTask<InitializationResult> Init(SaveSystem saveSystem)
        {
            _serializerSettings = saveSystem.GetJsonSerializerSettings();
            var dbPath = Path.Combine(Application.persistentDataPath, SaveSettings.Instance.DatabaseName + ".db");
            _connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            _connection.CreateTable<PersistentValueRecord>();
            IsReady = true;
            await UniTask.CompletedTask;
            return InitializationResult.Success;
        }

        public void DeInit()
        {
            _connection.Dispose();
            _connection.Close();
            IsReady = false;
        }

        public void Commit<T>(string key, T value)
        {
            var record = new PersistentValueRecord
            {
                Key = key
            };

            if (value is int || value is float || value is double || value is bool || value is string)
            {
                record.StringValue = value.ToString();
                record.BlobValue = null;
            }
            else
            {
                record.StringValue = null;
                record.BlobValue = SerializeToBlob(value);
            }

            _connection.InsertOrReplace(record);
        }

        public async UniTask CommitBatch(Dictionary<string, object> values)
        {
            await UniTask.RunOnThreadPool(() =>
            {
                _connection.BeginTransaction();
                try
                {
                    foreach (var kvp in values)
                    {
                        var key = kvp.Key;
                        var value = kvp.Value;

                        var record = new PersistentValueRecord
                        {
                            Key = key,
                        };

                        if (value is int || value is float || value is double || value is bool || value is string)
                        {
                            record.StringValue = value.ToString();
                            record.BlobValue = null;
                        }
                        else
                        {
                            record.StringValue = null;
                            record.BlobValue = SerializeToBlob(value);
                        }

                        _connection.InsertOrReplace(record);
                    }
                    _connection.Commit();
                }
                catch
                {
                    _connection.Rollback();
                    throw;
                }
            });
        }

        public bool TryGet<T>(string key, out T value)
        {
            var record = _connection.Find<PersistentValueRecord>(key);

            if (record == null)
            {
                value = default;
                return false;
            }

            if (record.BlobValue != null)
            {
                value = DeserializeFromBlob<T>(record.BlobValue);
                return true;
            }

            if (!string.IsNullOrEmpty(record.StringValue))
            {
                value = (T)Convert.ChangeType(record.StringValue, typeof(T));
                return true;
            }

            throw new Exception("No value found for the given key.");
        }

        public async UniTask DeleteAll()
        {
            _connection.DeleteAll<PersistentValueRecord>();
            await UniTask.CompletedTask;
        }

        public void DeleteKey(string key)
        {
            _connection.Delete<PersistentValueRecord>(key);
        }

        private byte[] SerializeToBlob<T>(T value)
        {
            string json = JsonConvert.SerializeObject(value, _serializerSettings);
            return Encoding.UTF8.GetBytes(json);
        }

        private T DeserializeFromBlob<T>(byte[] blob)
        {
            string json = Encoding.UTF8.GetString(blob);
            return JsonConvert.DeserializeObject<T>(json, _serializerSettings);
        }
    }
}


