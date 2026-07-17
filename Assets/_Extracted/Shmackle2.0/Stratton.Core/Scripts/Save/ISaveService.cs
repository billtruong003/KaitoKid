using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Stratton.Core;

namespace Stratton.Save
{
    public interface ISaveService
    {
        bool IsReady { get; }

        UniTask<InitializationResult> Init(SaveSystem saveSystem);
        void DeInit();
        /// <summary>
        /// Fetches data from the cached data by a key.
        /// </summary>
        /// <typeparam name="T">Type of the requested data class</typeparam>
        /// <param name="key">Key which holds the data</param>
        /// <returns>The requested data</returns>
        public bool TryGet<T>(string key, out T value);
        /// <summary>
        /// Updates the cached save with the given data
        /// </summary>
        /// <typeparam name="T">Type of the requested data class</typeparam>
        /// <param name="key">Key which holds the data</param>
        /// <param name="value">Data which is going to be saved under the key</param>
        public void Commit<T>(string key, T value);
        /// <summary>
        /// Updates the cached saves with the given datas
        /// </summary>
        /// <param name="values">Data which is going to be saved under the key</param>
        /// <returns></returns>
        public UniTask CommitBatch(Dictionary<string, object> values);
        /// <summary>
        /// Deletes all saved data
        /// </summary>
        public UniTask DeleteAll();
        /// <summary>
        /// Deletes saved data by key
        /// </summary>
        /// <param name="key">Key which holds the data</param>
        public void DeleteKey(string key);
    }
}