using Opencoding.Shared.Utils;
using UnityEngine;

namespace Opencoding.Console
{
	/// <summary>
	/// This class acts as the data storage for a text input field (or anything else that needs to take
	/// text input from the touch screen keyboard).
	/// This combined with the TouchScreenKeyboardManager class solves the problem of making sure that only
	/// a single TouchScreenKeyboard instance is active at a time by encapsulating it.
	/// </summary>
	class TouchScreenKeyboardInput
	{
		private string _text = "";
		public string Text
		{
			get { return _text; }
			set
			{
				_text = value;
				TouchScreenKeyboardManager.InputChanged(this);
			}
		}

		public bool IsDone
		{
			get;
			set;
		}

		public bool WasCancelled
		{
			get;
			set;
		}

		// To avoid notifying the keyboard again that the text has changed
		// If this method isn't used, then the iPad (but not iPhone) keyboard breaks
		public void SetTextFromKeyboard(string text)
		{
			_text = text ?? "";
		}
	}

	static class TouchScreenKeyboardManager
	{
		private static TouchScreenKeyboard _touchScreenKeyboard;
		private static TouchScreenKeyboardInput _currentInput;

		/// <summary>
		/// This should be called once a frame to update the current TouchScreenKeyboardInput
		/// with whatever text has been typed on the keyboard
		/// </summary>
		public static void Update()
		{
			if (!PlatformUtils.IsMobile)
				return;
			
			if (_currentInput != null && _touchScreenKeyboard != null)
			{
				_currentInput.SetTextFromKeyboard(_touchScreenKeyboard.text);
			}

			if (_touchScreenKeyboard != null)
			{
				if (_touchScreenKeyboard.status == TouchScreenKeyboard.Status.Done)
				{
					_touchScreenKeyboard = null;
					if (_currentInput != null)
						_currentInput.IsDone = true;
				}
				else if (_touchScreenKeyboard.status == TouchScreenKeyboard.Status.Canceled || _touchScreenKeyboard.status == TouchScreenKeyboard.Status.LostFocus  || !_touchScreenKeyboard.active)
				{
					_touchScreenKeyboard = null;
					if (_currentInput != null)
						_currentInput.WasCancelled = true;
				}
			}

			if (_currentInput == null && _touchScreenKeyboard != null)
			{
				_touchScreenKeyboard.active = false;
				_touchScreenKeyboard = null;
			}
		}

		public static void OnGUI()
		{
			if (!PlatformUtils.IsMobile)
				return;

			if (_touchScreenKeyboard == null) 
				return;

			var rect = GetTouchScreenKeyboardCloseButtonRect();
			if (Widgets.SimpleImageButton(rect, DebugConsole.Instance.ImageFiles._CloseKeyboardIcon))
			{
				Deactivate();
			}
		}

		public static Rect GetTouchScreenKeyboardCloseButtonRect()
		{
			if (!PlatformUtils.IsMobile)		
				return new Rect(0, 0, 0, 0);

			var kbRect = TouchScreenKeyboard.area;
			int buttonSize = (int) (GUIStyles.ScaleFactor*100.0f);
			return new Rect(kbRect.xMax - buttonSize, kbRect.yMin - buttonSize, buttonSize, buttonSize);
		}

		/// <summary>
		/// This should only be called from the TouchScreenKeyboardInput class.
		/// </summary>
		/// <param name="input"></param>
		public static void InputChanged(TouchScreenKeyboardInput input)
		{
			if (!PlatformUtils.IsMobile)
				return;

			if (input == _currentInput)
			{
				if (_touchScreenKeyboard != null)
					_touchScreenKeyboard.text = input.Text;
			}
		}

		/// <summary>
		/// This should be called when the TouchScreenKeyboardInput should start receving input
		/// from the keyboard.
		/// </summary>
		/// <param name="input"></param>
		public static void Activate(TouchScreenKeyboardInput input)
		{
			if (!PlatformUtils.IsMobile)
				return;

			if (_touchScreenKeyboard == null)
			{
#if !UNITY_ANROID || !UNITY_2022_1_OR_NEWER
				// Setting this on Unity 2022 on Android causes the keyboard not to show
				TouchScreenKeyboard.hideInput = true;
#endif
				_touchScreenKeyboard = TouchScreenKeyboard.Open(input.Text, TouchScreenKeyboardType.Default, false, false, false, true);
				input.IsDone = false;
				input.WasCancelled = false;
			}
			else
			{
				_touchScreenKeyboard.text = input.Text;
			}

			_currentInput = input;
		}

		/// <summary>
		/// This checks if the specified TouchScreenKeyboardInput is active or not.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static bool IsActive(TouchScreenKeyboardInput input)
		{
			return _currentInput == input;
		}

		/// <summary>
		/// This checks if any input is currently receving input (and by extension, if the keyboard is
		/// currently visible or not)
		/// </summary>
		public static bool IsAnyInputActive
		{
			get
			{
				return _currentInput != null;
			}
		}

		public static Rect? KeyboardArea
		{
			get
			{
			    if (_touchScreenKeyboard == null)
			        return null;

			    if (Application.platform == RuntimePlatform.Android)
			    {
			        var keyboardHeight = GetAndroidKeyboardSize() * 1.1f; // slightly larger as the size might not take the input area into account
			        return new Rect(0, Screen.height - keyboardHeight, Screen.width, keyboardHeight);
			    }
			    else
			    {
			        var touchScreenKeyboardArea = TouchScreenKeyboard.area;
			        if (touchScreenKeyboardArea.height > 0)
			            return touchScreenKeyboardArea;
			        else
			            return null;
			    }	
			}
		}

		private static int GetAndroidKeyboardSize()
		{
			AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject activity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
			AndroidJavaObject unityPlayer = activity.Get<AndroidJavaObject>("mUnityPlayer");
			AndroidJavaObject view = unityPlayer.Call<AndroidJavaObject>("getView");
			AndroidJavaObject rect = new AndroidJavaObject("android.graphics.Rect");

			view.Call("getWindowVisibleDisplayFrame", rect);
			int height = rect.Call<int>("height");

			// Release the local reference
			unityClass.Dispose();
			activity.Dispose();
			unityPlayer.Dispose();
			view.Dispose();
			rect.Dispose();

			return Screen.height - height;
		}

		/// <summary>
		/// This deactivates the specified TouchScreenKeyboardInput if it is active.
		/// </summary>
		/// <param name="input"></param>
		public static void Deactivate(TouchScreenKeyboardInput input)
		{
			if(input == _currentInput)
				_currentInput = null;

			// The keyboard itself will be closed on the next Update if nothing else grabs focus in the meantime.
		}

		/// <summary>
		/// This deactivates whatever the current input is.
		/// </summary>
		public static void Deactivate()
		{
			_currentInput = null;
		}
	}
}