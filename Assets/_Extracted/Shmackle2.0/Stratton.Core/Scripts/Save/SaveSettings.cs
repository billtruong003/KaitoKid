using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stratton.Core;
using Stratton.Data;
using UnityEngine;

namespace Stratton.Save
{
    public class SaveSettings : ScriptableObject
    {
        public const string SettingsFileName = "SaveSettings";
        public const string SettingsDirectory = "Settings/Resources";

        private static SaveSettings _instance;

        private readonly List<string> _saveHostTypes = new List<string>();

        #region Serialized Fields

        [SerializeField] private string _currentSaveHostType = BaseDataHostType.None;
        [SerializeField] private int _saveIntervalInMiliseconds = 1000;
        [SerializeField] private string _databaseName = "Save";

        #endregion

        #region Properties

        public static SaveSettings Instance
        {
            get
            {
                return _instance != null ? _instance : (_instance = Create());
            }
        }

        public List<string> SaveHostTypes => _saveHostTypes;
        public string CurrentSaveHostType
        {
            get
            {
                return _currentSaveHostType;
            }
            set
            {
                if (value != _currentSaveHostType)
                {
                    _currentSaveHostType = value;
                    Save();
                }
            }
        }

        public int SaveIntervalInMiliseconds => _saveIntervalInMiliseconds;
        public string DatabaseName => _databaseName;

        #endregion

        #region Public Methods

        public void Refresh()
        {
            _saveHostTypes.Clear();
            List<Type> hostTypes = new List<Type>();
            List<FieldInfo> sourceFields = new List<FieldInfo>();
            List<FieldInfo> hostFields = new List<FieldInfo>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                hostTypes.AddRange(asm.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IDataHostTypeList))));
            }
            foreach (var type in hostTypes)
            {
                hostFields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(string)));
            }
            foreach (var field in hostFields)
            {
                var hostType = (string)field.GetValue(null);
                _saveHostTypes.Add(hostType);
            }
        }

        public void Save()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        #endregion

        #region Private Methods

        private static SaveSettings Create()
        {
            return Resources.Load<SaveSettings>(SettingsFileName);
        }

        #endregion
    }
}