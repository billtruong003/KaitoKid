using System;
using UnityEngine;

namespace Stratton.AppReloading
{
    public abstract class AppReloadSource : MonoBehaviour
    {
        protected Action _onReloadCallback;
        protected bool _isInit;
        protected AppReloadSettings _settings;

        public virtual void Init(Action onReload, AppReloadSettings settings)
        {
            _onReloadCallback = onReload;
            _settings = settings;
            InitializeSettings();
            _isInit = true;
        }

        public virtual void DeInit()
        {
            _onReloadCallback = null;
            _isInit = false;
        }

        protected abstract void InitializeSettings();

        protected virtual void OnDestroy()
        {
            DeInit();
        }
    }
}