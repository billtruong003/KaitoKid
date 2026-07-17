namespace Stratton.Core
{
    using Stratton.CI;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    public static class Log
    {
        private static bool _isInit;
        private static CancellationTokenSource _cancellationTokenSource;
        private static CancellationTokenSource _previous;
        private static Queue<string> _textToFileQueue = new Queue<string>();
        private static StringBuilder _sb = new StringBuilder();
        private static string _fileName;
#if UNITY_EDITOR
        private static string _folderPath = Directory.GetParent(Application.dataPath) + $"/CustomLogs";
#elif LOG_TO_FILE
        private static string _folderPath = Application.persistentDataPath;
#endif

        internal static void Init()
        {
#if LOG_TO_FILE || UNITY_EDITOR
            InitializeWriteToFile();
#endif
        }

        static Log()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            LogBaseInformation();
#endif
#if LOG_TO_FILE || UNITY_EDITOR
            InitializeWriteToFile();
#endif
        }

        private static void LogBaseInformation()
        {
            StringBuilder sb = new StringBuilder();
            if (BuildSettings.Instance != null)
            {
                sb.Append($"Version: {BuildSettings.Instance.ReleaseVersion}, ");
                sb.Append($"Stage: {BuildSettings.Instance.BuildStage}, ");
                sb.Append($"Branch name: {BuildSettings.Instance.RepoBranch}, ");
                sb.Append($"Revision: {BuildSettings.Instance.RepoRevision}");
            }
            Debug.Log(sb.ToString());
        }

        private static void InitializeWriteToFile()
        {
            if (_isInit)
            {
                return;
            }
            _fileName = $"###LogFile_{DateTime.Today.Month}_{DateTime.Today.Day}_{DateTime.Today.Year}___{DateTime.Now.Hour}_{DateTime.Now.Minute}_{DateTime.Now.Second}.txt";
            if (_cancellationTokenSource != null)
            {
                _previous = _cancellationTokenSource;
            }
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (true)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        break;
                    }
                    if (_cancellationTokenSource == null)
                    {
                        break;
                    }
                    var anythingToSend = false;
                    lock (_textToFileQueue)
                    {
                        if (_textToFileQueue.Count > 0)
                        {
                            _sb.Clear();
                            for (int i = 0; i < _textToFileQueue.Count; i++)
                            {
                                var text = _textToFileQueue.Dequeue();
                                _sb.AppendLine(text);
                            }
                            anythingToSend = true;

                        }
                    }
                    if (anythingToSend)
                    {
                        await WriteToFile(_sb.ToString(), _cancellationTokenSource.Token);
                    }
                }
            }, _cancellationTokenSource.Token);
            _isInit = true;
        }

        public class Channel
        {
            private string _name;

            public Channel(string name)
            {
                _name = name;
            }

            public string Name => _name;
        }

        [Flags]
        public enum Target
        {
            None = 0,
            Log = 1,
            File = 2,
        }

        [Serializable]
        public class ChannelSettings
        {
            [SerializeField]
            private Target _target;
            public Target Target => _target;
            public ChannelSettings(Target target)
            {
                _target = target;
            }
        }

        public static void DeInit()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
            if (_previous != null)
            {
                _previous.Dispose();
                _previous = null;
            }
            _isInit = false;
        }
        private static void SendLogToFile(string logTag, Channel channel, string log, params object[] par)
        {
#if LOG_TO_FILE || UNITY_EDITOR
            var baseLog = string.Format("[{0}] [{1}] {2}", logTag, channel.Name, log);
            if (par.Length == 0)
            {
                lock (_textToFileQueue)
                {
                    _textToFileQueue.Enqueue(baseLog);
                }
            }
            else
            {
                var parametrized = string.Format(baseLog, par);
                lock (_textToFileQueue)
                {
                    _textToFileQueue.Enqueue(parametrized);
                }
            }
#endif
        }

        private static void SendLogToFile(string logTag, string log, string stackTrace, params object[] par)
        {
#if LOG_TO_FILE || UNITY_EDITOR
            var baseLog = string.Format("[{0}] {1}\n{2}", logTag, log, stackTrace);
            if (par.Length == 0)
            {
                lock (_textToFileQueue)
                {
                    _textToFileQueue.Enqueue(baseLog);
                }
            }
            else
            {
                var parametrized = string.Format(baseLog, par);
                lock (_textToFileQueue)
                {
                    _textToFileQueue.Enqueue(parametrized);
                }
            }
#endif
        }

        private static void Message(string message, params object[] par)
        {
#if !UNITY_EDITOR
            if (LogSettings.Instance.IsLoggingInRuntimeDisabled)
            {
                return;
            }
#endif
            // Messages shouldn't be shown in release unless the channel is included in all
            if (par.Length == 0)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogFormat(message, par);
            }
        }

        public static void Message(Channel channel, string message, params object[] par)
        {
            message = string.Concat("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff") + "]" + " ", message);
#if !UNITY_EDITOR
            if (LogSettings.Instance.IsLoggingInRuntimeDisabled)
            {
                return;
            }
#endif
            if (LogSettings.Instance.IsChannelLogsToTarget(channel, Target.Log))
            {
                if (par.Length == 0)
                {
                    Debug.Log(string.Format("[{0}] {1}", channel.Name, message));
                }
                else
                {
                    Debug.LogFormat(string.Format("[{0}] {1}", channel.Name, message), par);
                }
            }
            if (LogSettings.Instance.IsChannelLogsToTarget(channel, Target.File))
            {
                SendLogToFile("LOG", channel, message, par);
            }
        }

        public static void Warning(string warning, params object[] par)
        {
#if !UNITY_EDITOR
            if (LogSettings.Instance.IsLoggingInRuntimeDisabled)
            {
                return;
            }
#endif
            // Warnings shouldn't be shown in release unless the channel is included in all
            if (par.Length == 0)
            {
                Debug.LogWarning(warning);
            }
            else
            {
                Debug.LogWarningFormat(warning, par);
            }
        }

        public static void Warning(Channel channel, string warning, params object[] par)
        {
            warning = string.Concat("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff") + "]" + " ", warning);
#if !UNITY_EDITOR
            if (LogSettings.Instance.IsLoggingInRuntimeDisabled)
            {
                return;
            }
#endif
            if (LogSettings.Instance.IsChannelLogsToTarget(channel, Target.Log))
            {
                if (par.Length == 0)
                {
                    Debug.LogWarning(string.Format("[{0}] {1}", channel.Name, warning));
                }
                else
                {
                    Debug.LogWarningFormat(string.Format("[{0}] {1}", channel.Name, warning), par);
                }
            }
            if (LogSettings.Instance.IsChannelLogsToTarget(channel, Target.File))
            {
                SendLogToFile("WARNING", channel, warning, par);
            }
        }

        public static void Error(Channel channel, string error, params object[] par)
        {
            error = string.Concat("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff") + "]" + " ", error);
            if (LogSettings.Instance.IsChannelLogsToTarget(channel, Target.Log))
            {
                if (par.Length == 0)
                {
                    Debug.LogError(string.Format("[{0}] {1}", channel.Name, error));
                }
                else
                {
                    Debug.LogErrorFormat(string.Format("[{0}] {1}", channel.Name, error), par);
                }
            }
            if (LogSettings.Instance.IsChannelLogsToTarget(channel, Target.File))
            {
                SendLogToFile("ERROR", channel, error, par);
            }
        }
        private static void Error(string error, params object[] par)
        {
            if (par.Length == 0)
            {
                Debug.LogError(error);
            }
            else
            {
                Debug.LogErrorFormat(error, par);
            }
        }

        public static void ReleaseMessage(string message, params object[] par)
        {
#if !UNITY_EDITOR
            if (LogSettings.Instance.IsLoggingInRuntimeDisabled)
            {
                return;
            }
#endif
            if (par.Length == 0)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogFormat(message, par);
            }
        }

        public static void ReleaseMessage(Channel channel, string message, params object[] par)
        {
            ReleaseMessage(string.Format("[{0}] {1}", channel.Name, message), par);
        }

        private async static Task WriteToFile(string text, CancellationToken ct)
        {
            try
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }
#if LOG_TO_FILE || UNITY_EDITOR
                if (!Directory.Exists(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);
                }

                var combinedPath = Path.Combine(_folderPath, _fileName);
                using (StreamWriter sw = File.AppendText(combinedPath))
                {
                    await sw.WriteAsync(text);
                }
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Something went wrong {e.Message}");
            }
        }
    }
}