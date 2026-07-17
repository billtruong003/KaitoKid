using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Opencoding.CommandHandlerSystem;
using Opencoding.Console.Scripts.UI.Utilities;
using Opencoding.Console.TouchDetectors;
using Opencoding.LogHistory;
using Opencoding.Shared.Utils;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Opencoding.Console
{
	/// <summary>
	/// This is the main class that handles all the rendering and opening and closing of the console.
	/// Everything you see visually though is rendered by separate UI classes.
	/// </summary>
	public class DebugConsole : MonoBehaviour
	{
		private static readonly Vector2 OFF_SCREEN_MOUSE_POSITION = new Vector2(-1, -1);

		[SerializeField] private ImageFilesContainer _imageFiles = null;
		[SerializeField] private EmailTemplateFilesContainer _emailTemplateFiles = null;
		[SerializeField] private Settings _settings = null;

		private LogHistory.LogHistory _logHistory;
		private FilteredLogHistoryViewModel _logHistoryViewModel;
		private InputHistory _inputHistory;

		// UI Elements
		private HeaderBar _headerBar;
		private FilterBar _filterBar;
		private SettingsOverlay _settingsOverlay;
		private LogHistoryView _logHistoryView;
		private LogItemPopupMenu _logItemPopupMenu;
		private InputField _inputField;
		private CommandInformation _commandInformation;
		private Suggestions _suggestions;
		private HelpOverlay _helpOverlay;

		private TouchDetector _touchDetector;

		// The last few commands you've run
		private List<string> _recentCommands = new List<string>();
		private int _previousScreenHeight;
		private int _previousScreenWidth;
		private bool _visible;
		private float _timescaleWhenConsoleOpened;

		private LogSearch _logSearch;

		public static Func<Email, bool> CompleteLogEmailPreprocessor;
		public static Func<IEnumerable<SaveFileData>> SaveFileProvider;
		public static Func<IEnumerable<KeyValuePair<string, string>>> GameInfoProvider;
		public static Func<bool> ConsoleAboutToOpen;
		public static Func<bool> ConsoleAboutToClose;

		private float _consoleTop = 0;

		[NonSerialized] private bool _hasInitialized;

		private static MonoBehaviour _eventSystemWhenConsoleOpened = null;

		public static DebugConsole Instance { get; private set; }

		public bool ShowOnException { get; set; }

		public Settings Settings
		{
			get { return _settings; }
		}

		public EmailTemplateFilesContainer EmailTemplateFiles
		{
			get { return _emailTemplateFiles; }
		}

		public ImageFilesContainer ImageFiles
		{
			get { return _imageFiles; }
		}

		public bool JustMadeVisible { get; set; }

		// This is static to make it easier to call from game code
		public static bool IsVisible
		{
			get { return Instance != null && Instance._IsVisible; }
			set
			{
				if (Instance == null)
					throw new InvalidOperationException("DebugConsole has not been loaded yet");

				Instance._IsVisible = value;
			}
		}

		private bool _IsVisible
		{
			get { return _visible; }
			set
			{
				if (!_visible && value)
				{
					if (ConsoleAboutToOpen == null || ConsoleAboutToOpen())
					{
						if (Settings.PauseGameWhenOpen)
						{
							_timescaleWhenConsoleOpened = Time.timeScale;
							Time.timeScale = 0.0f;
						}

						DisableUGUIEventSystem();
						Instance.JustMadeVisible = true;
						Instance._visible = true;
						StartCoroutine(AnimateConsoleIn());
					}
				}
				else if (Instance._visible && !value)
				{
					if (ConsoleAboutToClose == null || ConsoleAboutToClose())
					{
						if (Settings.PauseGameWhenOpen)
						{
							Time.timeScale = _timescaleWhenConsoleOpened;
						}

						Rect = new Rect();
						EnableUGUIEventSystem();
						Instance.OnConsoleHidden();
						Instance._visible = false;
					}
				}
			}
		}

		public void ShowConsoleInstantly()
		{
			_IsVisible = true;
			_consoleTop = 0;
		}

		private IEnumerator AnimateConsoleIn()
		{
			_consoleTop = -GetConsoleHeight();
			float lastTime = Time.realtimeSinceStartup;
			while (true)
			{
				float deltaTime = Time.realtimeSinceStartup - lastTime;
				lastTime = Time.realtimeSinceStartup;

				_consoleTop = Mathf.Lerp(_consoleTop, 0, deltaTime * 12.0f);
				if (_consoleTop >= -0.01f)
					yield break;
				yield return null;
			}
		}

		private static PropertyInfo _currentUGUIEventSystemProperty = null;

		private static void CacheCurrentUGUIEventSystemProperty()
		{
			if (_currentUGUIEventSystemProperty != null)
				return;

			var eventSystemType = Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
			if (eventSystemType == null)
				return;

			_currentUGUIEventSystemProperty = eventSystemType.GetProperty("current");
		}

		private static void DisableUGUIEventSystem()
		{
			if (!Instance.Settings.DisableUGUIWhenOpen)
				return;

			CacheCurrentUGUIEventSystemProperty();

			if (_currentUGUIEventSystemProperty == null)
				return;

			_eventSystemWhenConsoleOpened = (MonoBehaviour)_currentUGUIEventSystemProperty.GetGetMethod(true).Invoke(null, null);

			if (_eventSystemWhenConsoleOpened != null)
				_eventSystemWhenConsoleOpened.enabled = false;
		}

		private static void EnableUGUIEventSystem()
		{
			if (!Instance.Settings.DisableUGUIWhenOpen)
				return;

			CacheCurrentUGUIEventSystemProperty();

			if (_currentUGUIEventSystemProperty == null)
				return;

			if (_eventSystemWhenConsoleOpened != null)
				_eventSystemWhenConsoleOpened.enabled = true;
		}

		public bool IsMaximized { get; set; }
		public Rect Rect { get; private set; }

		public IEnumerable<string> RecentCommands
		{
			get { return _recentCommands; }
		}

		public bool IsPopupMenuOpen
		{
			get { return _logItemPopupMenu.IsVisible; }
		}

		public float ConsoleTop
		{
			get { return _consoleTop; }
			set { _consoleTop = value; }
		}

		public TouchDetector CustomTouchDetector
		{
			get { return _touchDetector; }
			set
			{
				if (PlatformUtils.IsMobile)
					_touchDetector = value;
			}
		}

		public void SelectBuiltInTouchDetector(Settings.SelectedTouchDetector touchDetector)
		{
			if (PlatformUtils.IsMobile)
			{
				switch (touchDetector)
				{
					case Settings.SelectedTouchDetector.TWO_FINGER_SWIPE_DOWN:
						_touchDetector = new TwoFingerSwipeTouchDetector();
						break;
					case Settings.SelectedTouchDetector.THREE_FINGERS_HELD:
						_touchDetector = new ThreeFingersHeldTouchDetector();
						break;
					case Settings.SelectedTouchDetector.NONE:
						_touchDetector = null;
						break;
				}
			}
		}

		public int ConsoleWidth { get; set; }

		private Rect _safeArea;
		private int _screenWidth;
		private int _screenHeight;
		private bool _showLogHistory;

		protected void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}

			Initialize();
		}

		void Start()
		{
			UpdateScreenSize();
		}

		private void UpdateScreenSize()
		{
			if (_screenWidth == Screen.width && _screenHeight == Screen.height)
				return;

			_screenWidth = Screen.width;
			_screenHeight = Screen.height;

			_safeArea = Screen.safeArea;
		}

		private void Initialize()
		{
			if (_settings.DontDestroyOnLoad)
				DontDestroyOnLoad(gameObject);

			Instance = this;
			ShowOnException = _settings.ShowOnException;

			_logHistory = new LogHistory.LogHistory();
			_logHistoryViewModel = new FilteredLogHistoryViewModel(_logHistory);
			_logSearch = new LogSearch(_logHistoryViewModel);

			InitUIElements();

			SelectBuiltInTouchDetector(Settings.TouchDetector);

			_recentCommands = PlayerPrefs.GetString("Opencoding.Console.RecentConsoleCommands").Split(',').Where(x => x != "").ToList();

			CommandHandlers.CommandExecuted += UpdateRecentCommandsListAfterCommandExecuted;

			_logHistory.LogHistoryItemAdded += OnLogHistoryItemAdded;

			_hasInitialized = true;

#if ENABLE_INPUT_SYSTEM
			Keyboard.current.onTextInput += HandleKeyInput;
#endif
		}

		private void InitUIElements()
		{
			_logItemPopupMenu = new LogItemPopupMenu(this);
			_logHistoryView = new LogHistoryView(_logHistoryViewModel, _logItemPopupMenu, this, _logSearch);
			_inputHistory = new InputHistory();
			_helpOverlay = new HelpOverlay(this);

			if (PlatformUtils.IsDesktop)
				_inputField = new KeyboardInputField(_imageFiles, _inputHistory, _logHistoryView, _helpOverlay);
			else
				_inputField = new TouchScreenInputField(_inputHistory, _logHistoryView, _helpOverlay);

			_commandInformation = new CommandInformation(this, _inputField);
			_suggestions = new Suggestions(_inputField);
			_filterBar = new FilterBar(this, _logHistoryViewModel, _logSearch, _logHistoryView);
			_settingsOverlay = new SettingsOverlay(this);
			_headerBar = new HeaderBar(this, _logHistoryViewModel, _inputField, _filterBar, _settingsOverlay, _logHistory,
				_settings);
		}

		protected void OnDestroy()
		{
			if (Instance == this)
				Instance = null;

			if (_logHistory != null)
				_logHistory.Dispose();

			if (_helpOverlay != null)
				_helpOverlay.Dispose();

			if (_logHistoryView != null)
				_logHistoryView.Dispose();

			if (_logHistoryViewModel != null)
				_logHistoryViewModel.Dispose();
		}

		protected void Update()
		{
			if (!_hasInitialized)
				Initialize();

			if (Application.isEditor)
			{
				if (Settings.HasChangedSinceLastCall())
				{
					GUIStyles.InvalidateSkin();
				}
			}

			_filterBar.Update();
			_logHistory.Update();
			_logHistoryView.Update();

#if !ENABLE_INPUT_SYSTEM
			var inputString = Input.inputString;

			if (inputString.Length != 0)
			{
				for (int i = 0; i < inputString.Length; ++i)
				{
					var inputCharacter = inputString[i];
					HandleKeyInput(inputCharacter);
				}
			}
#endif

			if (_touchDetector != null && _touchDetector.Update())
			{
				IsVisible = true;
			}

			_suggestions.Update();
		}

		private void HandleKeyInput(char inputCharacter)
		{
			for (int index = 0; index < _settings.OpenAndCloseKeys.Length; index++)
			{
				var character = _settings.OpenAndCloseKeys[index];
				if (inputCharacter == character)
				{
					IsVisible = !IsVisible;
					break;
				}
			}
		}

		// Unity 5.2 and 5.2.1 before patch 3 have broken keyboard input on OS X
		public static bool IsOnUnityVersionWithBrokenKeyboardInput()
		{
			var versionSplit = Application.unityVersion.Split('p');
			var patchVersion = 0;
			if (versionSplit.Length == 2)
			{
				int.TryParse(versionSplit[1], out patchVersion);
			}

			bool isOnUnityVersionWithBrokenKeyboardInput = (Application.platform == RuntimePlatform.OSXEditor ||
			                                                Application.platform == RuntimePlatform.OSXPlayer)
			                                               &&
			                                               ((Application.unityVersion.StartsWith("5.2.1") && patchVersion < 3) ||
			                                                Application.unityVersion.StartsWith("5.2.0"));
			return isOnUnityVersionWithBrokenKeyboardInput;
		}

		private void UpdateRecentCommandsListAfterCommandExecuted(CommandHandler handler)
		{
			if (!_settings.ShowRecentCommands)
				return;

			string commandLower = handler.CommandName.ToLower();

			_recentCommands.Remove(commandLower);
			_recentCommands.Insert(0, commandLower);

			if (_recentCommands.Count > 12)
			{
				_recentCommands.RemoveAt(_recentCommands.Count - 1);
			}

			PlayerPrefs.SetString("Opencoding.Console.RecentConsoleCommands", String.Join(",", _recentCommands.ToArray()));
			PlayerPrefs.Save();
		}


		private void OnConsoleHidden()
		{
			_logHistoryView.AutoScrolling = true;
			TouchScreenKeyboardManager.Deactivate();
			HidePopups();
		}

		public void HidePopups()
		{
			_filterBar.IsVisible = false;
			_settingsOverlay.IsVisible = false;
			_helpOverlay.IsVisible = false;
			_logItemPopupMenu.IsVisible = false;
		}

		public void SetCommandInputTextAsIfTyped(string text)
		{
			_inputField.Input = text;
			_inputField.ConfirmInput();
		}

		private void OnGUI()
		{
			if (!IsVisible)
				return;

			if (Event.current.type == EventType.Layout)
			{
				TouchScreenKeyboardManager.Update();
			}

			if (_previousScreenWidth != Screen.width || _previousScreenHeight != Screen.height)
			{
				OnScreenRotated();
			}

			bool wasLogItemPopupMenuVisible = _logItemPopupMenu.IsVisible;
			GUIStyles.BeginCustomSkin(_imageFiles, _settings, _safeArea);

			var consoleWidth = ActualConsoleWidth;

			var consoleHeight = GetConsoleHeight();
			if (_helpOverlay.IsVisible)
			{
				_helpOverlay.OnGUI(new Rect(ConsoleLeft, _consoleTop + consoleHeight, consoleWidth, Screen.height - consoleHeight));
			}
			else
			{
				DrawSuggestionsUI();
			}

			Rect = new Rect(0, _consoleTop, Screen.width, consoleHeight);

			// We do this here to avoid changing the layout after the layout has happened
			if (Event.current.type == EventType.Layout)
			{
				_showLogHistory = Rect.height > GUIStyles.HeaderHeight * 2.9f;
			}

			GUI.Window(99, Rect, WindowFunc, "", GUIStyles.ConsoleWindowStyle);

			if (_logItemPopupMenu.IsVisible && _inputField.HasFocus)
			{
				_inputField.LoseFocus();
			}

			if (wasLogItemPopupMenuVisible)
			{
				_logItemPopupMenu.OnGUI();
				if (Event.current.type == EventType.MouseUp && !_logItemPopupMenu.Rect.Contains(Event.current.mousePosition))
				{
					_logItemPopupMenu.IsVisible = false;
					Event.current.Use();
				}
			}

			TouchScreenKeyboardManager.OnGUI();

			GUIStyles.EndCustomSkin();
		}

		public float ActualConsoleWidth
		{
			get { return ConsoleWidth == 0 ? _safeArea.width : ConsoleWidth; }
		}

		public float ConsoleLeft
		{
			get { return _safeArea.xMin; }
		}

		private void DrawSuggestionsUI()
		{
			var position = _commandInformation.OnGUI();
			float maxWidth = ActualConsoleWidth - 80 * GUIStyles.ScaleFactor;
			float maxHeight = Screen.height - position.y - 10;
			if (PlatformUtils.IsMobile)
			{
				// On mobile, the UI needs to fit between the console and the keyboard - this
				// works out how much space there is. In some cases, there won't be any room
				// at all, so it won't be shown.

				var keyboardArea = TouchScreenKeyboardManager.KeyboardArea;
				if (keyboardArea != null && ((Rect)keyboardArea).height > 0)
				{
					maxWidth -= TouchScreenKeyboardManager.GetTouchScreenKeyboardCloseButtonRect().width;
					maxHeight -= ((Rect)keyboardArea).height;
				}
			}

			_suggestions.Draw(new Rect(position.x, _consoleTop + position.y, maxWidth, maxHeight));
		}

		public float GetConsoleHeight()
		{
			float maximizedConsoleHeight = Screen.height;
			float minimizedConsoleHeight = Screen.height / 2.5f;
			float minimumConsoleHeight = GUIStyles.HeaderHeight * 3;

			float consoleHeight = IsMaximized ? maximizedConsoleHeight : minimizedConsoleHeight;

			if (!IsMaximized && _filterBar.IsVisible)
				consoleHeight += _filterBar.Height;

			if (TouchScreenKeyboardManager.IsAnyInputActive && PlatformUtils.IsMobile)
			{
				var keyboardArea = TouchScreenKeyboardManager.KeyboardArea;
				if (keyboardArea != null)
				{
					var keyboardAreaRect = (Rect)keyboardArea;
					var availableHeight =
						Screen.height - keyboardAreaRect.height -
						GUIStyles.SuggestionButtonHeight * 4f; // GUIStyles.SuggestionButtonHeight isn't quite the right height
					if (availableHeight < consoleHeight)
					{
						if (_filterBar.IsVisible && _filterBar.Height > availableHeight / 4)
							consoleHeight = Screen.height - keyboardAreaRect.height;
						else
							consoleHeight = availableHeight;
					}
				}
			}

			if (consoleHeight < minimumConsoleHeight)
				consoleHeight = GUIStyles.HeaderHeight * 2;
			return consoleHeight;
		}


		private void OnScreenRotated()
		{
			bool inputFieldHasFocus = _inputField.HasFocus;
			_settingsOverlay.IsVisible = false;
			_logItemPopupMenu.IsVisible = false;
			_inputField.LoseFocus();
			_previousScreenWidth = Screen.width;
			_previousScreenHeight = Screen.height;
			UpdateScreenSize();
			GUIStyles.InvalidateSkin();
			_suggestions.ForceRelayoutNextUpdate();
			if (inputFieldHasFocus)
				_inputField.Focus();
		}

		private void OnLogHistoryItemAdded(LogHistoryItem logHistoryItem)
		{
			if (logHistoryItem._Type == LogHistoryLogType.Exception && ShowOnException)
			{
				IsVisible = true;
			}
		}

		private void WindowFunc(int windowId)
		{
			bool settingsOverlayWasVisible = _settingsOverlay.IsVisible;
			bool isCursorOverSettingsOverlay = _settingsOverlay.IsVisible &&
			                                   _settingsOverlay.Rect.Contains(Event.current.mousePosition);
			bool inFocus = !_logItemPopupMenu.IsVisible && !isCursorOverSettingsOverlay;

			if (!inFocus)
			{
				// Makes the other windows modal by preventing input to the console when they're open
				if (Event.current.isKey || Event.current.isMouse || Event.current.type == EventType.ScrollWheel)
				{
					if (settingsOverlayWasVisible)
						_settingsOverlay.OnGUI();
					return;
				}

				Event.current.mousePosition = OFF_SCREEN_MOUSE_POSITION;
			}

			if (_settingsOverlay.IsVisible)
			{
				// Detect clicks outside the settings menu when it is open and close it
				if (Event.current.type == EventType.MouseUp &&
				    !_settingsOverlay.Rect.Contains(Event.current.mousePosition) &&
				    !_settingsOverlay.SettingsButtonRect.Contains(Event.current.mousePosition))
				{
					_settingsOverlay.IsVisible = false;
					Event.current.Use();
				}
			}

			if (_helpOverlay.IsVisible)
			{
				// Detect clicks outside the help overlay when it is open and close it
				if (Event.current.type == EventType.MouseUp &&
				    !_inputField.HelpButtonRect.Contains(Event.current.mousePosition))
				{
					_helpOverlay.IsVisible = false;
				}
			}

			DrawMainWindow(windowId, inFocus);

			if (settingsOverlayWasVisible)
				_settingsOverlay.OnGUI();
		}

		private void DrawMainWindow(int windowId, bool inFocus)
		{
			GUILayout.BeginVertical(GUIStyles.ConsoleWindowBackgroundStyle, GUILayout.Width(ActualConsoleWidth));

			_headerBar.OnGUI();
			_filterBar.OnGUI();
			if (_showLogHistory)
				_logHistoryView.OnGUI(inFocus);
			Widgets.HorizontalSeparator();
			_inputField.OnGUI(Rect);

			GUILayout.EndVertical();

			if (inFocus)
			{
				GUI.FocusWindow(windowId);
				GUI.BringWindowToFront(windowId);
			}
		}

		public void Minimize()
		{
			IsMaximized = false;
		}

		// Detect the close key in a string, and if we find it we close the console.
		// We have to do this because text boxes swallow all key down messages so we can't
		// detect them that way.
		public string DetectCloseKeyInInputText(string input)
		{
			if (_settings.OpenAndCloseKeys == "")
				return input;

			foreach (var key in _settings.OpenAndCloseKeys)
				input = input.Replace(key.ToString(CultureInfo.InvariantCulture), "");
			return input;
		}

		public void Clear()
		{
			_logHistory.Clear();
			_logHistoryView.AutoScrolling = true;
		}

		public void EmailLog(Action<string> callback = null)
		{
			// Emailing a log captures a screenshot first. Because screenshots can take a couple of frames to be
			// available, the actual email sending happens via a callback.
			var filename = Application.temporaryCachePath + "/log_" + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".htm";
			_logHistory.Update(); // force log messages to be flushed to the log
			ExportLog(filename, true, () =>
			{
				EmailOrOpenLog(filename);
				if (callback != null)
					callback(filename);
			});
		}

		public void ExportLog(Stream stream, bool captureScreenshot, Action callback = null)
		{
			if (captureScreenshot)
			{
				// Emailing a log captures a screenshot first. Because screenshots can take a couple of frames to be
				// available, the actual email sending happens via a callback.
				ScreenshotCapture.CaptureScreenshot(Settings.LogScreenshotCaptureMode,
					data =>
					{
						ExportLog(data, stream);
						if (callback != null)
							callback();
					});
			}
			else
			{
				ExportLog(null, stream);
			}
		}

		public void ExportLog(string filename, bool captureScreenshot, Action callback = null)
		{
			var fs = new FileStream(filename, FileMode.Create);

			try
			{
				ExportLog(fs, captureScreenshot, () =>
				{
					fs.Close();
					if (callback != null)
						callback();
				});
			}
			catch (Exception)
			{
				fs.Close();
				throw;
			}
		}

		private void ExportLog(byte[] screenshotData, Stream stream)
		{
			var debugLogHistoryEmailView = new LogHistoryEmailView(
				EmailTemplateFiles._LogEmailHeaderTextAsset.text,
				EmailTemplateFiles._LogEmailFooterTextAsset.text,
				this);

			IEnumerable<SaveFileData> saveFile = new SaveFileData[0];
			if (SaveFileProvider != null)
				saveFile = SaveFileProvider();

			IEnumerable<KeyValuePair<string, string>> gameInfo = new KeyValuePair<string, string>[0];
			if (GameInfoProvider != null)
				gameInfo = GameInfoProvider();

			debugLogHistoryEmailView.WriteToFile(stream, screenshotData, saveFile, gameInfo);
		}

		private void EmailOrOpenLog(string filename)
		{
			if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer ||
			    Application.platform == RuntimePlatform.LinuxPlayer)
			{
				// On editor platforms, we don't actually send an email - we just open the log in a browser.
				if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
					Application.OpenURL("file:///" + filename.Replace(" ", "%20")); // Windows requires an extra / before the filename
				else
					Application.OpenURL("file://" + filename.Replace(" ", "%20"));
			}
			else
			{
				var email = new Email
				{
					Subject = "Console Log",
					Message =
						"Log file is attached. If you're viewing this in Gmail, select the 'download' option - it will be unreadable with the 'view' option.",
					Attachments = new List<Email.Attachment> { new Email.Attachment(filename, "text/html") },
					ToAddress = Settings.DefaultToEmailAddress
				};

				if (CompleteLogEmailPreprocessor == null || CompleteLogEmailPreprocessor(email))
				{
					NativeMethodsInterface.SendEmail(email);
				}
			}
		}
	}
}