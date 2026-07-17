using System;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

namespace Stratton.Core
{
    public class LogSystem : GameSystemBase
    {
        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builtinContainerBuilder)
        {
        }

        public override async UniTask<InitializationResult> Init()
        {
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            Log.Init();
            IsReady = true;
            return InitializationResult.Success;
        }

        public override async UniTask<DeinitializationResult> DeInit()
        {
            Log.DeInit();
            IsReady = false;
            return DeinitializationResult.Success;
        }

        #endregion

        #region Private Methods

        void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Assert || type == LogType.Exception)
            {
                //Log.Error(BaseLogChannel.Exception, logString + Environment.NewLine + stackTrace);
            }
        }

        #endregion
    }
}