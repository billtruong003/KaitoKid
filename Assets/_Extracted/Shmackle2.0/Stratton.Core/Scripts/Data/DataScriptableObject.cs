using Stratton.Core;
using UnityEngine;

namespace Stratton.Data
{
    public interface IDataSource
    {
    }

    public abstract class DataScriptableObject : ScriptableObject, IDataSource, IResettableOnExitPlay
    {
        #region Properties

        protected virtual IVersionableDataModel DataModel { get; set; }
        protected virtual IVersionableDataModel BackupDataModel { get; set; }

        protected abstract string PrefsKey { get; }

        #endregion

        #region Public Methods

        public virtual void ApplyDataModel(IVersionableDataModel dataModel)
        {
            DataModel = dataModel;
            Save();
        }

        public virtual void Save()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                BackupDataModel = DataModel;
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
            }
#endif
        }

        public virtual void SaveToPrefs(string jsonString)
        {
            PlayerPrefs.SetString(PrefsKey, jsonString);
        }

        public virtual void SaveToPrefs<T>() where T : IVersionableDataModel
        {
            PlayerPrefs.SetString(PrefsKey, ((T)DataModel).ToJsonString());
        }

        public virtual void LoadFromPrefs<T>() where T : IVersionableDataModel
        {
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                var jsonString = PlayerPrefs.GetString(PrefsKey);
                ApplyDataModel(VersionedDataModel.FromJsonString<T>(jsonString));
            }
        }

        public void ResetOnExitPlay()
        {
            DataModel = BackupDataModel;
        }

        #endregion

        #region Public Methods

        protected virtual void OnEnable()
        {
            BackupDataModel = DataModel;
        }

        #endregion
    }
}