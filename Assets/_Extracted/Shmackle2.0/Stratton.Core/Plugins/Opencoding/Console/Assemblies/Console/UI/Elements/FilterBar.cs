using System;
using System.Collections.Generic;
using Opencoding.LogHistory;
using UnityEngine;

namespace Opencoding.Console
{
	/// <summary>
	/// The filter bar is the section that appears to allow you to filter/search the console.
	/// </summary>
	class FilterBar
	{
		private bool _isVisible = false;
		public bool IsVisible
		{
			get
			{
				return _isVisible;
			}
			set
			{
				if (value == _isVisible)
					return;

				_isVisible = value;
				if (_isVisible)
				{
					_logSearch.Enabled = true;
					_justMadeVisible = true;
					
					_filterInputField.IsDone = false;
					_filterInputField.WasCancelled = false;
					_searchInputField.IsDone = false;
					_searchInputField.WasCancelled = false;
				}
				else
				{
					_logSearch.Enabled = false;

					TouchScreenKeyboardManager.Deactivate(_filterInputField);
					TouchScreenKeyboardManager.Deactivate(_searchInputField);
				}
			}
		}

		public float Height
		{
			get
			{
				float minimumWidthForSingleLine = MINIMUM_WIDTH_FOR_SINGLE_LINE * GUIStyles.ScaleFactor;
				return _debugConsole.ActualConsoleWidth >= minimumWidthForSingleLine ? GUIStyles.HeaderHeight : GUIStyles.HeaderHeight * 2 + 1;
			}
		}

		private bool _justMadeVisible = false;

		private readonly LogHistoryView _logHistoryView;
		private readonly FilteredLogHistoryViewModel _filteredLogHistoryViewModel;

		private readonly TouchScreenKeyboardInput _filterInputField = new TouchScreenKeyboardInput();
		private readonly TouchScreenKeyboardInput _searchInputField = new TouchScreenKeyboardInput();
		private readonly LogSearch _logSearch;

		private bool _hideNextFrame = false;

		private int MINIMUM_WIDTH_FOR_SINGLE_LINE = 1800;
		private DebugConsole _debugConsole;

		public FilterBar(DebugConsole debugConsole, FilteredLogHistoryViewModel filteredLogHistoryViewModel, LogSearch logSearch, LogHistoryView logHistoryView)
		{
			_debugConsole = debugConsole;
			_filteredLogHistoryViewModel = filteredLogHistoryViewModel;
			_logSearch = logSearch;
			_logHistoryView = logHistoryView;
		}

		public void OnGUI()
		{
			if (!IsVisible)
				return;

			float minimumWidthForSingleLine = MINIMUM_WIDTH_FOR_SINGLE_LINE * GUIStyles.ScaleFactor;
			
			if (_debugConsole.ActualConsoleWidth >= minimumWidthForSingleLine)
			{
				GUILayout.BeginHorizontal(GUILayout.Height(GUIStyles.HeaderHeight));
			}
			

			GUILayout.BeginHorizontal(GUIStyles.SearchFilterToolBarStyle, GUILayout.Height(GUIStyles.HeaderHeight));
			DrawFilterLine();
			GUILayout.EndHorizontal();

			if (_debugConsole.ActualConsoleWidth >= minimumWidthForSingleLine)
			{
				Widgets.VerticalSeparator();
			}

			GUILayout.BeginHorizontal(GUIStyles.SearchFilterToolBarStyle, GUILayout.Height(GUIStyles.HeaderHeight));
			DrawSearchLine();
			GUILayout.EndHorizontal();

			if (_debugConsole.ActualConsoleWidth >= minimumWidthForSingleLine)
			{
				GUILayout.EndHorizontal();
			}
			Widgets.HorizontalSeparator();
			
			if (_justMadeVisible)
			{
				_justMadeVisible = false;
			    if (Application.platform != RuntimePlatform.Android) // this breaks on Android
			    {
			        GUI.FocusControl("FilterTextField");
			        TouchScreenKeyboardManager.Activate(_filterInputField);
			    }
			}
		}

		public void Update()
		{
			if (_hideNextFrame)
			{
				_hideNextFrame = false;
				IsVisible = false;
			}
		}

		private void DrawFilterLine()
		{
			GUILayout.Label("Filter:", GUIStyles.HeaderButtonLabelStyle, GUILayout.Width(150 * GUIStyles.ScaleFactor));

			GUI.SetNextControlName("FilterTextField");
			TouchFriendlyTextField(_filterInputField, GUIStyles.FilterTextFieldStyle);
			bool newSearchAtStart = Widgets.Checkbox(_filteredLogHistoryViewModel.OnlyMatchStringsThatStartWithFilterString, "At start");
			if (_filteredLogHistoryViewModel.FilterString != _filterInputField.Text || newSearchAtStart != _filteredLogHistoryViewModel.OnlyMatchStringsThatStartWithFilterString)
			{
				_filteredLogHistoryViewModel.OnlyMatchStringsThatStartWithFilterString = newSearchAtStart;
				_filteredLogHistoryViewModel.FilterString = _filterInputField.Text;
				if(_logSearch.IsSearchActive)
					_logSearch.RunSearchFromScratch();
				SelectCurrentSearchResultBasedOnLogScroll();
			}
		}

		private void DrawSearchLine()
		{
			GUILayout.Label("Search:", GUIStyles.HeaderButtonLabelStyle, GUILayout.Width(200 * GUIStyles.ScaleFactor));
			var userFriendlyIndex = _logSearch.Results.Count == 0 ? 0 : _logSearch.CurrentResultIndex + 1;
			var searchCountContent = new GUIContent(string.Format("{0} of {1}", userFriendlyIndex, _logSearch.Results.Count));
			var searchCountSize = GUIStyles.SearchTextFieldCountStyle.CalcSize(searchCountContent);

			GUIStyles.SearchTextFieldStyle.padding = new RectOffset((int)(10 * GUIStyles.ScaleFactor), (int)searchCountSize.x + (int)(30 * GUIStyles.ScaleFactor), 2, 2);
			
			TouchFriendlyTextField(_searchInputField, GUIStyles.SearchTextFieldStyle);
			if (_logSearch.SearchString != _searchInputField.Text)
			{
				_logSearch.SearchString = _searchInputField.Text;

				if (_logSearch.SearchString != "")
				{
					SelectCurrentSearchResultBasedOnLogScroll();
					_logHistoryView.JumpToCurrentSearchResult();
				}
			}
			var rect = GUILayoutUtility.GetLastRect();
			if(_logSearch.IsSearchActive)
				GUI.Label(new Rect(rect.x, rect.y, rect.width - (20 * GUIStyles.ScaleFactor), rect.height), searchCountContent, GUIStyles.SearchTextFieldCountStyle);
			GUILayout.Space(4 * GUIStyles.ScaleFactor);

			GUI.enabled = _logSearch.IsSearchActive;

			if (Widgets.Button(DebugConsole.Instance.ImageFiles._PreviousHistoryItemIcon, null, "", (int)(GUIStyles.HeaderHeight * 0.75f)))
			{
				_logSearch.PreviousSearchResult();
				_logHistoryView.JumpToCurrentSearchResult();
			}

			GUILayout.Space(4 * GUIStyles.ScaleFactor);
			if (Widgets.Button(DebugConsole.Instance.ImageFiles._NextHistoryItemIcon, null, "", (int)(GUIStyles.HeaderHeight * 0.75f)))
			{
				_logSearch.NextSearchResult();
				_logHistoryView.JumpToCurrentSearchResult();
			}
			
			GUI.enabled = true;
		}

		private void SelectCurrentSearchResultBasedOnLogScroll()
		{
			if (_logSearch.Results.Count != 0 && _logHistoryView.ItemInMiddleOfScreen != null)
			{
				int nearestSearchResultIndex = _logSearch.Results.BinarySearch(_logHistoryView.ItemInMiddleOfScreen, new NearestMatchFinder());

				if (nearestSearchResultIndex >= 0)
					_logSearch.CurrentResultIndex = nearestSearchResultIndex;
				else if (nearestSearchResultIndex == ~_logSearch.Results.Count)
					_logSearch.CurrentResultIndex = _logSearch.Results.Count - 1;
				else
					_logSearch.CurrentResultIndex = ~nearestSearchResultIndex;
			}
			else
			{
				_logSearch.CurrentResultIndex = -1;
			}
		}

		private void TouchFriendlyTextField(TouchScreenKeyboardInput touchScreenKeyboardInput, GUIStyle style)
		{
			if (_isVisible)
			{
				if (touchScreenKeyboardInput.IsDone)
				{
                    if(Application.platform != RuntimePlatform.Android)
					    _hideNextFrame = true;
				}
				else if (touchScreenKeyboardInput.WasCancelled)
				{
				    if(Application.platform != RuntimePlatform.Android)
					    _hideNextFrame = true;
				}
			}

			Widgets.TouchFriendlyTextField(touchScreenKeyboardInput, style, GUILayout.ExpandHeight(true));
		}
	}

	internal class NearestMatchFinder : IComparer<LogHistoryItem>
	{
		public int Compare(LogHistoryItem x, LogHistoryItem y)
		{
			return x._Id.CompareTo(y._Id);
		}
	}
}