using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Stratton.Core;
using Stratton.Data;
using MessagePipe;
using Newtonsoft.Json;

namespace Stratton.Save
{
    public interface ISaveController
    {
        void Init();
    }

    public class SaveSystem : GameSystemBase
    {
        #region Fields

        protected ISaveService _service;
        protected Dictionary<string, object> _dataToSave = new Dictionary<string, object>();
        protected bool _isBatchSavingInProgress;
        protected CancellationTokenSource _intervalSavingCancellationTokenSource;

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builtinContainerBuilder)
        {
        }

        public override async UniTask<InitializationResult> Init()
        {
            if (_service != null)
            {
                var result = await _service.Init(this);
                if (result.IsError)
                {
                    return InitializationResult.Error(result.ErrorCode, "Can't initialize the service!");
                }
                if (result.IsCancelled)
                {
                    Log.Warning(BaseLogChannel.Save, "Initialization cancelled of {0}", GetType());
                    return InitializationResult.Cancelled;
                }
                if (result.IsPaused)
                {
                    Log.Warning(BaseLogChannel.Save, "Initialization paused of {0}", GetType());
                    return InitializationResult.Paused;
                }
            }
            _intervalSavingCancellationTokenSource = new CancellationTokenSource();
            IntervalSaving(_intervalSavingCancellationTokenSource.Token).Forget();
            IsReady = true;
            return InitializationResult.Success;
        }

        public override async UniTask<DeinitializationResult> DeInit()
        {
            _intervalSavingCancellationTokenSource?.Cancel();
            while (_isBatchSavingInProgress)
            {
                await UniTask.Yield();
            }
            _service.DeInit();
            IsReady = false;
            return DeinitializationResult.Success;
        }

        public virtual JsonSerializerSettings GetJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = StrattonJsonContractResolver.Instance,
                Converters =
                {
                    new UnityTypeConverter(),
                    new DictionaryUnityTypeConverter(),
                }
            };
        }

        /// <summary>
        /// Fetches data from the cached data by a key.
        /// </summary>
        /// <typeparam name="T">Type of the requested data class</typeparam>
        /// <param name="key">Key which holds the data</param>
        /// <returns>The requested data</returns>
        public bool TryGet<T>(string key, out T value)
        {
            return _service.TryGet<T>(key, out value);
        }

        /// <summary>
        /// Updates the cached save with the given data
        /// </summary>
        /// <typeparam name="T">Type of the requested data class</typeparam>
        /// <param name="key">Key which holds the data</param>
        /// <param name="value">Data which is going to be saved under the key</param>
        public void Commit<T>(string key, T value)
        {
            _service.Commit<T>(key, value);
            Log.Message(BaseLogChannel.Save, $"Commited changes for key: {key}");
        }

        public void AddToNextInterval<T>(string key, T value)
        {
            _dataToSave.TryAdd(key, value);
        }

        public void RemoveFromInterval(string key)
        {
            _dataToSave.Remove(key);
        }

        public void DeleteKey(string key)
        {
            _service.DeleteKey(key);
            Log.Message(BaseLogChannel.Save, $"Deleted data for key: {key}");
        }

        /// <summary>
        /// Deletes all saved data
        /// </summary>
        public async UniTask DeleteAll()
        {
            while (_isBatchSavingInProgress)
            {
                await UniTask.Yield();
            }
            await _service.DeleteAll();
            Log.Message(BaseLogChannel.Save, $"Deleted all saved data!");
        }

        #endregion

        #region Private Methods

        protected virtual void Awake()
        {
            CheckAndFindService();
        }

        protected virtual ISaveService CheckAndFindService()
        {
            if (SaveSettings.Instance.CurrentSaveHostType == BaseDataHostType.None)
            {
                return default;
            }
            if (_service != null)
            {
                return _service;
            }
            List<Type> types = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                types.AddRange(asm.GetTypes().Where(t =>
                    !t.IsInterface &&
                    !t.IsAbstract &&
                    t.GetInterfaces().Contains(typeof(ISaveService)) &&
                    t.GetCustomAttributes(typeof(DataHostTarget), true).Length > 0 &&
                    ((DataHostTarget)t.GetCustomAttributes(typeof(DataHostTarget), true)[0]).HostType == SaveSettings.Instance.CurrentSaveHostType));
            }
            if (types.Count == 0)
            {
                Log.Error(BaseLogChannel.Save, "Can't find service " + typeof(ISaveService).FullName);
                return default;
            }
            _service = (ISaveService)Activator.CreateInstance(types[0]);
            return _service;
        }

        protected async UniTask IntervalSaving(CancellationToken cancellationToken)
        {
            while (true)
            {
                await UniTask.Delay(SaveSettings.Instance.SaveIntervalInMiliseconds, cancellationToken: cancellationToken);
                if (_dataToSave.Count > 0 && !_isBatchSavingInProgress)
                {
                    await SaveBatch();
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        protected async UniTask SaveBatch()
        {
            _isBatchSavingInProgress = true;
            try
            {
                await _service.CommitBatch(_dataToSave);
                Log.Message(BaseLogChannel.Save, $"Batch of data saved");
                _dataToSave.Clear();
            }
            finally
            {
                _isBatchSavingInProgress = false;
            }
        }

        #endregion

        #region SaveAttribute Methods

        public PersistentValue<T> GetPersistentValue<T>(Expression<Func<PersistentValue<T>>> fieldExpression, T baseValue)
        {
            var saveAttribute = GetSaveAttribute(fieldExpression, out var memberExpression, out var fieldInfo);
            var key = GetSaveKey(saveAttribute, memberExpression, fieldInfo);
            return new PersistentValue<T>(this, key, saveAttribute.SavePattern, baseValue);
        }

        private SaveAttribute GetSaveAttribute<T>(Expression<Func<T>> fieldExpression, out MemberExpression memberExpression, out FieldInfo fieldInfo)
        {
            if (fieldExpression.Body is MemberExpression memberExpr &&
                memberExpr.Member is FieldInfo memberFieldInfo)
            {
                var attribute = memberFieldInfo.GetCustomAttribute<SaveAttribute>();

                if (attribute == null)
                {
                    throw new ArgumentException("Invalid field expression");
                }
                memberExpression = memberExpr;
                fieldInfo = memberFieldInfo;
                return attribute;
            }

            throw new ArgumentException("Invalid field expression");
        }

        private string GetSaveKey(SaveAttribute saveAttribute, MemberExpression memberExpression, FieldInfo fieldInfo)
        {
            if (string.IsNullOrEmpty(saveAttribute.Key))
            {
                return fieldInfo.Name;
            }

            var instance = (memberExpression.Expression as ConstantExpression)?.Value;
            var method = instance?.GetType().GetMethod(saveAttribute.Key,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method != null && method.ReturnType == typeof(string))
            {
                return (string)method.Invoke(instance, null);
            }

            return saveAttribute.Key;
        }

        #endregion
    }
}