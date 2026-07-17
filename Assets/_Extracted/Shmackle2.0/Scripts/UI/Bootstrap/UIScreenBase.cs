using System;
using UnityEngine;

namespace Shmackle.UI
{
    [RequireComponent(typeof(Canvas))]
    public class UIScreenBase : MonoBehaviour
    {
        #region Fields

        private Canvas _canvas;

        protected IDisposable _eventsFullTimeBagDisposable;
        protected IDisposable _eventsScreenTimeBagDisposable;

        #endregion

        #region Public Methods

        public void Show()
        {
            _canvas.enabled = true;
            RegisterScreenTimeEvents();
        }

        public void Hide()
        {
            _canvas.enabled = false;
            UnregisterScreenTimeEvents();
        }

        #endregion

        #region Private Methods

        protected virtual void Awake()
        {
            Init();
            RegisterFullTimeEvents();
        }

        protected virtual void OnDestroy()
        {
            UnregisterFullTimeEvents();
            DeInit();
        }

        #endregion

        protected virtual void Init()
        {
            _canvas = GetComponent<Canvas>();
            Hide();
        }

        protected virtual void DeInit()
        {
        }

        protected virtual void RegisterFullTimeEvents() { }
        protected virtual void UnregisterFullTimeEvents()
        {
            _eventsFullTimeBagDisposable?.Dispose();
        }
        protected virtual void RegisterScreenTimeEvents() { }
        protected virtual void UnregisterScreenTimeEvents()
        {
            _eventsScreenTimeBagDisposable?.Dispose();
        }
    }
}