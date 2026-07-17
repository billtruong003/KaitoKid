using System;
using System.Collections.Generic;
using System.Linq;
using Opencoding.Shared.Utils;
using UnityEngine;

namespace Opencoding.LogHistory
{
	public enum LogHistoryLogType
	{
		Error,
		Assert,
		Warning,
		Log,
		Exception,
		// Cutom log types for the console
		ConsoleInput
	}

	/// <summary>
	/// This class stores the log history. It has a hard-coded limit of 3K items before the old ones 
	/// start being recycled.
	/// </summary>
	public class LogHistory : IDisposable
	{
		private int _itemLimit = 3000;
		// This stores log items from the previous frame
		private readonly List<LogHistoryItem> _deferredLogItems = new List<LogHistoryItem>();

		// This is the list of all the log items
		private readonly List<LogHistoryItem> _logItems = new List<LogHistoryItem>();

		private DateTime _gameStartTime;

		public int InfoCount
		{
			get;
			private set;
		}

		public int WarningCount
		{
			get;
			private set;
		}

		public int ErrorCount
		{
			get;
			private set;
		}

		public int ExceptionCount
		{
			get;
			private set;
		}

		public int AssertCount
		{
			get;
			private set;
		}

		public DateTime GameStartTime
		{
			get { return _gameStartTime; }
		}

		private object _deferredLogItemListMutex = new object();

		private Action<LogHistoryItem> _logHistoryItemAdded = delegate { };
		public event Action<LogHistoryItem> LogHistoryItemAdded
		{
			add { _logHistoryItemAdded += value; }
			remove { _logHistoryItemAdded -= value; }
		}
		private Action<LogHistoryItem> _logHistoryItemRemoved = delegate { };
		public event Action<LogHistoryItem> LogHistoryItemRemoved
		{
			add { _logHistoryItemRemoved += value; }
			remove { _logHistoryItemRemoved -= value; }
		}
		private Action _logHistoryCleared = delegate { };
		public event Action LogHistoryCleared
		{
			add { _logHistoryCleared += value; }
			remove { _logHistoryCleared -= value; }
		}

		private static LogHistory _instance = null;

		public IList<LogHistoryItem> LogItems
		{
			get
			{
				return _logItems.AsReadOnly();
			}
		}

		public static LogHistory Instance
		{
			get
			{
				return _instance;
			}
		}

		public int ItemLimit
		{
			get { return _itemLimit; }
			set {
				lock (_deferredLogItemListMutex)
				{
					_itemLimit = value;
				} 
			}
		}

		public LogHistory()
		{
			if (_instance != null)
				throw new InvalidOperationException("DebugLogHistory is a singleton and has already been instantiated");

			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

			_gameStartTime = DateTime.Now;
			_instance = this;
    	
			LogHandler.RegisterLogCallback(HandleLoggingEvent);
		}

		public void Dispose()
		{
			if(_instance == this)
				_instance = null;

			LogHandler.UnRegisterLogCallback(HandleLoggingEvent);
		}

		public void Update()
		{
            if(_deferredLogItems.Count == 0)
                return;

            LogHistoryItem[] newLogHistoryItems = null;
	            
            lock (_deferredLogItemListMutex)
			{
				newLogHistoryItems = _deferredLogItems.ToArray();
				
				_logItems.AddRange(_deferredLogItems);

				foreach (var item in _deferredLogItems)
				{
					UpdateTypeCounter(item._Type, 1);
				}

				while (_logItems.Count > _itemLimit)
				{
					var firstItem = _logItems.First();
					_logHistoryItemRemoved(firstItem);

					UpdateTypeCounter(firstItem._Type, -1);

					_logItems.RemoveAt(0);
				}

				_deferredLogItems.Clear();
			}

		    foreach (var logItem in newLogHistoryItems)
		    {
		        _logHistoryItemAdded(logItem);
		    }
		}

		private void UpdateTypeCounter(LogHistoryLogType type, int change)
		{
			if (type == LogHistoryLogType.Log)
				InfoCount += change;
			else if (type == LogHistoryLogType.Warning)
				WarningCount += change;
			else if(type == LogHistoryLogType.Error)
				ErrorCount += change;
			else if (type == LogHistoryLogType.Exception)
				ExceptionCount += change;
			else if (type == LogHistoryLogType.Assert)
				AssertCount += change;
		}

		public void Clear()
		{
			InfoCount = 0;
			ErrorCount = 0;
			WarningCount = 0;
			AssertCount = 0;
			ExceptionCount = 0;

			lock (_deferredLogItemListMutex)
			{
				_deferredLogItems.Clear();
			}

			_logItems.Clear();

			_logHistoryCleared();
		}

		/// <summary>
		/// Removes logging related parts from a stack trace to make it easier to read.
		/// </summary>
		private string FilterStackTrace(string stackTrace)
		{
			var parts = stackTrace.Split('\n');
			bool isDebugLog = parts[0].StartsWith("Debug:Log");

			if (isDebugLog && parts[1].StartsWith("UnityAppender:Append"))
			{
				int i = 2;
				for (; i < parts.Length; ++i)
					if (parts[i].StartsWith("log4net.Core."))
						break;

				++i;

				return String.Join("\n", parts.SubArray(i, parts.Length - i));
			}
			else if (isDebugLog)
				return String.Join("\n", parts.SubArray(1, parts.Length - 1));

			return stackTrace.Trim();
		}

		public LogHistoryLogType ConvertFromUnityLogType(UnityEngine.LogType logType)
		{
			switch (logType)
			{
				case UnityEngine.LogType.Log:
					return LogHistoryLogType.Log;
				case UnityEngine.LogType.Warning:
					return LogHistoryLogType.Warning;
				case UnityEngine.LogType.Exception:
					return LogHistoryLogType.Exception;
				case UnityEngine.LogType.Assert:
					return LogHistoryLogType.Assert;
				case UnityEngine.LogType.Error:
					return LogHistoryLogType.Error;
				default:
					throw new InvalidOperationException("Unrecognised log type " + logType);
			}
		}

		/// <summary>
		/// This callback is called by Unity whenever there's a log message.
		/// </summary>
		private void HandleLoggingEvent(string logString, string stackTrace, UnityEngine.LogType type)
		{
			InternalHandleLoggingEvent(logString, stackTrace, ConvertFromUnityLogType(type));
		}

		private void InternalHandleLoggingEvent(string logString, string stackTrace, LogHistoryLogType type)
		{
			// Time.realtimeSinceStartup lies and goes backwards
			float timeSinceGameStart = DateTime.Now.Subtract(_gameStartTime).Ticks / (float)TimeSpan.TicksPerSecond;

			var logItem = new LogHistoryItem(type, logString, timeSinceGameStart, FilterStackTrace(stackTrace));
			lock (_deferredLogItemListMutex)
			{
				_deferredLogItems.Add(logItem);
			}
		}

		/// <summary>
		/// Log a message as an exception. Will cause the console to open if that's the current behaviour.
		/// </summary>
		/// <param name="message"></param>
		public void LogException(string message)
		{
			string stackTrace = Environment.StackTrace;
			InternalHandleLoggingEvent(message, stackTrace, LogHistoryLogType.Exception);
		}

		/// <summary>
		/// This logs a message to the console, without going through Debug.Log.
		/// </summary>
		public void LogMessage(string message, LogHistoryLogType type = LogHistoryLogType.Log)
		{
			InternalHandleLoggingEvent(message, "", type);
		}

		public void LogMessage(string message, string stacktrace, LogHistoryLogType type = LogHistoryLogType.Log)
		{
			InternalHandleLoggingEvent(message, stacktrace, type);
		}

		private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Debug.LogException((Exception)e.ExceptionObject);
		}
	}
}