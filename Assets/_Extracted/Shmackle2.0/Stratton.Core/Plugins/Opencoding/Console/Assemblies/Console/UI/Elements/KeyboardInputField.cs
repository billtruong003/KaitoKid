using System;
using System.Reflection;
using Opencoding.Shared.Utils;
using UnityEngine;

namespace Opencoding.Console
{
	/// <summary>
	/// This handles the input field (where you enter commands) on desktop platforms.
	/// </summary>
	class KeyboardInputField : InputField
	{
		private string _commandInput = "";
		private Rect _commandInputRect;
		private TextEditor _commandInputBoxTextEditor;
		private readonly ImageFilesContainer _imageFiles;
		private readonly InputHistory _inputHistory;
		private int _tabIndex = 0;
		private bool _moveCursorToEndNextRepaint;
		private string _inputFieldName;

	    private FieldInfo _unityBefore53ContentField;
        private PropertyInfo _unityAfter53TextProperty;
	    private bool _isOnUnityBefore53;

        public override string Input
		{
			get
			{
				return _commandInput;
			}
			set
			{
				_commandInput = value;

			    SetTextEditorTextWithReflection();

				GUI.FocusControl(_inputFieldName);
				_moveCursorToEndNextRepaint = true;
			}
		}

	    public override bool HasFocus
		{
			get
			{
				return GUI.GetNameOfFocusedControl() == _inputFieldName;
			}
		}

	    public override Rect TextFieldRect
		{
			get
			{
				return _commandInputRect;
			}
		}

	   
	    private readonly LogHistoryView _logHistoryView;
		private readonly HelpOverlay _helpOverlay;

		private readonly PropertyInfo _cursorIndexProperty;
		private readonly PropertyInfo _selectIndexProperty;
		private readonly FieldInfo _cursorIndexField;
		private readonly FieldInfo _selectIndexField;

		public KeyboardInputField(ImageFilesContainer imageFiles, InputHistory inputHistory, LogHistoryView logHistoryView, HelpOverlay helpOverlay)
		{
			_imageFiles = imageFiles;
			_inputHistory = inputHistory;
			_logHistoryView = logHistoryView;
			_helpOverlay = helpOverlay;

            var unityVersion = Application.unityVersion;
            _isOnUnityBefore53 = unityVersion.StartsWith("4") || unityVersion.StartsWith("5.0") ||
		                         unityVersion.StartsWith("5.1") ||
		                         unityVersion.StartsWith("5.2");

            _cursorIndexProperty = typeof(TextEditor).GetProperty("cursorIndex", BindingFlags.Instance | BindingFlags.Public);
			_selectIndexProperty = typeof(TextEditor).GetProperty("selectIndex", BindingFlags.Instance | BindingFlags.Public);
			
			if (_cursorIndexProperty == null && _selectIndexProperty == null)
			{
				// For before Unity 5.2
				_cursorIndexField = typeof(TextEditor).GetField("pos", BindingFlags.Instance | BindingFlags.Public);
				_selectIndexField = typeof(TextEditor).GetField("selectPos", BindingFlags.Instance | BindingFlags.Public);	

				if(_cursorIndexField == null || _selectIndexField == null)
					Debug.LogWarning(
					"Couldn't find TextEditor properties via reflection. This probably means that you're using an unsupported version of Unity. Contact support@opencoding.net and let me know!");
			}
		}

		public override void OnGUI(Rect containingRect)
		{
			if (_moveCursorToEndNextRepaint && Event.current.type == EventType.Repaint)
			{
				// When the text field gets focus, it automatically selects all. Whoop. 
				// So we need to wait a bit (for a repaint event, it seems) before we 
				// can move the cursor to where we actually want it to be. Nasty.
				// Even worse, in Unity 5.2, the names of these properties changed, so 
				// reflection has to be used!
				if(_cursorIndexProperty != null)
					_cursorIndexProperty.SetValue(_commandInputBoxTextEditor, _commandInput.Length, null);
				if (_selectIndexProperty != null) 
					_selectIndexProperty.SetValue(_commandInputBoxTextEditor, _commandInput.Length, null);

				if (_cursorIndexField != null)
					_cursorIndexField.SetValue(_commandInputBoxTextEditor, _commandInput.Length);
				if (_selectIndexField != null)
					_selectIndexField.SetValue(_commandInputBoxTextEditor, _commandInput.Length);

				_moveCursorToEndNextRepaint = false;
			}

			if (Event.current.type == EventType.KeyDown)
			{
				HandleKeyDownEvent();
			}

			if (Event.current.isKey && (Event.current.keyCode == KeyCode.Tab || Event.current.character == 9))
			{
				Event.current.Use();
			}

			GUILayout.BeginHorizontal(GUIStyles.HeaderStyle, GUILayout.ExpandWidth(true), GUILayout.Height(GUIStyles.HeaderHeight));
			_inputFieldName = "InputField" + GUIUtility.GetControlID(FocusType.Keyboard);
			GUI.SetNextControlName(_inputFieldName);
			string oldCommandInput = _commandInput;
			var originalText = Widgets.FixedSizeTextField(_commandInput, GUIStyles.InputTextFieldStyle,
				GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
			_commandInput = DebugConsole.Instance.DetectCloseKeyInInputText(originalText);

			// Workaround for bug in Unity 5.2.0 and .1
			if (DebugConsole.IsOnUnityVersionWithBrokenKeyboardInput())
			{
				if (_commandInput != originalText)
				{
					DebugConsole.IsVisible = false;
				}
			}

			// Prevent a single space being typed at the begining of the input, which breaks everything a bit
			_commandInput = _commandInput.TrimStart(' ');
		
			if (Event.current.type == EventType.Repaint)
			{
				var lastRect = GUILayoutUtility.GetLastRect();
				_commandInputRect = new Rect(containingRect.x + lastRect.x, containingRect.y + lastRect.y, lastRect.width, lastRect.height);
			}

			if (HasFocus)
			{
				// The state object seems to only be able to be grabbed if it has focus. Fantastic.
				// It also changes between focuses - it seems.
				_commandInputBoxTextEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
			}

			if (DebugConsole.Instance.JustMadeVisible)
			{			
				// This is a somewhat nasty hack - we need to grab focus once for the input field when the console is opened
				DebugConsole.Instance.JustMadeVisible = false;
				GUI.FocusControl(_inputFieldName);
			}

			if (oldCommandInput != _commandInput)
			{
				_tabIndex = 0;
				ConfirmInput();
			}

			bool newHelpVisibility = Widgets.ToggleHeaderButton(_imageFiles._HelpIcon, "", _helpOverlay.IsVisible);
			if (newHelpVisibility && !_helpOverlay.IsVisible)
			{
				DebugConsole.Instance.Minimize();
			}
			_helpOverlay.IsVisible = newHelpVisibility;
			
			if (Event.current.type == EventType.Repaint)
				HelpButtonRect = GUILayoutUtility.GetLastRect();
			GUILayout.EndHorizontal();
		}

		public override void ClearInput()
		{
			_commandInput = "";
		}

		private void HandleKeyDownEvent()
		{
			switch (Event.current.keyCode)
			{
				case KeyCode.Return:
					HandleReturnKeyPressed();
					break;
				case KeyCode.Tab:
					HandleTabKeyPressed();
					break;
				case KeyCode.UpArrow:
					HandleUpKeyPressed();
					break;
				case KeyCode.DownArrow:
					HandleDownKeyPressed();
					break;
			}
		}

		private void HandleReturnKeyPressed()
		{
			if (_commandInput.Trim() == "") 
				return;

			_inputHistory.RecordInput(_commandInput);
			CommandHandlerSystem.CommandHandlers.HandleCommand(_commandInput);
			_commandInput = "";
			ConfirmInput();
			_logHistoryView.AutoScrolling = true;
		}

		private void HandleUpKeyPressed()
		{
			var previousInput = _inputHistory.GetPreviousInput();
			
			if (previousInput == null) 
				return;

			Input = previousInput;
            ConfirmInput();
			Event.current.Use();
		}

		private void HandleDownKeyPressed()
		{
			var nextInput = _inputHistory.GetNextInput();
			
			if (nextInput == null) 
				return;

			Input = nextInput;
            ConfirmInput();
            Event.current.Use();
		}

		private void HandleTabKeyPressed()
		{
			if (_lastTypedParameters != null && _lastTypedParameters.Length != 0)
			{
				int currentParameter = _lastTypedParameters.Length - 2;

				if (LastTypedInput.EndsWith(" "))
					currentParameter++;

				if (currentParameter < 0)
				{
					// we're completing the command name
					var closestMatch = CommandHandlerSystem.CommandHandlers.FindClosestMatchingCommand(_lastTypedParameters[0], _tabIndex);
					if (closestMatch != null)
					{
						Input = closestMatch.CommandName;
					}
				}
				else
				{
					// we're completing a parameter
					var commandHandler = CommandHandlerSystem.CommandHandlers.GetCommandHandler(_lastTypedParameters[0]);
					if (commandHandler != null)
					{
						var parameters = commandHandler.Parameters;
						if (currentParameter < parameters.Length)
						{
							string parameterValue = _lastTypedParameters.Length < currentParameter + 2
								? ""
								: _lastTypedParameters[currentParameter + 1];

							var possibleParameters = parameters[currentParameter].GetParameterPossibleValues(parameterValue).ToArray();
							if (possibleParameters.Length != 0)
							{
								var closestMatchingParameterValue = possibleParameters[_tabIndex % possibleParameters.Length];
								if (!String.IsNullOrEmpty(closestMatchingParameterValue))
								{
									var parts = String.Join(" ", _lastTypedParameters.SubArray(0, currentParameter + 1));
									Input = parts + " " + CommandHandlerSystem.Utils.WrapInQuotesIfNecessary(closestMatchingParameterValue);
								}
							}
						}
					}
				}
			}
			_tabIndex++;
		}

        private void SetTextEditorTextWithReflection()
        {
            if (_isOnUnityBefore53)
            {
                var content = new GUIContent(_commandInput);
                if (_unityBefore53ContentField == null)
                    _unityBefore53ContentField = typeof(TextEditor).GetField("content",
                        BindingFlags.Instance | BindingFlags.Public);

                if (_unityBefore53ContentField == null)
                    throw new InvalidOperationException("Couldn't find 'content' field in TextEditor class on Unity version " +
                                                        Application.unityVersion);

                _unityBefore53ContentField.SetValue(_commandInputBoxTextEditor, content);
            }
            else
            {
                if (_unityAfter53TextProperty == null)
                    _unityAfter53TextProperty = typeof(TextEditor).GetProperty("text",
                        BindingFlags.Instance | BindingFlags.Public);

                if (_unityAfter53TextProperty == null)
                    throw new InvalidOperationException("Couldn't find 'text' property in TextEditor class on Unity version " +
                                                        Application.unityVersion);

                _unityAfter53TextProperty.SetValue(_commandInputBoxTextEditor, _commandInput, null);
            }
        }


        public override void LoseFocus()
		{
			_tabIndex = 0;
		}

		public override void Focus()
		{
			//GUI.FocusControl("Input");
		}
	}
}