using System;
using Stratton.Core;

namespace Stratton.AppReloading
{
    public class OnApplicationFocusReloadSource : AppReloadSource
    {
        private float _waitSeconds = 30 * 60;// 30 minutes
        private DateTime _focusDisabledTime;

        protected override void InitializeSettings()
        {
            _focusDisabledTime = DateTime.Now;
            if (_settings != null)
            {
                _waitSeconds = _settings.OnFocusTimeInSeconds;
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!_isInit)
            {
                return;
            }
            Log.Message(BaseLogChannel.Core, $"Application focus changed {focus}");
            if (focus)
            {
                TimeSpan length = DateTime.Now - _focusDisabledTime;
                if (length.TotalSeconds > _waitSeconds)
                {
                    _onReloadCallback?.Invoke();
                }
            }
            else
            {
                _focusDisabledTime = DateTime.Now;
            }
        }
    }
}