using System;
using Cysharp.Threading.Tasks;
using Fusion.Photon.Realtime;
using Opencoding.CommandHandlerSystem;
using Stratton.AppReloading;
using Stratton.Core;
using Stratton.Debugging.UI;
using System.IO;
using Player.Config;
using Stratton.Configuration;
using UnityEngine;

namespace Shmackle.Debugging
{
    public class DebugConsoleCommands : MonoBehaviour
    {
        private DebugTools _debugTools;
        private AppReloader _appReloader;
        private string ConfigPath => Path.Combine(Application.persistentDataPath, _playerLocomotionConfig.GetType().Name + ".json");

        [SerializeField] private PlayerLocomotionConfig _playerLocomotionConfig;
        [SerializeField] private BootyJiggleConfig _bootyJiggleConfig;
        [SerializeField] private DoubleJumpConfig _doubleJumpConfig;
        [SerializeField] private ThrowingConfig _throwingConfig;
        [SerializeField] private TurningConfig _turningConfig;
        [SerializeField] private MouthConfig _mouthConfig;
        [SerializeField] private FlashlightConfig _flashlightConfig;
        
#if DEVELOPMENT_BUILD || UNITY_EDITOR || DEBUG

        private void Awake()
        {
            CommandHandlers.RegisterCommandHandlers(this);
        }

        private void Start()
        {
            _debugTools = AppServicesManager.Instance.Get<DebugTools>();
            _appReloader = AppServicesManager.Instance.Get<AppReloader>();
        }

        [CommandHandler]
        public void ReloadGame()
        {
            _appReloader.Reload().Forget();
        }

        [CommandHandler]
        public void ShowFPS()
        {
            if (_debugTools.DebugConsole != null)
            {
                _debugTools.DebugConsole.ShowFPSCounter();
            }
        }

        [CommandHandler]
        public void HideFPS()
        {
            if (_debugTools.DebugConsole != null)
            {
                _debugTools.DebugConsole.HideFPSCounter();
            }
        }

        [CommandHandler]
        public void OutputConfigFile()
        {
            var json = JsonUtility.ToJson(_playerLocomotionConfig);
            File.WriteAllText(ConfigPath, json);
        }

        [CommandHandler]
        public void ReadConfigFile()
        {
            var json = File.ReadAllText(ConfigPath);
            JsonUtility.FromJsonOverwrite(json, _playerLocomotionConfig);
            _playerLocomotionConfig.ConfigUpdated.Invoke();

            var _commandSimulatorContainer = FindAnyObjectByType<UICommandSimulatorContainer>();
            if (!_commandSimulatorContainer) return;
            _commandSimulatorContainer.CreateSimulatedCommandWidgets(); //update debug UI to match new values. 
        }

        [CommandHandler]
        public void SetFloatFieldOnScriptableObject(string fieldName, float value, string scriptableObjectName)
        {
            SetFieldOnScriptableObject(fieldName, value, scriptableObjectName);
        }

        [CommandHandler]
        public void SetIntFieldOnScriptableObject(string fieldName, int value, string scriptableObjectName)
        {
            SetFieldOnScriptableObject(fieldName, value, scriptableObjectName);
        }

        [CommandHandler]
        public void ChangeRegion(string regionName)
        {
            var regionCode = "";
            if (regionName != "Default")
            {
                regionCode = regionName.ToLower();
            }
            PhotonAppSettings.Global.AppSettings.FixedRegion = regionCode;
            _appReloader.Reload().Forget();
        }

        [CommandHandler]
        public void ToggleDebugInfo()
        {
            var runtimeInfo = _debugTools.DebugRuntimeInfo;
            if (runtimeInfo.gameObject.activeInHierarchy)
            {
                runtimeInfo.DisableDebugInfo();
            }
            else
            {
                runtimeInfo.gameObject.SetActive(true);
                runtimeInfo.EnableDebugInfo();
            }
        }

        [CommandHandler]
        public void SetBoolFieldOnScriptableObject(string fieldName, bool value, string scriptableObjectName)
        {
            SetFieldOnScriptableObject(fieldName, value, scriptableObjectName);
        }

        private void SetFieldOnScriptableObject<T>(string fieldName, T value, string scriptableObjectName)
        {
            // 1. Get the specific instance based on the string name
            var targetObject = GetScriptableObject(scriptableObjectName);

            if (targetObject == null)
            {
                Log.Error(BaseLogChannel.Debug, $"ScriptableObject '{scriptableObjectName}' could not be found.");
                return;
            }

            var type = targetObject.GetType();

            // 2. Find the field
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field == null)
            {
                Log.Error(BaseLogChannel.Debug, $"Field '{type.Name}.{fieldName}' does not exist.");
                return;
            }

            // 3. Set the value on the SPECIFIC instance (targetObject)
            field.SetValue(targetObject, value);

            // 4. Trigger the update event dynamically
            var updateEventProperty = type.GetProperty(nameof(IConfig.ConfigUpdated), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (updateEventProperty != null)
            {
                var action = updateEventProperty.GetValue(targetObject) as Action;
                action?.Invoke();
            }
        }

        private object GetScriptableObject(string scriptableObjectName)
        {
            return scriptableObjectName switch
            {
                _ when scriptableObjectName == _playerLocomotionConfig.name => _playerLocomotionConfig,
                _ when scriptableObjectName == _bootyJiggleConfig.name => _bootyJiggleConfig,
                _ when scriptableObjectName == _doubleJumpConfig.name => _doubleJumpConfig,
                _ when scriptableObjectName == _throwingConfig.name => _throwingConfig,
                _ when scriptableObjectName == _turningConfig.name => _turningConfig,
                _ when scriptableObjectName == _mouthConfig.name => _mouthConfig,
                _ when scriptableObjectName == _flashlightConfig.name => _flashlightConfig,
                _ => null
            };
        }
#endif
    }
}