using System;
using Stratton.Core;

namespace Stratton.AppReloading
{
    public class OnApplicationPauseReloadSource : AppReloadSource
    {
        private float _waitSeconds = 30 * 60;// 30 minutes
        private DateTime _pauseTime;

        protected override void InitializeSettings()
        {
            if (_settings != null)
            {
                _waitSeconds = _settings.OnPauseTimeInSeconds;
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (!_isInit)
            {
                return;
            }
            Log.Message(BaseLogChannel.Core, $"Application paused {pause}");
            if (pause)
            {
                _pauseTime = DateTime.Now;
            }
            else
            {
                TimeSpan length = DateTime.Now - _pauseTime;
                if (length.TotalSeconds > _waitSeconds)
                {
                    _onReloadCallback?.Invoke();
                }
            }
        }
    }
}