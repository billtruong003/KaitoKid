using Opencoding.Shared.Utils;
using UnityEngine;

namespace Opencoding.Console
{
	static class Widgets
	{
		private static float _timeOfMouseDown;

		public static void VerticalSeparator()
		{
			var rect = GUILayoutUtility.GetRect(1, 1, 1, 5000, GUILayout.ExpandHeight(true), GUILayout.Width(1));
			GUI.DrawTexture(rect, GUIStyles.BlackTexture);
		}

		public static void HorizontalSeparator()
		{
			var rect = GUILayoutUtility.GetRect(1, 5000, 1, 1, GUILayout.ExpandWidth(true), GUILayout.Height(1));
			GUI.DrawTexture(rect, GUIStyles.BlackTexture);
		}

		public static bool Button(string text)
		{
			bool wasHeld = false;
			return Button(null, null, text, 0, out wasHeld);
		}

		public static bool Button(Texture2D icon, Texture2D backgroundImage, string text, int iconSize)
		{
			bool wasButtonHeldDown;
			return Button(icon, backgroundImage, text, iconSize, out wasButtonHeldDown);
		}

		public static bool Button(Texture2D icon, Texture2D backgroundImage, string text, int iconSize, out bool wasButtonHeldDown)
		{
			int padding = (int)(15 * GUIStyles.ScaleFactor);
			if (icon == null)
				iconSize = 0;
		
			var buttonSize = new Vector2(iconSize + padding * 2, iconSize);
			if (text != "")
			{
				var content = new GUIContent(text);
				buttonSize = GUIStyles.HeaderButtonLabelStyle.CalcSize(content);

				buttonSize.x += padding + iconSize + padding;

				if (buttonSize.y < iconSize)
					buttonSize.y = iconSize;
			}

			var rect = GUILayoutUtility.GetRect(buttonSize.x, buttonSize.y, GUIStyles.HeaderButtonLabelStyle, GUILayout.Width(buttonSize.x));

			return Button(rect, icon, backgroundImage, text, iconSize, padding, out wasButtonHeldDown);
		}

		public static bool Button(Rect rect, Texture2D icon, Texture2D backgroundImage, string text, int iconSize, int padding)
		{
			bool wasButtonHeldDown;
			return Button(rect, icon, backgroundImage, text, iconSize, padding, out wasButtonHeldDown);
		}

		public static bool Button(Rect rect, Texture2D icon, Texture2D backgroundImage, string text, int iconSize, int padding, out bool wasButtonHeldDown)
		{
			wasButtonHeldDown = false;

			int controlId = GUIUtility.GetControlID(FocusType.Passive, rect);
			switch (Event.current.type)
			{
				case EventType.Repaint:
				{
					if (backgroundImage != null)
						GUI.DrawTexture(rect, backgroundImage);

					if (GUIUtility.hotControl == controlId && _timeOfMouseDown > 0 && Time.realtimeSinceStartup - _timeOfMouseDown >= 0.3f)
					{
						wasButtonHeldDown = true;
						GUIUtility.hotControl = 0;
					}

					var oldColor = GUI.color;
					if (!GUI.enabled)
						GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
					if (icon != null)
						GUI.DrawTexture(new Rect(rect.x + padding, rect.y + (rect.height - iconSize)/2, iconSize, iconSize), icon);
					GUI.color = oldColor;

					if (text != "")
					{
						GUI.Label(new Rect(rect.x + iconSize + padding, rect.y, rect.width, rect.height), text,
							GUIStyles.HeaderButtonLabelStyle);
					}
					break;
				}
				case EventType.MouseDown:
				{
					if (GUI.enabled && rect.Contains(Event.current.mousePosition))
					{
						GUIUtility.hotControl = controlId;

						_timeOfMouseDown = Time.realtimeSinceStartup;

						Event.current.Use();
					}
					break;
				}
				case EventType.MouseUp:
				{
					if (GUI.enabled && rect.Contains(Event.current.mousePosition))
					{
						if (GUIUtility.hotControl == controlId)
						{
							_timeOfMouseDown = -1;
							GUIUtility.hotControl = 0;
							Event.current.Use();
							return true;
						}
					}
					break;
				}
			}


			return false;
		}

		public static bool SimpleImageButton(Rect rect, Texture2D icon)
		{
			int controlId = GUIUtility.GetControlID(FocusType.Passive, rect);
			switch (Event.current.type)
			{
				case EventType.Repaint:
					{
						var oldColor = GUI.color;
						if (!GUI.enabled)
							GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
						if (icon != null)
							GUI.DrawTexture(rect, icon, ScaleMode.StretchToFill);
						GUI.color = oldColor;

						break;
					}
				case EventType.MouseDown:
					{
						if (GUI.enabled && rect.Contains(Event.current.mousePosition))
						{
							GUIUtility.hotControl = controlId;
							Event.current.Use();
						}
						break;
					}
				case EventType.MouseUp:
					{
						if (GUI.enabled && rect.Contains(Event.current.mousePosition))
						{
							if (GUIUtility.hotControl == controlId)
							{
								GUIUtility.hotControl = 0;
								Event.current.Use();
								return true;
							}
						}
						break;
					}
			}


			return false;
		}

		public static bool Checkbox(bool isChecked, string text)
		{
			bool wasHeld = false;
			if (Button(
				isChecked ? DebugConsole.Instance.ImageFiles._CheckboxChecked : DebugConsole.Instance.ImageFiles._CheckboxUnchecked,
				null, text, (int)(GUIStyles.ScaleFactor * 80f), out wasHeld))
				return !isChecked;
			return isChecked;
		}

		public static bool HighlightHeaderButton(Texture2D icon, string text, bool isHighlighted, bool isActive, out bool wasButtonHeldDown)
		{
			var backgroundImage = isHighlighted ? DebugConsole.Instance.ImageFiles._HighlightGradient : DebugConsole.Instance.ImageFiles._BackgroundGradient;
			if (isActive)
				backgroundImage = DebugConsole.Instance.ImageFiles._ActiveGradient;
			return HeaderButton(icon, backgroundImage, text, out wasButtonHeldDown);
		}


		public static bool ToggleHeaderButton(Texture2D icon, string text, bool value)
		{
			bool wasButtonHeldDown;
			return ToggleHeaderButton(icon, text, value, out wasButtonHeldDown);
		}

		public static bool ToggleHeaderButton(Texture2D icon, string text, bool value, out bool wasButtonHeldDown)
		{
			return HighlightHeaderButton(icon, text, value, false, out wasButtonHeldDown) ? !value : value;
		}

		public static bool HeaderButton(Texture2D icon)
		{
			bool wasButtonHeldDown;
			return HeaderButton(icon, DebugConsole.Instance.ImageFiles._BackgroundGradient, "", out wasButtonHeldDown);
		}

		public static bool OverlayHeaderButton(Texture2D icon, string text, bool value)
		{
			bool wasButtonHeldDown;
			return Widgets.HighlightHeaderButton(icon, text, false, value, out wasButtonHeldDown) ? !value : value;
		}

		public static bool HeaderButton(Texture2D icon, Texture2D backgroundImage, string text)
		{
			bool wasButtonHeldDown;
			return HeaderButton(icon, backgroundImage, text, out wasButtonHeldDown);
		}

		public static bool HeaderButton(Texture2D icon, Texture2D backgroundImage, string text, out bool wasButtonHeldDown)
		{
			int iconSize = 0;
			if (icon != null)
				iconSize = (int)(GUIStyles.HeaderHeight * 0.75f);
			return Widgets.Button(icon, backgroundImage, text, iconSize, out wasButtonHeldDown);
		}

		public static void TouchFriendlyTextField(TouchScreenKeyboardInput touchScreenKeyboardInput, GUIStyle style, params GUILayoutOption[] options)
		{
			var oldColor = GUI.color;
			if (PlatformUtils.IsMobile)
			{
				GUI.enabled = false;
				GUI.color = new Color(1, 1, 1, 2);
			}

			touchScreenKeyboardInput.Text = DebugConsole.Instance.DetectCloseKeyInInputText(FixedSizeTextField(touchScreenKeyboardInput.Text, style));
			GUI.enabled = true;
			GUI.color = oldColor;

			var rect = GUILayoutUtility.GetLastRect();

			if (Event.current.type == EventType.Repaint)
			{
				if (TouchScreenKeyboardManager.IsActive(touchScreenKeyboardInput))
				{
					// For some reason the cursor is drawn off the bottom of the box unless the height is halved
					GUIStyles.InputTextFieldStyle.DrawCursor(new Rect(rect.x, rect.y, rect.width, rect.height), new GUIContent(touchScreenKeyboardInput.Text), GUIUtility.hotControl, touchScreenKeyboardInput.Text.Length);
				}
			}
			else if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
			{
				TouchScreenKeyboardManager.Activate(touchScreenKeyboardInput);
			}
		}

		public static string FixedSizeTextField(string text, GUIStyle style, params GUILayoutOption[] options)
		{
			var rect = GUILayoutUtility.GetRect(100, 10000, 0, 1000, style, options);

			return GUI.TextField(rect, text, style);
		}
	}
}