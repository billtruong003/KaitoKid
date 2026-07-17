using Opencoding.Shared.Utils;
using UnityEngine;

namespace Opencoding.Console
{
	public static class GUIStyles
	{
		private static GUISkin _customGUISkin;
		private static GUISkin _defaultGUISkin;

		public static GUIStyle ExceptionLabelStyle { get; private set; }
		public static GUIStyle AssertLabelStyle { get; private set; }
		public static GUIStyle ErrorLabelStyle { get; private set; }
		public static GUIStyle InfoLabelStyle { get; private set; }
		public static GUIStyle WarningLabelStyle { get; private set; }
		public static GUIStyle CommandInformationParameterNormalStyle { get; private set; }
		public static GUIStyle CommandInformationParameterHighlightedStyle { get; private set; }
		public static GUIStyle SuggestionButtonBackgroundStyle { get; private set; }
		public static GUIStyle SuggestionButtonMoreBackgroundStyle { get; private set; }
	    public static GUIStyle ConsoleWindowStyle { get; private set; }
		public static GUIStyle ConsoleWindowBackgroundStyle { get; private set; }
		public static GUIStyle CommandDescriptionBackgroundStyle { get; private set; }
		public static GUIStyle HeaderStyle { get; private set; }
		public static GUIStyle HeaderButtonLabelStyle { get; private set; }
		public static GUIStyle ItemAlternateBackgroundStyle { get; private set; }
		public static GUIStyle HighlightedItemBackgroundStyle { get; private set; }
		public static GUIStyle SearchResultItemBackgroundStyle { get; private set; }
		public static GUIStyle OverlayWindowStyle { get; private set; }
		public static GUIStyle FilterTextFieldStyle { get; private set; }
		public static GUIStyle SettingsPanelButtonStyle { get; private set; }
		public static GUIStyle FilterTextFieldHelpOverlayStyle { get; private set; }
		public static GUIStyle InputTextFieldStyle { get; private set; }
		public static GUIStyle LogHistoryItemTextAreaStyle { get; private set; }
		public static GUIStyle SearchTextFieldStyle { get; private set; }
		public static GUIStyle SearchTextFieldCountStyle { get; private set; }
		public static GUIStyle SearchFilterToolBarStyle { get; private set; }
		public static GUIStyle SelectedSearchItemBackgroundStyle { get; private set; }
		public static GUIStyle PopupMenuButtonLabelStyle { get; private set; }
		public static GUIStyle HorizontalPopupMenuButtonLabelStyle { get; private set; }
		public static GUIStyle PopupMenuWindowStyle { get; private set; }
		public static GUIStyle HelpWindowParameterListStyle { get; private set; }
		public static GUIStyle HelpWindowHelpStyle { get; private set; }
		public static GUIStyle HelpItemBackgroundStyle { get; private set; }
		public static GUIStyle HelpWindowDescriptionStyle { get; private set; }
		public static GUIStyle HelpWindowBackgroundStyle { get; private set; }

		public static float ScaleFactor { get; private set; }
		public static int ConsoleRowHeight { get; private set; }
		public static int ConsoleRowTextLeftMargin { get; private set; }
		public static float HeaderHeight { get; private set; }
		public static float SuggestionButtonHeight { get; set; }
		public static Texture2D BlackTexture { get; private set; }

		private static float _nativeScreenScaleFactor = -1.0f;

		/// <summary>
		///     Sets up the GUI styles that we need the first time we get an OnGUI.
		/// </summary>
		public static void SetUpStyles(ImageFilesContainer imageFiles, Settings settings, Rect safeArea,
		    bool force = false)
		{
			if (_customGUISkin != null && !force)
				return;

			if (_nativeScreenScaleFactor < 0)
				_nativeScreenScaleFactor = NativeMethodsInterface.GetNativeScreenScaleFactor();

			const int setupFontSize = 50;
			const int setupDPI = 440;
			const int setupScrollFixedWidth = 50;

			var dpi = (int) Screen.dpi;

			if (Application.isEditor || dpi == 0)
				dpi = 120; // 320 DPI is the DPI for an iPhone 5+, 120 looks good in the editor

			ScaleFactor = (float) dpi/setupDPI;

			if (!Application.isEditor)
			{
				// Cope with small-screen phones (there must be a neater way to do this)
				// This detects screens below 2.5 inches wide or high
				if (Screen.width * 1/Screen.dpi < 2.5 || Screen.height*1/Screen.dpi < 2.5)
				{
					ScaleFactor *= 0.75f;
				}
			}

			if (Application.isEditor)
			{
				ScaleFactor *= settings.EditorScaleFactor;
			}
			else if (PlatformUtils.IsMobile)
			{
				if(Screen.width > Screen.height)
					ScaleFactor *= settings.MobileScaleFactorLandscape;
				else
					ScaleFactor *= settings.MobileScaleFactorPortrait;
			}
			else
			{
				ScaleFactor *= settings.StandaloneScaleFactor;
			}

			ScaleFactor *= _nativeScreenScaleFactor; // This is to deal with the 'target resolution' being set to something non native on iOS

			var fontSize = (int) (setupFontSize*ScaleFactor);
			var scrollFixedWidth = (int) (setupScrollFixedWidth*ScaleFactor);

			ConsoleRowHeight = (int) (fontSize + 30*ScaleFactor);
			ConsoleRowTextLeftMargin = (int)(10 * ScaleFactor);
			HeaderHeight = ConsoleRowHeight * 1.3f;
			SuggestionButtonHeight = ScaleFactor * 100;
			if (Application.isEditor)
			{
				SuggestionButtonHeight *= 1.05f;
			}

			_customGUISkin = ScriptableObject.CreateInstance<GUISkin>();

			_customGUISkin.box = new GUIStyle(GUI.skin.box);
			_customGUISkin.button = new GUIStyle(GUI.skin.button) {padding = new RectOffset(10, 10, 10, 10), fontSize = fontSize};
			_customGUISkin.font = GUI.skin.font;
			_customGUISkin.horizontalScrollbar = new GUIStyle(GUI.skin.horizontalScrollbar);
			_customGUISkin.horizontalScrollbarLeftButton = new GUIStyle(GUI.skin.horizontalScrollbarLeftButton);
			_customGUISkin.horizontalScrollbarRightButton = new GUIStyle(GUI.skin.horizontalScrollbarRightButton);
			_customGUISkin.horizontalScrollbarThumb = new GUIStyle(GUI.skin.horizontalScrollbarThumb);
			
			_customGUISkin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider);
			_customGUISkin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb);
			_customGUISkin.label = new GUIStyle(GUI.skin.label) {fontSize = fontSize};
			_customGUISkin.scrollView = new GUIStyle(GUI.skin.scrollView);
			_customGUISkin.textArea = new GUIStyle(GUI.skin.textArea) {fontSize = fontSize};
			_customGUISkin.textField = new GUIStyle(GUI.skin.textField) {fontSize = fontSize};
			_customGUISkin.toggle = new GUIStyle(GUI.skin.toggle) {fontSize = fontSize};
			_customGUISkin.verticalScrollbar = new GUIStyle(GUI.skin.verticalScrollbar) {fixedWidth = scrollFixedWidth};
			_customGUISkin.verticalScrollbarDownButton = new GUIStyle(GUI.skin.verticalScrollbarDownButton);
			_customGUISkin.verticalScrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb);
			_customGUISkin.verticalScrollbarUpButton = new GUIStyle(GUI.skin.verticalScrollbarUpButton);
			_customGUISkin.verticalScrollbarThumb.fixedWidth = scrollFixedWidth;
			_customGUISkin.verticalSlider = new GUIStyle(GUI.skin.verticalSlider);
			_customGUISkin.verticalSliderThumb = new GUIStyle(GUI.skin.verticalSliderThumb);
			_customGUISkin.window = new GUIStyle(GUI.skin.window);
			SetBackgroundForAllStyleStates(_customGUISkin.verticalScrollbarThumb, imageFiles._ScrollbarThumb);

			var borderlessLabelStyle = new GUIStyle(GUI.skin.label)
			{
				padding = new RectOffset(0, 0, 0, 0),
				margin = new RectOffset(0, 0, 0, 0),
				fontSize = fontSize,
				wordWrap = false,
				alignment = TextAnchor.MiddleLeft
			};
			ExceptionLabelStyle = new GUIStyle(borderlessLabelStyle) { normal = { textColor = new Color32(255, 0, 108, 255) } };
			AssertLabelStyle = new GUIStyle(borderlessLabelStyle) { normal = { textColor = new Color32(48, 163, 255, 255) } };
			ErrorLabelStyle = new GUIStyle(borderlessLabelStyle) { normal = { textColor = Color.red } };
			InfoLabelStyle = new GUIStyle(borderlessLabelStyle) { normal = { textColor = Color.white } };
			WarningLabelStyle = new GUIStyle(borderlessLabelStyle) { normal = { textColor = Color.yellow } };

			HeaderButtonLabelStyle = new GUIStyle(borderlessLabelStyle)
			{
				stretchHeight = true,
				padding = new RectOffset(5, 5, 3, 3),
				fontSize = (int) (HeaderHeight*0.5f)
			};

			PopupMenuButtonLabelStyle = new GUIStyle(borderlessLabelStyle)
			{
				stretchHeight = true,
				padding = new RectOffset((int) (50 * ScaleFactor), (int) (25 * ScaleFactor), (int) (15 * ScaleFactor), (int) (15 * ScaleFactor)),
				fontSize = (int)(HeaderHeight * 0.5f),
				hover =
				{
					background = UIUtilities.CreateTexture(new Color(0, 0, 0.6f, 0.4f)),
					textColor = Color.white
				}
			};
			
			HorizontalPopupMenuButtonLabelStyle = new GUIStyle(PopupMenuButtonLabelStyle)
			{
				alignment = TextAnchor.MiddleCenter,
				padding = new RectOffset((int)(25 * ScaleFactor), (int)(25 * ScaleFactor), (int)(15 * ScaleFactor), (int)(15 * ScaleFactor))
			};

			HelpWindowParameterListStyle = new GUIStyle(borderlessLabelStyle)
			{
				fontSize = fontSize,
				padding = new RectOffset(8, 8, 12, 8),
				richText = true,
				alignment = TextAnchor.MiddleLeft,
				wordWrap = true
			};

			HelpWindowHelpStyle = new GUIStyle(borderlessLabelStyle)
			{
				fontSize = fontSize,
				padding = new RectOffset(8, 8, 8, 8),
				fontStyle = FontStyle.Bold,
				wordWrap = true
			};

			HelpWindowDescriptionStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = fontSize,
				padding = new RectOffset(30, 2, 2, 2)
			};

			HelpItemBackgroundStyle = new GUIStyle(GUI.skin.label)
			{
				normal =
				{
					textColor = new Color(1, 1, 1, 1),
					background = imageFiles._SuggestionButtonBackground
				},
				padding = new RectOffset(0,0,0,0),
				alignment = TextAnchor.MiddleLeft,
				border = new RectOffset(7, 7, 7, 7),
				fontSize = fontSize,
				wordWrap = false
			};

			CommandInformationParameterNormalStyle = new GUIStyle(borderlessLabelStyle)
			{
				fontSize = fontSize,
				padding = new RectOffset(3, 3, 3, 3),
				normal = { background = UIUtilities.CreateTexture(new Color(0.15f, 0.15f, 0.15f, 0.90f)) },
				richText = true,
				alignment = TextAnchor.MiddleLeft,
				wordWrap = true
			};

			CommandInformationParameterHighlightedStyle = new GUIStyle(CommandInformationParameterNormalStyle)
			{
				normal =
				{
					textColor = new Color(0, 1, 0, 1),
					background = UIUtilities.CreateTexture(new Color(0.0f, 0.3f, 0.0f, 0.80f))
				},
				padding = new RectOffset(3, 3, 3, 3),
				fontSize = fontSize,
				wordWrap = false,
				richText = true
			};

			CommandDescriptionBackgroundStyle = new GUIStyle(GUI.skin.label)
			{
				padding = new RectOffset(4, 4, 4, 4),
				normal = {background = UIUtilities.CreateTexture(new Color(0.15f, 0.15f, 0.15f, 0.90f))},
				fontSize = fontSize,
				alignment = TextAnchor.MiddleLeft,
				wordWrap = true
			};

			SuggestionButtonBackgroundStyle = new GUIStyle(GUI.skin.label)
			{
				padding = new RectOffset(8, 8, 4, 4),
				margin = new RectOffset(10, 10, 10, 10),
				normal =
				{
					textColor = new Color(1, 1, 1, 1),
					background = imageFiles._SuggestionButtonBackground
				},
				alignment = TextAnchor.MiddleLeft,
				border = new RectOffset(7, 7, 7, 7),
				fontSize = fontSize,
				wordWrap = false
			};

			SuggestionButtonMoreBackgroundStyle = new GUIStyle(GUI.skin.label)
			{
				padding = new RectOffset(8, 8, 8, 8),
				normal =
				{
					textColor = new Color(1, 1, 1, 1),
					background = UIUtilities.CreateTexture(new Color(0.15f, 0.15f, 0.15f, 0.40f))
				},
				alignment = TextAnchor.MiddleLeft,
				fontSize = fontSize
			};

			ConsoleWindowBackgroundStyle = new GUIStyle("box")
			{
				margin = new RectOffset(0, 0, 0, 0),
				padding = new RectOffset((int)safeArea.xMin, (int) (Screen.width - safeArea.xMax), (int) (Screen.height - safeArea.yMax), 0),
				normal = {background = UIUtilities.CreateTexture(new Color(0.15f, 0.15f, 0.15f, 0.90f))},
				fontSize = fontSize
			};

		    ConsoleWindowStyle = new GUIStyle("box")
		    {
		        margin = new RectOffset(0, 0, 0, 0),
		        padding = new RectOffset(0, 0, 0, 0),
		        normal = {background = UIUtilities.CreateTexture(new Color(0.15f, 0.15f, 0.15f, 0.90f))},
		        fontSize = fontSize
		    };

			HelpWindowBackgroundStyle = new GUIStyle(ConsoleWindowBackgroundStyle)
			{
				padding = new RectOffset((int) (ScaleFactor*50), (int) (ScaleFactor*50), 0, 0),
				normal = { background = UIUtilities.CreateTexture(new Color(0.278f, 0.278f, 0.278f, 1.0f)) },
			};

			ItemAlternateBackgroundStyle = new GUIStyle
			{
				normal = {background = UIUtilities.CreateTexture(new Color(0.0f, 0.0f, 0.0f, 0.1f))}
			};

			HighlightedItemBackgroundStyle = new GUIStyle
			{
				normal = {background = UIUtilities.CreateTexture(new Color(1.0f, 1.0f, 1.0f, 0.1f))}
			};

			SearchResultItemBackgroundStyle = new GUIStyle
			{
				normal = { background = UIUtilities.CreateTexture(new Color(1.0f, 1.0f, 0.0f, 0.05f)) }
			};

			SelectedSearchItemBackgroundStyle = new GUIStyle
			{
				normal = { background = UIUtilities.CreateTexture(new Color(1.0f, 1.0f, 0.0f, 0.2f)) }
			};

			HeaderStyle = new GUIStyle { normal = { background = imageFiles._BackgroundGradient } };

			OverlayWindowStyle = new GUIStyle(GUI.skin.window)
			{
				border = new RectOffset(8, 8, 2, 8),
				padding = new RectOffset(9, 9, 1, 9)
			};
			SetBackgroundForAllStyleStates(OverlayWindowStyle, imageFiles._SettingsPopupBackground);

			PopupMenuWindowStyle = new GUIStyle(OverlayWindowStyle)
			{
				border = new RectOffset(8, 8, 2, 8),
				padding = new RectOffset(6, 6, 0, 6)
			};

			FilterTextFieldStyle = new GUIStyle(GUI.skin.textField)
			{
				alignment = TextAnchor.MiddleLeft,
				padding = new RectOffset((int) (10*ScaleFactor), (int) (10*ScaleFactor), 2, 2),
				fontSize = (int) (HeaderHeight*0.5f)
			};

			FilterTextFieldHelpOverlayStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = FilterTextFieldStyle.alignment,
				padding = FilterTextFieldStyle.padding,
				fontSize = FilterTextFieldStyle.fontSize,
				fontStyle = FontStyle.Italic,
				normal = {textColor = Color.gray},
			};

			SearchTextFieldStyle = new GUIStyle(GUI.skin.textField)
			{
				alignment = TextAnchor.MiddleLeft,
				fontSize = (int)(HeaderHeight * 0.5f)
			};

			SearchTextFieldCountStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.MiddleRight,
				fontSize = SearchTextFieldStyle.fontSize,
				normal = { textColor = Color.gray },
			};

			InputTextFieldStyle = new GUIStyle(GUI.skin.textField)
			{
				alignment = TextAnchor.MiddleLeft,
				padding = new RectOffset((int) (10*ScaleFactor), (int) (10*ScaleFactor), 2, 2),
				fontSize = (int) (HeaderHeight*0.5f),
				margin = new RectOffset(5, 3, 3, 4)
			};

			SettingsPanelButtonStyle = new GUIStyle(GUI.skin.button)
			{
				fontSize = HeaderButtonLabelStyle.fontSize,
				padding = new RectOffset(5, 5, 5, 5)
			};

			LogHistoryItemTextAreaStyle = new GUIStyle(GUI.skin.textArea)
			{
				fontSize = fontSize,
				padding = new RectOffset(),
				margin = new RectOffset(),
				richText = true
			};

			SearchFilterToolBarStyle = new GUIStyle()
			{
				padding = new RectOffset((int)(10 * ScaleFactor), (int)(10 * ScaleFactor), (int)(10 * ScaleFactor), (int)(10 * ScaleFactor))
			};

			SetBackgroundForAllStyleStates(LogHistoryItemTextAreaStyle, UIUtilities.CreateTexture(Color.clear));

			BlackTexture = UIUtilities.CreateTexture(Color.black);
		}

		private static void SetBackgroundForAllStyleStates(GUIStyle style, Texture2D texture)
		{
			style.normal.background = texture;
			style.hover.background = texture;
			style.active.background = texture;
			style.onNormal.background = texture;
			style.onHover.background = texture;
			style.onActive.background = texture;
			style.focused.background = texture;
			style.onFocused.background = texture;
		}

		public static void BeginCustomSkin(ImageFilesContainer imageFiles, Settings settings, Rect safeArea)
		{
			SetUpStyles(imageFiles, settings, safeArea);
			_defaultGUISkin = GUI.skin;
			GUI.skin = _customGUISkin;
		}

		public static void EndCustomSkin()
		{
			GUI.skin = _defaultGUISkin;
		}

		public static void InvalidateSkin()
		{
			_customGUISkin = null;
		}
	}
}