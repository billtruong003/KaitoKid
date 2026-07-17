using System;
using System.Collections.Generic;
using System.IO;
using Opencoding.LogHistory;
using Opencoding.Shared.Utils;
using UnityEngine;

namespace Opencoding.Console
{
	/// <summary>
	/// This is the 'context' menu that appears when you either right click on a log item (on desktop)
	/// or press-and-hold (on mobile). On mobile it's shown as a horizontal list (as that's easier to see)
	/// on desktop as a vertical list.
	/// </summary>
	class LogItemPopupMenu 
	{
		public bool IsVisible
		{
			get;
			set;
		}

		public Rect Rect
		{
			get;
			private set;
		}

		public LogHistoryItem TargetLogHistoryItem { get; private set; }

		private Vector2 _position;
		
		private readonly DebugConsole _debugConsole;

		private static bool IsHorizontal
		{
			get { return Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer; }
		}

		public LogItemPopupMenu(DebugConsole debugConsole)
		{
			_debugConsole = debugConsole;
		}

		public void Show(Vector2 position, LogHistoryItem logHistoryItem)
		{
			if(logHistoryItem == null)
				throw new InvalidOperationException("Log history item can't be null");

			_position = position;
			
			TargetLogHistoryItem = logHistoryItem;
						
			IsVisible = true;
		}

		public void OnGUI()
		{
			int numberOfButtons = NativeMethodsInterface.CanSendEmail ? 3 : 2;
			int width = 0;
			int height = 0;
			Vector2 position;
			const int buttonWidth = 300;

			if (IsHorizontal)
			{
				width = (int) (GUIStyles.ScaleFactor*buttonWidth*numberOfButtons);
				height = (int) (GUIStyles.HeaderHeight);

				position = new Vector2(_position.x - width/2.0f, _position.y - height);
				position = new Vector2(Mathf.Clamp(position.x, 0, _debugConsole.ActualConsoleWidth - width),
					Mathf.Clamp(position.y, 0, Screen.height - height));
			}
			else
			{
				width = (int) (GUIStyles.ScaleFactor*buttonWidth);
				height = (int)(GUIStyles.HeaderHeight * numberOfButtons);
				position = new Vector2(Mathf.Clamp(_position.x - 4, 0, _debugConsole.ActualConsoleWidth - width),
					Mathf.Clamp(_position.y - 2, 0, Screen.height - height));
			}

			Rect = new Rect(position.x, position.y, width, height);

			GUI.Window(101, Rect, WindowFunc, "", GUIStyles.PopupMenuWindowStyle);
		}

		private void WindowFunc(int id)
		{
			if (!IsVisible)
				return;

			if(IsHorizontal)
				GUILayout.BeginHorizontal();

			if (Button("Copy"))
			{
				NativeMethodsInterface.CopyTextToClipboard(TargetLogHistoryItem._LogMessage + "\n\n" + TargetLogHistoryItem._StackTrace);
			}

			if (NativeMethodsInterface.CanSendEmail)
			{
				Separator();

				if (Button("Email..."))
				{
					var version = _debugConsole.Settings.GameVersion;

					var email = new Email
					{
						Subject = TargetLogHistoryItem._LogMessage.Split(new char[] { '\n' }, 2)[0],
						Message = "Version: " + (String.IsNullOrEmpty(version) ? "<not set>" : version) + "\nPlatform: " + Application.platform + "\n\n" + TargetLogHistoryItem._LogMessage + "\n" + TargetLogHistoryItem._StackTrace,
						ToAddress = _debugConsole.Settings.DefaultToEmailAddress
					};
					NativeMethodsInterface.SendEmail(email);
				}
			}

			Separator();

			if (Button("Clear all"))
			{
				_debugConsole.Clear();
			}

			if(IsHorizontal)
				GUILayout.EndHorizontal();
			
			GUI.FocusWindow(id);
			GUI.BringWindowToFront(id);
		}

		private void Separator()
		{
			if(IsHorizontal)
				Widgets.VerticalSeparator();
			else
				Widgets.HorizontalSeparator();
		}

		private bool Button(string text)
		{
			var content = new GUIContent(text);
			Vector2 buttonSize = GUIStyles.HeaderButtonLabelStyle.CalcSize(content);
			GUIStyle style = IsHorizontal ? GUIStyles.HorizontalPopupMenuButtonLabelStyle : GUIStyles.PopupMenuButtonLabelStyle;

			var rect = GUILayoutUtility.GetRect(buttonSize.x, buttonSize.y, style, GUILayout.ExpandWidth(true));

			if (Event.current.type == EventType.Repaint)
			{
				style.Draw(new Rect(rect.x, rect.y, rect.width, rect.height), text, rect.Contains(Event.current.mousePosition), true, false, false);
			}
			else if (Event.current.type == EventType.MouseUp)
			{
				if (GUI.enabled && rect.Contains(Event.current.mousePosition))
				{
					Event.current.Use();
					IsVisible = false;
					return true;
				}
			}

			return false;
		}

	}
}