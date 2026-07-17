using System;
using UnityEngine;

namespace Opencoding.LogHistory
{
	/// <summary>
	/// Each log message generated is stored as one of these.
	/// </summary>
	public class LogHistoryItem
	{
		public LogHistoryLogType _Type;
		public string _StackTrace;
		public string _LogMessage;
		public string _FirstLineOfLogMessage;
		public float _Time;
		public int _Id;
		private static int _Counter;

		public LogHistoryItem(LogHistoryLogType type, string message, float time, string stackTrace = "")
		{
			_Id = _Counter++;
			_Type = type;
			_LogMessage = message;
			_StackTrace = stackTrace;
			_FirstLineOfLogMessage = _LogMessage.Split(new char[] { '\n' }, 2)[0];
			_Time = time;
		    if (_FirstLineOfLogMessage.Length > 150)
		        _FirstLineOfLogMessage = _FirstLineOfLogMessage.Substring(0, 150);
		}
	}
}