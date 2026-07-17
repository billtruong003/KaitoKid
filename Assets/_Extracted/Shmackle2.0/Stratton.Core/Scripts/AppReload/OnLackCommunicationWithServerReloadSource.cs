using System;
using System.Collections;
using MessagePipe;
using UnityEngine;

namespace Stratton.AppReloading
{
    public class OnLackCommunicationWithServerReloadSource : AppReloadSource
    {
        #region Fields

        protected IDisposable _eventsBagDisposable;

        private float _waitSeconds = 30 * 60;// 30 minutes
        private Coroutine _coroutine;

        #endregion

        #region Public Methods

        public override void Init(Action onReload, AppReloadSettings settings)
        {
            base.Init(onReload, settings);

            var bag = DisposableBag.CreateBuilder();

            // Subscribe to events here

            _eventsBagDisposable = bag.Build();
        }

        public override void DeInit()
        {
            if (!_isInit)
            {
                return;
            }
            StopCoroutine();
            _eventsBagDisposable?.Dispose();
            base.DeInit();
        }

        #endregion

        #region Private Methods

        protected override void InitializeSettings()
        {
            if (_settings != null)
            {
                _waitSeconds = _settings.NoCommunicationWithServerTimeInSeconds;
            }
            ResetCoroutine();
        }

        private IEnumerator CountTime()
        {
            yield return new WaitForSecondsRealtime(_waitSeconds);
            _onReloadCallback?.Invoke();
        }

        private void ResetCoroutine()
        {
            StopCoroutine();
            _coroutine = StartCoroutine(CountTime());
        }

        private void StopCoroutine()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }
        }

        #endregion
    }
}