using System.Linq;
using Opencoding.Shared.Utils;
using UnityEngine;

namespace Opencoding.Console
{
	/// <summary>
	/// This is the base class for the input field. It is overriden with a different implementation
	/// for mobile and desktop as the two behave quite differently (partly due to bugs in the Unity 
	/// UI implementation on mobile).
	/// </summary>
	abstract class InputField
	{
		private string _lastTypedInput = "";
		protected string[] _lastTypedParameters = new string[] {};

		public abstract string Input
		{
			get;
			set;
		}

		public abstract bool HasFocus
		{
			get;
		}

		public abstract Rect TextFieldRect
		{
			get;
		}

		public Rect HelpButtonRect
		{
			get;
			protected set;
		}

		public string LastTypedInput
		{
			get
			{
				return _lastTypedInput;
			}
		}

		// These are the last typed parameters that the user entered
		// It doesn't include characters that were entered via auto-complete
		public string[] LastTypedParameters
		{
			get
			{
				return _lastTypedParameters;
			}
		}

		public int CurrentParameterIndex
		{
			get
			{
				int currentParameter = LastTypedParameters.Length - 2;

				if (LastTypedInput.EndsWith(" "))
				{
					currentParameter++;
				}
				return currentParameter;
			}
		}

		public abstract void OnGUI(Rect containingRect);
		public abstract void LoseFocus();
		public abstract void Focus();

		// Call this to update the last typed parameters
		public void ConfirmInput()
		{
			_lastTypedParameters = StringUtils.SplitCommandLine(Input).ToArray();
			_lastTypedInput = Input;
		}

		public abstract void ClearInput();
	}
}