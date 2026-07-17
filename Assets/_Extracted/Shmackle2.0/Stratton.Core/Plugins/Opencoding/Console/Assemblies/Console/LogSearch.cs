using System;
using System.Collections.Generic;
using Opencoding.LogHistory;
using UnityEngine;

namespace Opencoding.Console
{
	class LogSearch
	{
		private FilteredLogHistoryViewModel _filteredLogHistoryViewModel;
		private readonly List<LogHistoryItem> _searchResults = new List<LogHistoryItem>();
		private readonly HashSet<LogHistoryItem> _searchResultsHashSet = new HashSet<LogHistoryItem>();
		private string _searchString = "";

		public Action<LogHistoryItem> CurrentResultChanged = delegate { };

		public bool Enabled
		{
			get;
			set;
		}

		public bool IsSearchActive
		{
			get
			{
				return Enabled && _searchString != "";
			}
		}

		public List<LogHistoryItem> Results
		{
			get
			{
				return _searchResults;
			}
		}

		private int _currentResultIndex = -1;
		public int CurrentResultIndex
		{
			get { return _currentResultIndex; }
			set 
			{
				if (value > _searchResults.Count - 1)
					throw new InvalidOperationException(string.Format("Invalid search result index: {0} Max: {1}", value, _searchResults.Count - 1));
				_currentResultIndex = value;
				CurrentResultChanged(CurrentResult);
			}
		}

		public LogHistoryItem CurrentResult
		{
			get
			{
				if (CurrentResultIndex < 0)
					return null;

				return Results[CurrentResultIndex];
			}
		}

		public LogSearch(FilteredLogHistoryViewModel filteredLogHistoryViewModel)
		{
			_filteredLogHistoryViewModel = filteredLogHistoryViewModel;
			_filteredLogHistoryViewModel.LogHistoryItemAdded += CheckIfItemMatchesSearch;
			_filteredLogHistoryViewModel.LogHistoryItemRemoved += RemoveLogHistoryItem;
			_filteredLogHistoryViewModel.LogHistoryListReset += RunSearchFromScratch;
		}

		private void RemoveLogHistoryItem(LogHistoryItem logHistoryItem)
		{
			_searchResults.Remove(logHistoryItem);
			_searchResultsHashSet.Remove(logHistoryItem);
		}

		private void CheckIfItemMatchesSearch(LogHistoryItem logHistoryItem)
		{
			if (!IsSearchActive)
				return;

			AddItemIfMatchesSearch(logHistoryItem);
		}

		public string SearchString
		{
			set
			{
				if (value == _searchString)
					return;

				_searchString = value;

				RunSearchFromScratch();
			}
			get { return _searchString; }
		}

		public void RunSearchFromScratch()
		{
			_searchResults.Clear();
			_searchResultsHashSet.Clear();
			CurrentResultIndex = -1;

			if (!IsSearchActive)
				return;

			foreach (var item in _filteredLogHistoryViewModel.LogHistoryItems)
			{
				AddItemIfMatchesSearch(item);
			}
		}

		private void AddItemIfMatchesSearch(LogHistoryItem item)
		{
			if (!DoesItemMatchSearchString(item)) 
				return;

			_searchResults.Add(item);
			_searchResultsHashSet.Add(item);
		}

		private bool DoesItemMatchSearchString(LogHistoryItem item)
		{
			return item._LogMessage.IndexOf(_searchString, StringComparison.CurrentCultureIgnoreCase) >= 0;
		}

		public bool IsLogHistoryItemASearchResult(LogHistoryItem item)
		{
			return _searchResultsHashSet.Contains(item);
		}

		public void NextSearchResult()
		{
			if (CurrentResultIndex == Results.Count - 1)
				CurrentResultIndex = 0;
			else
				CurrentResultIndex++;
		}

		public void PreviousSearchResult()
		{
			if (CurrentResultIndex == 0)
				CurrentResultIndex = Results.Count - 1;
			else
				CurrentResultIndex--;
		}
	}
}