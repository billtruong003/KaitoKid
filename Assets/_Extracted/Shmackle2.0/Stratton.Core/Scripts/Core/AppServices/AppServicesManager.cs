using System;
using System.Collections.Generic;
using MessagePipe;
using UnityEngine;

namespace Stratton.Core
{
    public class AppServicesManager : Singleton<AppServicesManager>
    {
        #region Serialized Fields

        [SerializeField] private AppServiceBase[] _appServices;

        #endregion

        #region Fields

        protected IPublisher<AppQuitEvent> _appQuitEventPublisher;
        protected IPublisher<AppFocusedEvent> _appFocusedEventPublisher;
        protected IPublisher<AppPausedEvent> _appPausedEventPublisher;

        protected readonly Dictionary<Type, AppServiceBase> _appServicesByType = new Dictionary<Type, AppServiceBase>();

        #endregion

        #region Public Properties

        public bool IsReady { protected set; get; }

        #endregion

        #region Public Methods

        public virtual T Get<T>() where T : AppServiceBase
        {
            if (_appServicesByType.TryGetValue(typeof(T), out var gs))
            {
                return (T)gs;
            }
            foreach (var appService in _appServices)
            {
                if (appService is T)
                {
                    _appServicesByType.Add(typeof(T), appService);
                    return (T)appService;
                }
            }
            return default;
        }

        public void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            int index = 0;
            foreach (var appService in _appServices)
            {
                if (!appService)
                {
                    Log.Warning(BaseLogChannel.Debug, $"App service at index {index} is null!");
                    continue;
                }
                appService.InstallMessageBrokers(builder);
                index++;
            }
            builder.AddMessageBroker<AppQuitEvent>();
            builder.AddMessageBroker<AppFocusedEvent>();
            builder.AddMessageBroker<AppPausedEvent>();
        }

        public virtual void Init()
        {
            if (IsReady)
            {
                Log.Message(BaseLogChannel.Core, "App services are already initialized!");
                return;
            }
            Log.Message(BaseLogChannel.Core, "Initializing app services...");
            foreach (var appService in _appServices)
            {
                if (!appService) continue;
                appService.Init();
            }

            _appQuitEventPublisher = GlobalMessagePipe.GetPublisher<AppQuitEvent>();
            _appFocusedEventPublisher = GlobalMessagePipe.GetPublisher<AppFocusedEvent>();
            _appPausedEventPublisher = GlobalMessagePipe.GetPublisher<AppPausedEvent>();

            IsReady = true;
            Log.Message(BaseLogChannel.Core, $"App services initialization completed!");
        }

        public virtual void DeInit()
        {
            Log.Message(BaseLogChannel.Core, "Deinitializing app services...");
            for (int i = _appServices.Length - 1; i >= 0; i--)
            {
                _appServices[i].DeInit();
            }
            IsReady = false;
            Log.Message(BaseLogChannel.Core, $"App services deinitialization completed!");
        }

        #endregion

        #region Private Methods

        protected virtual void OnApplicationQuit()
        {
            _appQuitEventPublisher?.Publish(new());
        }

        protected virtual void OnApplicationFocus(bool isFocused)
        {
            _appFocusedEventPublisher?.Publish(new() { IsFocused = isFocused });
        }

        protected virtual void OnApplicationPause(bool isPaused)
        {
            _appPausedEventPublisher?.Publish(new() { IsPaused = isPaused });
        }

        #endregion
    }
}