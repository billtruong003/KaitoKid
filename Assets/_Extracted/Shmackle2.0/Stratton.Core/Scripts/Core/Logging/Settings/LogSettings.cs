using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace Stratton.Core
{
    public class LogSettings : ScriptableObject
    {
        [Serializable]
        public class ChannelSettingsDictionary : SerializedDictionary<string, Log.ChannelSettings> { }

        public const string SettingsFileName = "LogSettings";
        public const string SettingsDirectory = "Settings/Resources";

        private static LogSettings _instance;

        [SerializeField] private bool _isLoggingInRuntimeDisabled;
        [SerializeField] private ChannelSettingsDictionary _channelSettings = new ChannelSettingsDictionary();

        public static LogSettings Instance
        {
            get
            {
                return _instance != null ? _instance : (_instance = Create());
            }
        }

        public bool IsLoggingInRuntimeDisabled => _isLoggingInRuntimeDisabled;

        private static LogSettings Create()
        {
            return Resources.Load<LogSettings>(SettingsFileName);
        }

        public void Refresh()
        {
            List<Type> types = new List<Type>();
            List<FieldInfo> fields = new List<FieldInfo>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                types.AddRange(asm.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && t.GetInterfaces().Contains(typeof(ILogChannelList))));
            }
            foreach (var type in types)
            {
                fields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(Log.Channel)));
            }
            List<string> channelsToRemove = new List<string>();
            foreach (var channel in _channelSettings)
            {
                if (!fields.Any(f => f.Name == channel.Key))
                {
                    channelsToRemove.Add(channel.Key);
                }
            }
            foreach (var channel in channelsToRemove)
            {
                _channelSettings.Remove(channel);
            }
            foreach (var field in fields)
            {
                var channel = (Log.Channel)field.GetValue(null);
                if (!_channelSettings.ContainsKey(channel.Name))
                {
                    _channelSettings.Add(channel.Name, new Log.ChannelSettings(Log.Target.Log));
                }
            }
        }

        public void Save()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        public bool IsChannelLogsToTarget(Log.Channel channel, Log.Target target)
        {
            if (_channelSettings.TryGetValue(channel.Name, out Log.ChannelSettings settings))
            {
                return (settings.Target & target) == target;
            }
            return false;
        }

        public void SetIsLoggingInRuntimeDisabled(bool isLoggingInRuntimeDisabled)
        {
            _isLoggingInRuntimeDisabled = isLoggingInRuntimeDisabled;
            if (!Application.isPlaying)
            {
                Save();
            }
            Log.Message(BaseLogChannel.Core, $"IsLoggingInRuntimeDisabled: {_isLoggingInRuntimeDisabled}");
        }
    }
}