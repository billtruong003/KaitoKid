using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stratton.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Stratton.Assets
{
    public class AssetsSettings : ScriptableObject
    {
        public const string SettingsFileName = "AssetsSettings";
        public const string SettingsDirectory = "Settings/Resources";

        private static AssetsSettings _instance;

        private readonly List<string> _assetsHostTypes = new List<string>();

        #region Serialized Fields

        [SerializeField] private string _currentAssetsHostType = BaseAssetsHostType.Local;
        [SerializeField] private AddressablesSceneList _addressablesSceneList;

        #endregion

        #region Properties

        public static AssetsSettings Instance
        {
            get
            {
                return _instance ?? (_instance = Create());
            }
        }

        public List<string> AssetsHostTypes => _assetsHostTypes;
        public string CurrentAssetsHostType
        {
            get 
            { 
                return _currentAssetsHostType; 
            }
            set 
            {
                if (value != _currentAssetsHostType)
                {
                    _currentAssetsHostType = value;
                    Save();
                }
            }
        }
        public AddressablesSceneList AddressablesSceneList => _addressablesSceneList;

        #endregion

        private static AssetsSettings Create()
        {
            return Resources.Load<AssetsSettings>(SettingsFileName);
        }

        public void Refresh()
        {
            _assetsHostTypes.Clear();
            List<Type> hostTypes = new List<Type>();
            List<FieldInfo> sourceFields = new List<FieldInfo>();
            List<FieldInfo> hostFields = new List<FieldInfo>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                hostTypes.AddRange(asm.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IAssetsHostTypeList))));
            }
            foreach (var type in hostTypes)
            {
                hostFields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(string)));
            }
            foreach (var field in hostFields)
            {
                var hostType = (string)field.GetValue(null);
                _assetsHostTypes.Add(hostType);
            }
        }

        public void Save()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
        
        public virtual AssetReference GetSceneAssetReference(string sceneName)
        {
            for (int i = 0; i < _addressablesSceneList.SceneAssetReferences.Length; i++)
            {
                if (sceneName == _addressablesSceneList.SceneAssetReferences[i].SceneName)
                {
                    return _addressablesSceneList.SceneAssetReferences[i].SceneReference;
                }
            }

            return null;
        }
    }
}