using System;
using UnityEngine;

namespace Opencoding.Console
{
	public class Settings : ScriptableObject
	{
		public enum SelectedTouchDetector
		{
			NONE,
			TWO_FINGER_SWIPE_DOWN,
			THREE_FINGERS_HELD
		}

		public enum SelectedScreenshotCaptureMode
		{
			DISABLED,
			CAPTURE_MAIN_CAMERA,
			CAPTURE_FULL_SCREEN
		}

        [Tooltip("Should the console persist between scenes? It's strongly recommended to leave this enabled and to place the prefab in the first scene of your game.")]
        public new bool DontDestroyOnLoad = true;
        [Tooltip("Should the game version be set automatically based on the player settings?")]
        public bool AutoSetVersion = true;
        [Tooltip("What version is the game? This is output in log files.")]
        public string GameVersion;
        [Tooltip("If this is not empty, then the console will be removed from the build unless the define specified is set in the Player Settings. This is overridden by 'Disable if Defined' if that is set.")]
        public string EnableIfDefined = "";
        [Tooltip("If this is not empty, then the console will be removed from the build if the define specified is set in the Player Settings.")]
        public string DisableIfDefined = "FINAL_BUILD";
        [Tooltip("If this is ticked, then the console won't be included in non-development builds.")]
        public bool OnlyInDevBuilds = true;
        [Tooltip("Should the console open itself automatically when an exception occurs?")]
        public bool ShowOnException = true;
        [Tooltip("This defines how the console can be opened when on a touch-screen device.")]
		public SelectedTouchDetector TouchDetector = SelectedTouchDetector.TWO_FINGER_SWIPE_DOWN;
        [Tooltip("These are the keys that open the console on desktop builds and in the editor")]
		public string OpenAndCloseKeys = "~\\`|§±";
        [Tooltip("Should a separate filter button be shown at the top for exceptions, or should they be shown with the errors as Unity does?")]
		public bool ShowSeparateExceptionButton = true;
        [Tooltip("Should a separate filter button be shown at the top for asserts, or should they be shown with the errors as Unity does?")]
        public bool ShowSeparateAssertButton = true;
        [Tooltip("Should UGUI input be disabled when the console is open?")]
        public bool DisableUGUIWhenOpen = true;
        [Tooltip("Should the game be paused when the console is open? Sometimes this is desireable in action heavy games, but it may limit how interactive console commands can be.")]
        public bool PauseGameWhenOpen = false;
        [Tooltip("Should the last few commands you've used be remembered and shown below the console?")]
        public bool ShowRecentCommands = true;
        [Tooltip("When emailing a log file on device, what should the default 'to' address be?")]
        public string DefaultToEmailAddress = "";
		[Range(0.5f, 2.5f)]
		public float EditorScaleFactor = 1.0f;
		[Range(0.5f, 2.5f)]
		public float MobileScaleFactorPortrait = 0.8f;
		[Range(0.5f, 2.5f)]
		public float MobileScaleFactorLandscape = 1.0f;
		[Range(0.5f, 2.5f)]
		public float StandaloneScaleFactor = 1.0f;

        [Tooltip("How should a screenshot be captured? Capturing from the main camera may not work if you use multiple cameras in your game, but the alternative of capturing the full screen requires the console to be turned off for a frame while the screenshot is captured.")]
		public SelectedScreenshotCaptureMode LogScreenshotCaptureMode = SelectedScreenshotCaptureMode.CAPTURE_MAIN_CAMERA;

		
		private float _oldEditorScaleFactor = 1.0f;
		private float _oldMobileScaleFactorLandscape = 1.0f;
		private float _oldMobileScaleFactorPortrait = 1.0f;
		private float _oldStandaloneScaleFactor = 1.0f;

		public bool HasChangedSinceLastCall()
		{
			bool hasChanged = EditorScaleFactor != _oldEditorScaleFactor 
				|| MobileScaleFactorPortrait != _oldMobileScaleFactorPortrait 
				|| MobileScaleFactorLandscape != _oldMobileScaleFactorLandscape 
				|| StandaloneScaleFactor != _oldStandaloneScaleFactor;
			_oldEditorScaleFactor = EditorScaleFactor;
			_oldMobileScaleFactorPortrait = MobileScaleFactorPortrait;
			_oldMobileScaleFactorLandscape = MobileScaleFactorLandscape;
			_oldStandaloneScaleFactor = StandaloneScaleFactor;
			return hasChanged;
		}
	}
}