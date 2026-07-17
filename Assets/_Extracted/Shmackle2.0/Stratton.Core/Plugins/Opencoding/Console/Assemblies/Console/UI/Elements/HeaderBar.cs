using UnityEngine;

namespace Opencoding.Console
{
	/// <summary>
	/// This draws the header that goes across the top of the console, with the close, filter options and help button.
	/// </summary>
	class HeaderBar
	{
		private readonly DebugConsole _debugConsole;
		private readonly FilteredLogHistoryViewModel _logHistoryViewModel;
		private readonly InputField _inputField;
		private readonly FilterBar _filterBar;
		private readonly SettingsOverlay _settingsOverlay;
		private readonly LogHistory.LogHistory _logHistory;
		private readonly ImageFilesContainer _imageFiles;
		private readonly Settings _settings;

		public HeaderBar(DebugConsole debugConsole, 
			FilteredLogHistoryViewModel filteredLogHistoryViewModel, 
			InputField inputField, 
			FilterBar filterBar, 
			SettingsOverlay settingsOverlay, 
			LogHistory.LogHistory logHistory,
			Settings settings)
		{
			_debugConsole = debugConsole;
			_logHistoryViewModel = filteredLogHistoryViewModel;
			_inputField = inputField;
			_filterBar = filterBar;
			_settingsOverlay = settingsOverlay;
			_logHistory = logHistory;
			_settings = settings;

			_imageFiles = _debugConsole.ImageFiles;
		}

		public void OnGUI()
		{
			GUILayout.BeginHorizontal(GUIStyles.HeaderStyle, GUILayout.Height(GUIStyles.HeaderHeight));

			if (Widgets.HeaderButton(_debugConsole.ImageFiles._CloseButton, _debugConsole.ImageFiles._RedBackgroundGradient, ""))
			{
				_inputField.LoseFocus();
				DebugConsole.IsVisible = false;
			}

			Widgets.VerticalSeparator();

			if (Widgets.HeaderButton(_debugConsole.IsMaximized ? _imageFiles._MinimizeTopIcon : _imageFiles._MaximizeIcon))
			{
				_debugConsole.IsMaximized = !_debugConsole.IsMaximized;

				if (_debugConsole.IsMaximized && TouchScreenKeyboardManager.IsAnyInputActive)
					TouchScreenKeyboardManager.Deactivate();
			}

			Widgets.VerticalSeparator();

			GUI.enabled = true;

			GUILayout.FlexibleSpace();

			Widgets.VerticalSeparator();
			GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

			bool wasButtonHeldDown = false;
			_filterBar.IsVisible = Widgets.ToggleHeaderButton(_logHistoryViewModel.FilterString != "" ? _imageFiles._FilterActiveIcon : _imageFiles._SearchIcon, "", _filterBar.IsVisible, out wasButtonHeldDown);

			Widgets.VerticalSeparator();
			_logHistoryViewModel.ShowInfos = Widgets.ToggleHeaderButton(_imageFiles._InfoIcon, _logHistory.InfoCount.ToString(), _logHistoryViewModel.ShowInfos, out wasButtonHeldDown);
			if (wasButtonHeldDown)
				HandleButtonHeld(LogType.Log);

			Widgets.VerticalSeparator();
			_logHistoryViewModel.ShowWarnings = Widgets.ToggleHeaderButton(_imageFiles._WarningIcon, _logHistory.WarningCount.ToString(), _logHistoryViewModel.ShowWarnings, out wasButtonHeldDown);
			if (wasButtonHeldDown)
				HandleButtonHeld(LogType.Warning);

			Widgets.VerticalSeparator();

			int errorCount = _logHistory.ErrorCount;
			if (!_settings.ShowSeparateExceptionButton)
				errorCount += _logHistory.ExceptionCount;
			if (!_settings.ShowSeparateAssertButton)
				errorCount += _logHistory.AssertCount;

			_logHistoryViewModel.ShowErrors = Widgets.ToggleHeaderButton(_imageFiles._ErrorIcon, errorCount.ToString(), _logHistoryViewModel.ShowErrors, out wasButtonHeldDown);
			if (wasButtonHeldDown)
				HandleButtonHeld(LogType.Error);

			if (!_settings.ShowSeparateExceptionButton)
				_logHistoryViewModel.ShowExceptions = _logHistoryViewModel.ShowErrors;
			if (!_settings.ShowSeparateAssertButton)
				_logHistoryViewModel.ShowAsserts = _logHistoryViewModel.ShowErrors;
			
			Widgets.VerticalSeparator();

			if (_settings.ShowSeparateExceptionButton)
			{
				_logHistoryViewModel.ShowExceptions = Widgets.ToggleHeaderButton(_imageFiles._ExceptionIcon, _logHistory.ExceptionCount.ToString(), _logHistoryViewModel.ShowExceptions, out wasButtonHeldDown);
				if (wasButtonHeldDown)
					HandleButtonHeld(LogType.Exception);

				Widgets.VerticalSeparator();
			}

			if (_settings.ShowSeparateAssertButton)
			{
				_logHistoryViewModel.ShowAsserts = Widgets.ToggleHeaderButton(_imageFiles._AssertIcon, _logHistory.AssertCount.ToString(), _logHistoryViewModel.ShowAsserts, out wasButtonHeldDown);
				if (wasButtonHeldDown)
					HandleButtonHeld(LogType.Assert);

				Widgets.VerticalSeparator();
			}

			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();

			Widgets.VerticalSeparator();

			_settingsOverlay.IsVisible = Widgets.OverlayHeaderButton(_imageFiles._SettingsIcon, "", _settingsOverlay.IsVisible);

			if (Event.current.type == EventType.Repaint)
				_settingsOverlay.SettingsButtonRect = GUILayoutUtility.GetLastRect();
			GUILayout.EndHorizontal();
			var rect = GUILayoutUtility.GetRect(1, 5000, 1, 1, GUILayout.ExpandWidth(true));
			GUI.DrawTexture(rect, GUIStyles.BlackTexture);
		}

		private void HandleButtonHeld(LogType logType)
		{
			bool isSolo = _logHistoryViewModel.ShowWarnings == (logType == LogType.Warning) &&
			              _logHistoryViewModel.ShowErrors == (logType == LogType.Error) &&
			              _logHistoryViewModel.ShowExceptions == (logType == LogType.Exception) &&
			              _logHistoryViewModel.ShowAsserts == (logType == LogType.Assert) &&
			              _logHistoryViewModel.ShowInfos == (logType == LogType.Log);

			if (isSolo)
			{
				// show all log types
				_logHistoryViewModel.ShowWarnings = true;
				_logHistoryViewModel.ShowErrors = true;
				_logHistoryViewModel.ShowExceptions = true;
				_logHistoryViewModel.ShowAsserts = true;
				_logHistoryViewModel.ShowInfos = true;
			}
			else
			{
				_logHistoryViewModel.ShowWarnings = (logType == LogType.Warning);
				_logHistoryViewModel.ShowErrors = (logType == LogType.Error);
				_logHistoryViewModel.ShowExceptions = (logType == LogType.Exception);
				_logHistoryViewModel.ShowAsserts = (logType == LogType.Assert);
				_logHistoryViewModel.ShowInfos = (logType == LogType.Log);
			}
		}
	}
}