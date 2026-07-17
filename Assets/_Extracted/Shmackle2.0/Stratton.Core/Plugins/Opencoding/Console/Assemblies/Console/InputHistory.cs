using System.Collections.Generic;

namespace Opencoding.Console
{
	/// <summary>
	/// Stores the history of all the commands that have been typed into the input field.
	/// These are not necessarily valid commands.
	/// </summary>
	public class InputHistory
	{
		private readonly List<string> _history = new List<string>();
		private int _currentIndex = 0;

		public void RecordInput(string input)
		{
			if (input.Trim().Length == 0)
				return;

			_history.Add(input);
			_currentIndex = _history.Count;
		}

		public bool HasPreviousInput
		{
			get
			{
				return _currentIndex != 0;
			}
		}

		public bool HasNextInput
		{
			get
			{
				return _currentIndex < _history.Count - 1;
			}
		}

		public string GetPreviousInput()
		{
			if (!HasPreviousInput)
				return null;

			_currentIndex--;
			var previousInput = _history[_currentIndex];
			return previousInput;
		}

		public string GetNextInput()
		{
			if (!HasNextInput)
				return null;

			_currentIndex++;
			var nextInput = _history[_currentIndex];
			return nextInput;
		}
	}
}