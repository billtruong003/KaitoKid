using System;
using UnityEngine;

namespace Opencoding.Console
{
	/// <summary>
	/// This is the settings popup that appears when you press the cog in the corner of the console.
	/// It doesn't really contain many real settings per-se, except the ability to stop the console
	/// opening everytime an exception occurs.
	/// </summary>
	class SettingsOverlay
	{
		public bool IsVisible { get; set; }

		private Rect _settingsButtonRect;

		private DebugConsole _debugConsole;

		public SettingsOverlay(DebugConsole debugConsole)
		{
			IsVisible = false;
			_debugConsole = debugConsole;
		}

		public Rect SettingsButtonRect
		{
			get
			{
				return  _settingsButtonRect;
			}
			set
			{
				_settingsButtonRect = value;

				Rect = new Rect(_settingsButtonRect.xMax - Width + 6, _settingsButtonRect.yMax, Width, Height); // -6 for the shadow size
			}
		}

		public Rect Rect
		{
			get;
			private set;
		}

		private int Height
		{
			get
			{
				var height = (int)(GUIStyles.ScaleFactor * 250.0f + 8); // 8 for the border/shadow
				if (!String.IsNullOrEmpty(_debugConsole.Settings.GameVersion))
					height += GUIStyles.HeaderButtonLabelStyle.fontSize + 8;
				return height;
			}
		}

		private int Width
		{
			get
			{
				return (int)(GUIStyles.ScaleFactor * 1100);
			}
		}

		public void OnGUI()
		{
			if (!IsVisible)
				return;

			if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Escape || Event.current.keyCode == KeyCode.Return))
			{
				IsVisible = false;
				return;
			}

			GUILayout.BeginArea(Rect, GUIStyles.OverlayWindowStyle);
		
			GUILayout.BeginVertical();
			GUILayout.Space(16 * GUIStyles.ScaleFactor);
			GUILayout.BeginHorizontal();
			GUILayout.Space(5 * GUIStyles.ScaleFactor);
			GUILayout.BeginVertical();
			_debugConsole.ShowOnException = Widgets.Checkbox(_debugConsole.ShowOnException, "Show automatically on exception");
			GUILayout.Space(GUIStyles.ScaleFactor * 20.0f);
			GUILayout.BeginHorizontal();
			bool supportedOpeningFiles = Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer;
			bool supportsEmail = NativeMethodsInterface.CanSendEmail;
			if (supportsEmail || supportedOpeningFiles)
			{
				if (GUILayout.Button(supportsEmail ? "Email log..." : "Export log...", GUIStyles.SettingsPanelButtonStyle,
					GUILayout.Height(GUIStyles.ScaleFactor*100.0f)))
				{
					_debugConsole.EmailLog();
				}
			}
			
			GUI.enabled = LogHistory.LogHistory.Instance.LogItems.Count != 0;
			if (GUILayout.Button("Clear history", GUIStyles.SettingsPanelButtonStyle, GUILayout.Height(GUIStyles.ScaleFactor * 100.0f)))
				_debugConsole.Clear();
			GUI.enabled = true;
			GUILayout.EndHorizontal();
			if (!String.IsNullOrEmpty(_debugConsole.Settings.GameVersion))
			{
				GUILayout.Label("Game version: " + _debugConsole.Settings.GameVersion, GUIStyles.HeaderButtonLabelStyle);
			}

			GUILayout.FlexibleSpace();
		
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}
}