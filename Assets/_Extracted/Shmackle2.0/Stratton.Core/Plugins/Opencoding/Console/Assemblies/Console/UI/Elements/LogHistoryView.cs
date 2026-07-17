using System;
using System.Text.RegularExpressions;
using Opencoding.LogHistory;
using Opencoding.Shared.Utils;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Opencoding.Console
{
	/// <summary>
	/// This renders the log history - the main part of the console.
	/// </summary>
	class LogHistoryView : IDisposable
	{
		public LogHistoryItem ItemInMiddleOfScreen { get; set; }
		private bool _autoScrolling = true;

		public bool AutoScrolling
		{
			get
			{
				if (_debugConsole.IsPopupMenuOpen || _isDragging)
					return false;
				return _autoScrolling;
			}
			set { _autoScrolling = value; }
		}

		private readonly DebugConsole _debugConsole;
		private readonly FilteredLogHistoryViewModel _viewModel;
		private readonly LogSearch _logSearch;
		private readonly LogItemPopupMenu _logItemPopupMenu;

		private int _expandedLogHistoryItemIndex = -1;
		private int _expandedItemHeight;
		private string _expandedItemStacktrace; // we pre-process the stacktrace to make it more readable, so this is a cache

		private bool _isDragging = false;
		private float _totalDragDelta; // used to set a threshold for how far you have to drag before the list moves

		private Vector2 _scrollPosition = Vector2.zero;
		private float _listHeight;
		private float _scollingViewTopInWindowCoords;
		
		private Vector2 _clickDownPosition;
		private float _clickDownTime = Mathf.Infinity; // used for detecting long press
		private int PADDING_BETWEEN_MESSAGE_AND_STACKTRACE = 12;
		private static Regex _stackTraceFormattingRegex;

		public LogHistoryView(FilteredLogHistoryViewModel viewModel, LogItemPopupMenu logItemPopupMenu, DebugConsole debugConsole, LogSearch logSearch)
		{
			_logItemPopupMenu = logItemPopupMenu;
			_viewModel = viewModel;
			_debugConsole = debugConsole;
			_logSearch = logSearch;
			_viewModel.LogHistoryListReset += CollapseExpandedItem; // If the list changes substantially (filtering etc) then we close the expanded item
			_viewModel.LogHistoryItemRemoved += DecrementExpandedItemIndex;
		}

		private void DecrementExpandedItemIndex(LogHistoryItem obj)
		{
			// An item has been removed from the begining of the list (presumably) due to 
			// the list getting too long. In this case we decrement the expanded item index, 
			// as it'll be moving towards the top of the list.
			if(_expandedLogHistoryItemIndex >= 0)
				_expandedLogHistoryItemIndex--;
		}

		private void CollapseExpandedItem()
		{
			_expandedLogHistoryItemIndex = -1;
		}

		public void Update()
		{
#if ENABLE_INPUT_SYSTEM
			if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
#else
			if (Input.GetMouseButtonUp(0))
#endif
			{
				_clickDownTime = Mathf.Infinity;
			}

		}

		public void OnGUI(bool inFocus)
		{
			if (AutoScrolling && Event.current.type != EventType.ScrollWheel)
				_scrollPosition = new Vector2(0, Mathf.Infinity);

			_scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true), GUILayout.Width(_debugConsole.ActualConsoleWidth));

			DrawConsoleItemList(inFocus);

			GUILayout.EndScrollView();

			var listRect = GUILayoutUtility.GetLastRect();
			float listHeight = listRect.height;

			if (Event.current.type == EventType.Repaint)
			{
				_listHeight = listHeight;
				_scollingViewTopInWindowCoords = listRect.y;
			}

			// Drag to scroll
			if(Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
				HandleTouchDrag(listRect);

			// Detect if we're auto-scrolling - this basically checks to see if the scrollbar is at the bottom
			// of the console.
			if (!_isDragging)
			{
				_autoScrolling = _scrollPosition.y >= CalculateListHeight() - listHeight;
			}
		}

		private void HandleTouchDrag(Rect listRect)
		{
			if (!Event.current.isMouse || (!listRect.Contains(Event.current.mousePosition) && !_isDragging)) 
				return;

			switch (Event.current.type)
			{
				case EventType.MouseDrag:
				{
					const float dragThreshold = 25;
					_totalDragDelta += Event.current.delta.y;
					if (!_isDragging && Mathf.Abs(_totalDragDelta) > dragThreshold*GUIStyles.ScaleFactor)
					{
						_isDragging = true;
						AutoScrolling = false;
					}

					if (_isDragging)
					{
						_scrollPosition += new Vector2(0, Event.current.delta.y);
						if (_scrollPosition.y < 0)
							_scrollPosition.y = 0;
					}
					break;
				}
				case EventType.MouseUp:
				{
					_totalDragDelta = 0;
					_isDragging = false;
					break;
				}
			}
		}

		public void JumpToCurrentSearchResult()
		{
			ScrollToShowItem(_logSearch.CurrentResult);
		}

		private void ExpandItem(int index, Rect itemRect)
		{
			_expandedLogHistoryItemIndex = index;
			var item = _viewModel.LogHistoryItems[index];

			// Precaclculate the size of the expanded item so that we can take this into account when rendering the list
			Texture2D image;
			GUIStyle messageStyle;
			Utils.GetImageAndStyleForHistoryItem(item, _debugConsole.ImageFiles, out messageStyle, out image);
			messageStyle.wordWrap = true;
			var width = CalculateRowTextSpaceAvailable(itemRect);
			var titleHeight = Mathf.Max(messageStyle.CalcHeight(new GUIContent(TrimLogMessageIfNecessary(item._LogMessage)), width), GUIStyles.ConsoleRowHeight);
			_expandedItemHeight = (int)titleHeight; 

			if (!String.IsNullOrEmpty(item._StackTrace))
			{
				_expandedItemStacktrace = FormatStackTrace(item._StackTrace);
				_expandedItemHeight += PADDING_BETWEEN_MESSAGE_AND_STACKTRACE + // padding between header and stacktrace
				(int)GUIStyles.LogHistoryItemTextAreaStyle.CalcHeight(new GUIContent(_expandedItemStacktrace), width) + 
				8; // padding at end
			}
			else
			{
				_expandedItemStacktrace = "";
			}

			messageStyle.wordWrap = false;

			ScrollToShowItem(item);
		}

	    private static string TrimLogMessageIfNecessary(string message)
	    {
	        if (message.Length < 10000)
	            return message;
	        return message.Substring(0, 10000) + "\n<trimmed>";
	    }

		private string FormatStackTrace(string stackTrace)
		{
			_stackTraceFormattingRegex = new Regex("((?:[a-z][a-z\\.\\d\\-]+):\\d+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			return _stackTraceFormattingRegex.Replace(stackTrace, "<b>$1</b>");
		}

		private void ScrollToShowItem(LogHistoryItem item)
		{
			var index = _viewModel.LogHistoryItems.IndexOf(item);
			var itemTop = GetItemTop(index);
			var itemBottom = itemTop + GetItemHeight(index);

			float screenTop = _scrollPosition.y;
			float screenBottom = _scrollPosition.y + _listHeight;
			
			// Check if the item is already fully on screen
			if (itemTop > screenTop && itemBottom < screenBottom)
				return;

			if (itemTop < screenTop) // off the top of the screen
			{
				// bring it down to sit at top of the log
				_scrollPosition.y = Mathf.Clamp(itemTop, 0, Mathf.Infinity);
			}
			else if (itemBottom > screenBottom) // off the bottom of the screen
			{
				// bring it up to sit on the bottom
				// or, if it's so long that it is taller than there is room for
				// make it sit at the top.
				_scrollPosition.y = Mathf.Min(Mathf.Clamp(itemTop, 0, Mathf.Infinity), Mathf.Clamp(itemBottom - _listHeight, 0, Mathf.Infinity));
			}
			else
				return;

			AutoScrolling = false;
		}

		private void DrawConsoleItemList(bool inFocus)
		{
			int itemCount = _viewModel.LogHistoryItems.Count;

			var listHeight = CalculateListHeight();

			if (float.IsInfinity(_scrollPosition.y))
				_scrollPosition = new Vector2(0, listHeight);

			// Reserve a rect the full height of the list
			var listRect = GUILayoutUtility.GetRect(0, _debugConsole.ActualConsoleWidth, listHeight, listHeight);

			int firstIndexVisible = (int)(_scrollPosition.y / GUIStyles.ConsoleRowHeight);
			
			if (_expandedLogHistoryItemIndex != -1 && firstIndexVisible > _expandedLogHistoryItemIndex)
			{
				if (_scrollPosition.y < GUIStyles.ConsoleRowHeight*_expandedLogHistoryItemIndex - 1 + _expandedItemHeight)
					firstIndexVisible = _expandedLogHistoryItemIndex;
				else
					firstIndexVisible = (int)((_scrollPosition.y - _expandedItemHeight)/GUIStyles.ConsoleRowHeight) + 1;
			}
			int lastIndexVisible = Mathf.Clamp(firstIndexVisible + Screen.height / GUIStyles.ConsoleRowHeight, 0, itemCount - 1);
			if (Event.current.type == EventType.Repaint)
			{
				if (_viewModel.LogHistoryItems.Count != 0 && lastIndexVisible > firstIndexVisible)
					ItemInMiddleOfScreen = _viewModel.LogHistoryItems[firstIndexVisible + (lastIndexVisible - firstIndexVisible)/2];
				else
					ItemInMiddleOfScreen = null;
			}

			for (int index = firstIndexVisible; index <= lastIndexVisible; ++index)
			{
				var item = _viewModel.LogHistoryItems[index];

				Rect itemRect = new Rect(0, GetItemTop(index), listRect.width, GetItemHeight(index));

				DrawConsoleItem(itemRect, item, index, inFocus);
			}

			GUI.color = Color.white;
		}

		private int CalculateListHeight()
		{
			var listHeight = _viewModel.LogHistoryItems.Count * GUIStyles.ConsoleRowHeight;
			if (_expandedLogHistoryItemIndex != -1)
			{
				listHeight += -GUIStyles.ConsoleRowHeight + _expandedItemHeight;
			}
			return listHeight;
		}

		private int GetItemTop(int index)
		{
			if (_expandedLogHistoryItemIndex == -1)
			{
				return index*GUIStyles.ConsoleRowHeight;
			}

			if (index <= _expandedLogHistoryItemIndex)
			{
				return index * GUIStyles.ConsoleRowHeight;
			}

			return (index - 1) * GUIStyles.ConsoleRowHeight + _expandedItemHeight;
		}

		private int GetItemHeight(int index)
		{
			return index == _expandedLogHistoryItemIndex ? _expandedItemHeight : GUIStyles.ConsoleRowHeight;
		}

		private int CalculateRowTextSpaceAvailable(Rect itemRect)
		{
			return (int)(itemRect.width - 3 * GUIStyles.ConsoleRowTextLeftMargin - GUIStyles.ConsoleRowHeight);
		}

		private void DrawConsoleItem(Rect itemRect, LogHistoryItem historyItem, int index, bool inFocus)
		{
			int controlId = 100000 + index; // Using GUIUtility.GetControlID breaks things here (the KeyboardInputField can't get focus).

			if (itemRect.Contains(Event.current.mousePosition) && !_debugConsole.IsPopupMenuOpen && inFocus)
			{
				switch (Event.current.type)
				{
					case EventType.MouseUp:
						HandleMouseUpOnItem(itemRect, historyItem, index, controlId);
						break;
					case EventType.MouseDown:
						HandleMouseDownOnItem(controlId);
						break;
				}
			}

			if (Event.current.type == EventType.Repaint)
			{
				if(_logSearch.IsSearchActive)
				{
					if (_logSearch.CurrentResult == historyItem)
						GUIStyles.SelectedSearchItemBackgroundStyle.Draw(itemRect, false, false, false, false);
					else if(_logSearch.IsLogHistoryItemASearchResult(historyItem))
						GUIStyles.SearchResultItemBackgroundStyle.Draw(itemRect, false, false, false, false);
				}

				// We highlight the item if the popup menu is showing for this item
				if (_logItemPopupMenu.IsVisible && _logItemPopupMenu.TargetLogHistoryItem == historyItem)
				{
					GUIStyles.HighlightedItemBackgroundStyle.Draw(itemRect, false, false, false, false);
				}
				else
				{
					if (index % 2 == 0)
						GUIStyles.ItemAlternateBackgroundStyle.Draw(itemRect, false, false, false, false);
				}

				if (_expandedLogHistoryItemIndex == index)
					DrawExpandedItemContent(itemRect, historyItem);
				else
					DrawCollapsedItemContent(itemRect, historyItem);
			}
		}

		// This allows 'right clicking' on items by holding down a finger, only on mobile
		private void HandleMouseDownOnItem(int controlId)
		{
			GUIUtility.hotControl = controlId;
			HandleMobileTouchBeginOnItem();
		}

		private void HandleMobileTouchBeginOnItem()
		{
			if (PlatformUtils.IsDesktop)
				return;

			_clickDownTime = Time.realtimeSinceStartup;
			_clickDownPosition = Event.current.mousePosition;
		}

		private void HandleMouseUpOnItem(Rect itemRect, LogHistoryItem historyItem, int index, int controlId)
		{
			if (_isDragging) 
				return;

			if (GUIUtility.hotControl != controlId)
				return;

			if (Event.current.button == 0)
			{
				if (historyItem._Type == LogHistoryLogType.ConsoleInput)
				{
					_debugConsole.SetCommandInputTextAsIfTyped(historyItem._LogMessage);
					CollapseExpandedItem();
					_clickDownTime = Mathf.Infinity;
				}
				else if (Time.realtimeSinceStartup - _clickDownTime > 0.3f &&
				    Vector2.Distance(_clickDownPosition, Event.current.mousePosition) < 40*GUIStyles.ScaleFactor)
				{
					ShowItemPopup(historyItem);
					_clickDownTime = Mathf.Infinity;
				}
				else
				{
					// The user clicked on a row
					if (_expandedLogHistoryItemIndex == index)
						CollapseExpandedItem();
					else
						ExpandItem(index, itemRect);
				}
			}
			else if (Event.current.button == 1)
			{
				ShowItemPopup(historyItem);
			}
			Event.current.Use();
		}

		private void ShowItemPopup(LogHistoryItem historyItem)
		{
			var mousePositionInWindowCoordinates = new Vector2(Event.current.mousePosition.x,
				Event.current.mousePosition.y - _scrollPosition.y + _scollingViewTopInWindowCoords);

			_logItemPopupMenu.Show(mousePositionInWindowCoordinates, historyItem);
		}

		private void DrawCollapsedItemContent(Rect itemRect, LogHistoryItem historyItem)
		{
			GUIStyle style;
			Texture2D image;
			Utils.GetImageAndStyleForHistoryItem(historyItem, _debugConsole.ImageFiles, out style, out image);

			// limit each row to a fixed height (one line), otherwise we can't scroll easily - we currently rely
			// on knowing the row height to detect when you're at the bottom of the list.
			var width = CalculateRowTextSpaceAvailable(itemRect);

			// Draw the first line of the message
			GUI.Label(new Rect(itemRect.x + 2 * GUIStyles.ConsoleRowTextLeftMargin + GUIStyles.ConsoleRowHeight, itemRect.y, width, GUIStyles.ConsoleRowHeight), TrimLogMessageIfNecessary(historyItem._FirstLineOfLogMessage), style);

			// Draw the icon
			float iconSize = GUIStyles.ConsoleRowHeight * 0.8f;
			float iconPadding = (GUIStyles.ConsoleRowHeight - iconSize) / 2.0f;
			GUI.DrawTexture(new Rect(itemRect.x + GUIStyles.ConsoleRowTextLeftMargin, itemRect.y + iconPadding, iconSize, iconSize), image, ScaleMode.ScaleToFit);
		}

		private void DrawExpandedItemContent(Rect itemRect, LogHistoryItem historyItem)
		{
			GUIStyle style;
			Texture2D image;
			Utils.GetImageAndStyleForHistoryItem(historyItem, _debugConsole.ImageFiles, out style, out image);
			
			var content = new GUIContent(TrimLogMessageIfNecessary(historyItem._LogMessage));
			var width = CalculateRowTextSpaceAvailable(itemRect);
			style.wordWrap = true; 
			var titleHeight = Mathf.Max(style.CalcHeight(content, width), GUIStyles.ConsoleRowHeight);
			// Draw the message body
			GUI.Label(new Rect(itemRect.x + 2 * GUIStyles.ConsoleRowTextLeftMargin + GUIStyles.ConsoleRowHeight, itemRect.y, width, titleHeight), content, style);
			style.wordWrap = false;

			// Draw the icon
			float iconSize = GUIStyles.ConsoleRowHeight * 0.8f;
			float iconPadding = (GUIStyles.ConsoleRowHeight - iconSize) / 2.0f;
			GUI.DrawTexture(new Rect(itemRect.x + GUIStyles.ConsoleRowTextLeftMargin, itemRect.y + iconPadding, iconSize, iconSize), image, ScaleMode.ScaleToFit);

			// Draw the stacktrace
			if (!String.IsNullOrEmpty(_expandedItemStacktrace))
			{
				GUI.Label(new Rect(itemRect.x + 2 * GUIStyles.ConsoleRowTextLeftMargin + GUIStyles.ConsoleRowHeight, itemRect.y + titleHeight + PADDING_BETWEEN_MESSAGE_AND_STACKTRACE,
						width, itemRect.height - titleHeight - 4),
						_expandedItemStacktrace, GUIStyles.LogHistoryItemTextAreaStyle);
			}
		}

        public void Dispose()
        {
            _viewModel.LogHistoryListReset -= CollapseExpandedItem; 
            _viewModel.LogHistoryItemRemoved -= DecrementExpandedItemIndex;
        }
    }
}