using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Stratton.Core;
using Stratton.Data;
using Newtonsoft.Json;
using UnityEngine;

namespace Stratton.Save
{
    [DataHostTarget(BaseDataHostType.PlayerPrefs)]
    public class PlayerPrefsSaveService : ISaveService
    {
        #region Fields

        private JsonSerializerSettings _serializerSettings;

        #endregion

        #region Properties

        public bool IsReady { get; protected set; }

        #endregion

        #region Public Methods

        public async UniTask<InitializationResult> Init(SaveSystem saveSystem)
        {
            _serializerSettings = saveSystem.GetJsonSerializerSettings();
            IsReady = true;
            await UniTask.CompletedTask;
            return InitializationResult.Success;
        }

        public void DeInit()
        {
            IsReady = false;
        }

        /// <summary>
        /// Fetches data from the cached data by a key.
        /// </summary>
        /// <typeparam name="T">Type of the requested data class</typeparam>
        /// <param name="key">Key which holds the data</param>
        /// <returns>The requested data</returns>
        public bool TryGet<T>(string key, out T value)
        {
            if (PlayerPrefs.HasKey(key))
            {
                var jsonString = PlayerPrefs.GetString(key);
                if (typeof(T) == typeof(int) || typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(bool) || typeof(T) == typeof(string))
                {
                    value = (T)Convert.ChangeType(jsonString, typeof(T));
                }
                else
                {
                    value = JsonConvert.DeserializeObject<T>(jsonString, _serializerSettings);
                }
                
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Updates the cached save with the given data
        /// </summary>
        /// <typeparam name="T">Type of the requested data class</typeparam>
        /// <param name="key">Key which holds the data</param>
        /// <param name="value">Data which is going to be saved under the key</param>
        public void Commit<T>(string key, T value)
        {
            string jsonString;
            if (value is int || value is float || value is double || value is bool || value is string)
            {
                jsonString = value.ToString();
            }
            else
            {
                jsonString = JsonConvert.SerializeObject(value, _serializerSettings);
            }
                 
            PlayerPrefs.SetString(key, jsonString);
            PlayerPrefs.Save();
        }

        public async UniTask CommitBatch(Dictionary<string, object> values)
        {
            foreach (var kvp in values)
            {
                Commit(kvp.Key, kvp.Value);
            }
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Deletes all saved data
        /// </summary>
        public async UniTask DeleteAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            await UniTask.CompletedTask;
        }

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        #endregion
    }
}