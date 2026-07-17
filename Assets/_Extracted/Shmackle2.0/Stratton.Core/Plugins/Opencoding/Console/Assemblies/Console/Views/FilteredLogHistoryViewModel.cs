using System;
using System.Collections.Generic;
using System.Linq;
using Opencoding.LogHistory;
using UnityEngine;

namespace Opencoding.Console
{
	class FilteredLogHistoryViewModel : IDisposable
	{
		// This list is populated when a filter string is specified
		private readonly List<LogHistoryItem> _filteredLogItems = new List<LogHistoryItem>();

		private string _filterString = "";

		private bool _showInfos = true;
		private bool _showWarnings = true;
		private bool _showErrors = true;
		private bool _showExceptions = true;
		private bool _showAsserts = true;

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
		private Action _logHistoryListReset = delegate { };
		public event Action LogHistoryListReset
		{
			add { _logHistoryListReset += value; }
			remove { _logHistoryListReset -= value; }
		}

		// This is a reference to either the _filteredLogItems or the list of 
		// log items provided by the DebugLogHistory.
		private IList<LogHistoryItem> _currentItemSource = null;

		private LogHistory.LogHistory _logHistory = null;

		public string FilterString
		{
			get
			{
				return _filterString;
			}
			set
			{
				if (_filterString == value)
					return;

				_filterString = value;
				FilterLogItems();
			}
		}

		private bool _onlyMatchStringsThatStartWithFilterString;

		public bool OnlyMatchStringsThatStartWithFilterString
		{
			get
			{
				return _onlyMatchStringsThatStartWithFilterString;
			}
			set
			{
				if (_onlyMatchStringsThatStartWithFilterString == value)
					return;

				PlayerPrefs.SetInt("Opencoding.Console.OnlyMatchStringsThatStartWithFilterString", value ? 1 : 0);
				PlayerPrefs.Save();

				_onlyMatchStringsThatStartWithFilterString = value;
				FilterLogItems();
			}
		}

		public bool ShowInfos
		{
			get
			{
				return _showInfos;
			}
			set
			{
				if (_showInfos == value)
					return;

				_showInfos = value;
				FilterLogItems();
			}
		}

		public bool ShowWarnings
		{
			get
			{
				return _showWarnings;
			}
			set
			{
				if (_showWarnings == value)
					return;

				_showWarnings = value;
				FilterLogItems();
			}
		}

		public bool ShowErrors
		{
			get
			{
				return _showErrors;
			}
			set
			{
				if (_showErrors == value)
					return;

				_showErrors = value;
				FilterLogItems();
			}
		}

		public bool ShowExceptions
		{
			get
			{
				return _showExceptions;
			}
			set
			{
				if (_showExceptions == value)
					return;

				_showExceptions = value;
				FilterLogItems();
			}
		}

		public bool ShowAsserts
		{
			get { return _showAsserts; }
			set
			{
				if (_showAsserts == value)
					return;

				_showAsserts = value;
				FilterLogItems();
			}
		}

		public IList<LogHistoryItem> LogHistoryItems
		{
			get
			{
				return _currentItemSource;
			}
		}

		public bool IsFiltered
		{
			get
			{
				return _filterString != "" || !_showErrors || !_showInfos || !_showWarnings || !_showAsserts || !_showExceptions;
			}
		}

		public FilteredLogHistoryViewModel(LogHistory.LogHistory logHistory)
		{
			logHistory.LogHistoryItemAdded += AddLogItemToFilteredListIfNecessary;
			logHistory.LogHistoryItemRemoved += RemoveLogItemFromFilteredList;
			logHistory.LogHistoryCleared += FilterLogItems;
			_logHistory = logHistory;
			_currentItemSource = logHistory.LogItems;

			_onlyMatchStringsThatStartWithFilterString = PlayerPrefs.GetInt("Opencoding.Console.OnlyMatchStringsThatStartWithFilterString") == 1;
		}

        public void Dispose()
        {
            _logHistory.LogHistoryItemAdded -= AddLogItemToFilteredListIfNecessary;
            _logHistory.LogHistoryItemRemoved -= RemoveLogItemFromFilteredList;
            _logHistory.LogHistoryCleared -= FilterLogItems;
            _logHistory = null;
        }

		private void AddLogItemToFilteredListIfNecessary(LogHistoryItem historyItem)
		{
			if (IsFiltered && PassesFilter(historyItem))
			{
				_filteredLogItems.Add(historyItem);
				_logHistoryItemAdded(historyItem);
			}
			else if (!IsFiltered)
			{
				_logHistoryItemAdded(historyItem);
			}
		}

		private void RemoveLogItemFromFilteredList(LogHistoryItem historyItem)
		{
			if(IsFiltered && _filteredLogItems.Remove(historyItem))
				_logHistoryItemRemoved(historyItem);
			else if (!IsFiltered)
				_logHistoryItemRemoved(historyItem);
		}

		/// <summary>
		/// Go through all the items in the log and filter them into the filtered list based
		/// on whether they match the filter string.
		/// </summary>
		private void FilterLogItems()
		{
			_filteredLogItems.Clear();

			if (!IsFiltered)
			{
				_currentItemSource = _logHistory.LogItems;
				_logHistoryListReset();
				return;
			}

			foreach (var item in _logHistory.LogItems.Where(item => PassesFilter(item)))
			{
				_filteredLogItems.Add(item);
			}

			_currentItemSource = _filteredLogItems.AsReadOnly();
			_logHistoryListReset();
		}

		/// <summary>
		/// This checks to see if a particular log item passes the filter(s) that are set.
		/// </summary>
		private bool PassesFilter(LogHistoryItem historyItem)
		{
			if (!_showInfos && historyItem._Type == LogHistoryLogType.Log)
			{
				return false;
			}

			if (!_showWarnings && historyItem._Type == LogHistoryLogType.Warning)
			{
				return false;
			}

			if (!_showErrors && historyItem._Type == LogHistoryLogType.Error)
			{
				return false;
			}

			if (!_showAsserts && historyItem._Type == LogHistoryLogType.Assert)
			{
				return false;
			}

			if (!_showExceptions && historyItem._Type == LogHistoryLogType.Exception)
			{
				return false;
			}

			var matchIndex = historyItem._LogMessage.IndexOf(_filterString, StringComparison.CurrentCultureIgnoreCase);

			if (OnlyMatchStringsThatStartWithFilterString)
				return matchIndex == 0;

			return matchIndex >= 0;
		}

	}
}