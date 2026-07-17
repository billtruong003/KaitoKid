using UnityEngine;

namespace Opencoding.Console
{
	class TouchScreenInputField : InputField
	{
		private TouchScreenKeyboardInput _touchScreenKeyboardInput = new TouchScreenKeyboardInput();
		private Rect _commandInputRect;
		private readonly InputHistory _inputHistory;
		private readonly LogHistoryView _logHistoryView;
		private readonly HelpOverlay _helpOverlay;

		public override string Input
		{
			get
			{
				return _touchScreenKeyboardInput.Text;
			}
			set
			{
				_touchScreenKeyboardInput.Text = value;

                if(Application.platform != RuntimePlatform.Android)
				    ShowInputForConsole();
			}
		}

		public override bool HasFocus
		{
			get
			{
				return TouchScreenKeyboardManager.IsActive(_touchScreenKeyboardInput);
			}
		}

		public override Rect TextFieldRect
		{
			get
			{
				return _commandInputRect;
			}
		}

		public TouchScreenInputField(InputHistory inputHistory, LogHistoryView logHistoryView, HelpOverlay helpOverlay)
		{
			_inputHistory = inputHistory;
			_logHistoryView = logHistoryView;
			_helpOverlay = helpOverlay;
		}

		public override void OnGUI(Rect containingRect)
		{
			GUILayout.BeginHorizontal(GUIStyles.HeaderStyle, GUILayout.ExpandWidth(true), GUILayout.Height(GUIStyles.HeaderHeight));
			GUI.SetNextControlName("Input");
			Widgets.TouchFriendlyTextField(_touchScreenKeyboardInput, GUIStyles.InputTextFieldStyle, GUILayout.ExpandHeight(true));

			// Prevent a single space being typed at the begining of the input, which breaks everything a bit
			_touchScreenKeyboardInput.Text = _touchScreenKeyboardInput.Text.TrimStart(' ');

			if (Event.current.type == EventType.Repaint)
			{
				var lastRect = GUILayoutUtility.GetLastRect();
				_commandInputRect = new Rect(containingRect.x + lastRect.x, containingRect.y + lastRect.y, lastRect.width, lastRect.height);
			}

			if (!TouchScreenKeyboardManager.IsActive(_touchScreenKeyboardInput))
			{
				if (Event.current.type == EventType.MouseDown)
				{
					if (_commandInputRect.Contains(Event.current.mousePosition))
					{
						ShowInputForConsole();
					}
				}
			}
			else
			{
				if (_touchScreenKeyboardInput.IsDone)
				{
					ExecuteInput();
				}
				else if (_touchScreenKeyboardInput.WasCancelled)
				{
					LoseFocus();
				}
			}

			if (Widgets.HeaderButton(DebugConsole.Instance.ImageFiles._RunIcon,
				DebugConsole.Instance.ImageFiles._BackgroundGradient, ""))
			{
				ExecuteInput();
			}

			GUI.enabled = Input != "";
			GUILayout.Space(4 * GUIStyles.ScaleFactor);
			if (Widgets.HeaderButton(DebugConsole.Instance.ImageFiles._ClearIcon, DebugConsole.Instance.ImageFiles._BackgroundGradient, ""))
			{
				Input = "";
			}
			GUI.enabled = true;

			bool newHelpVisibility = Widgets.ToggleHeaderButton(DebugConsole.Instance.ImageFiles._HelpIcon, "", _helpOverlay.IsVisible);
			if (newHelpVisibility && !_helpOverlay.IsVisible)
			{
				TouchScreenKeyboardManager.Deactivate();
				DebugConsole.Instance.Minimize();
			}
			_helpOverlay.IsVisible = newHelpVisibility;

			if (Event.current.type == EventType.Repaint)
				HelpButtonRect = GUILayoutUtility.GetLastRect();

			GUILayout.EndHorizontal();

			ConfirmInput();
		}

		private void ExecuteInput()
		{
			LoseFocus();

			_inputHistory.RecordInput(_touchScreenKeyboardInput.Text);
			CommandHandlerSystem.CommandHandlers.HandleCommand(_touchScreenKeyboardInput.Text);
			_touchScreenKeyboardInput.Text = "";
			_logHistoryView.AutoScrolling = true;
		}

		public override void LoseFocus()
		{
			TouchScreenKeyboardManager.Deactivate(_touchScreenKeyboardInput);
		}

		public override void ClearInput()
		{
			_touchScreenKeyboardInput.Text = "";
		}

		public override void Focus()
		{
			ShowInputForConsole();
		}

		private void ShowInputForConsole()
		{
			TouchScreenKeyboardManager.Activate(_touchScreenKeyboardInput);
			DebugConsole.Instance.HidePopups();
		}
	}
}