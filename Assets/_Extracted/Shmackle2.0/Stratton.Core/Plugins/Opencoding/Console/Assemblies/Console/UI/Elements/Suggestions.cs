using System;
using System.Collections.Generic;
using System.Linq;
using Opencoding.CommandHandlerSystem;
using Opencoding.Shared.Utils;
using UnityEngine;

namespace Opencoding.Console
{
	class Suggestions
	{
		class SuggestionButton
		{
			private string _input;
			private bool _automaticallyExecute;
			private readonly InputField _inputField;
			private GUIContent _guiContent;

			private float _width;

			public SuggestionButton(string label, string input, bool automaticallyExecute, InputField inputField)
			{
				_input = input + (automaticallyExecute ? "" : " ");
				_automaticallyExecute = automaticallyExecute;
				_inputField = inputField;

				_guiContent = new GUIContent(label + (automaticallyExecute ? "" : "..."));
			}

			public float Width
			{
				get
				{
					if (_width != 0)
						return _width;

					if(Event.current.type == EventType.Layout)
					{
						_width =  GUIStyles.SuggestionButtonBackgroundStyle.CalcSize(_guiContent).x;
					}

					return _width;
				}
			}

			public GUIContent Content
			{
				get
				{
					return _guiContent;
				}
			}

			public string Input
			{
				get
				{
					return _input;
				}
			}

			public void Pressed()
			{
				if (_automaticallyExecute)
				{
					CommandHandlers.HandleCommand(Input);
					_inputField.ClearInput();
					_inputField.ConfirmInput();
				}
				else
				{
					_inputField.Input = Input;
					_inputField.ConfirmInput();
				}
				Event.current.Use();
			}
		}

		private List<SuggestionButton> _suggestionButtons = new List<SuggestionButton>(); 
		private InputField _inputField;
		private string _lastInput = "";
		private bool _firstRun = true;

		public Suggestions(InputField inputField)
		{
			_inputField = inputField;
		}

		public void Update(bool forceReLayout = false)
		{
			if (_firstRun || _inputField.LastTypedInput != _lastInput)
			{
				_firstRun = false;
				_lastInput = _inputField.LastTypedInput;
				GenerateSuggestionButtons();
			}
		}

		public void ForceRelayoutNextUpdate()
		{
			_firstRun = true;
		}

		private void GenerateSuggestionButtons()
		{
			_suggestionButtons.Clear();

			if (_lastInput.Contains(" ") && _lastInput.Trim().Length != 0)
			{
				GenerateParameterSuggestionButtons();
			}
			else if(_lastInput == "")
			{
				GenerateRecentCommandButtons();
			}
			else if (_inputField.LastTypedParameters.Length != 0)
			{
				GeneratePossibleCommandButtons();
			}
		}

		private void GeneratePossibleCommandButtons()
		{
			var matchingCommands = CommandHandlers.FindMatchingCommands(_inputField.LastTypedParameters[0]).ToArray();

			foreach(var matchingCommand in matchingCommands)
			{
				bool hasParameters = matchingCommand.Parameters.Length != 0;
				_suggestionButtons.Add(new SuggestionButton(matchingCommand.CommandName, matchingCommand.CommandName, !hasParameters, _inputField));
			}
		}

		private void GenerateParameterSuggestionButtons()
		{
			var lastTypedParameters = _inputField.LastTypedParameters;

			CommandHandler commandHandler = CommandHandlers.GetCommandHandler(lastTypedParameters.First());

			if (commandHandler == null)
				return;

			var commandHandlerParameters = commandHandler.Parameters.ToArray();

			var currentParameterIndex = _inputField.CurrentParameterIndex;

		    bool lastParameterIsParamsArray = commandHandlerParameters.Length != 0 && commandHandlerParameters[commandHandlerParameters.Length - 1].IsParamArray;

		    if (currentParameterIndex >= commandHandlerParameters.Length)
		    {
		        if (lastParameterIsParamsArray)
		        {
		            currentParameterIndex = commandHandlerParameters.Length - 1;
		        }
		        else
		        {
                    return;
                }
		    }

            var parameterValue = currentParameterIndex + 1 < lastTypedParameters.Length ? lastTypedParameters[currentParameterIndex + 1] : "";

			List<string> parameterOptions = commandHandler.Parameters[currentParameterIndex].GetParameterPossibleValues(parameterValue);

			bool isLastParameter = currentParameterIndex == commandHandlerParameters.Length - 1;

			var commandUpToLastParameter = String.Join(" ", lastTypedParameters.SubArray(0, currentParameterIndex + 1));

			_suggestionButtons.AddRange(parameterOptions.Select(x => new SuggestionButton(x, commandUpToLastParameter + " " + CommandHandlerSystem.Utils.WrapInQuotesIfNecessary(x), isLastParameter && !lastParameterIsParamsArray, _inputField)));
		}

		

		private void GenerateRecentCommandButtons()
		{
            if (!DebugConsole.Instance.Settings.ShowRecentCommands)
                return;

            foreach (var recentCommand in DebugConsole.Instance.RecentCommands)
			{
				var commandHandler = CommandHandlers.GetCommandHandler(recentCommand);
				if (commandHandler == null)
					continue;

				bool hasParameters = commandHandler.Parameters.Length != 0;

				_suggestionButtons.Add(new SuggestionButton(commandHandler.CommandName, commandHandler.CommandName, !hasParameters, _inputField));
			}
		}

		public void Draw(Rect rect)
		{
			const float MARGIN = 20;
			float padding = GUIStyles.SuggestionButtonBackgroundStyle.margin.left;
			
			float x = rect.x;
			float y = rect.y;
			
			int rowsRemaining = Mathf.FloorToInt(rect.height / (GUIStyles.SuggestionButtonHeight + padding)) - 1;
			
			if (rowsRemaining < 0)
				return;

			int itemCount = _suggestionButtons.Count;

			foreach (var button in _suggestionButtons)
			{
				if (rowsRemaining == 0)
				{
					string moreItemsLabel = itemCount + " more...";
					var moreContent = UIUtilities.TempGUIContent(moreItemsLabel);
					float moreTextWidth = GUIStyles.SuggestionButtonMoreBackgroundStyle.CalcSize(moreContent).x;

					if (x + button.Width + moreTextWidth > rect.width - MARGIN)
					{
						var moreRect = new Rect(x, y, moreTextWidth, GUIStyles.SuggestionButtonHeight);
						GUI.Label(moreRect, moreContent, GUIStyles.SuggestionButtonMoreBackgroundStyle);

						if (Event.current.type == EventType.MouseDown && moreRect.Contains(Event.current.mousePosition))
						{
							TouchScreenKeyboardManager.Deactivate();
						}

						break;
					}
				}

				if (x + button.Width > rect.width - MARGIN)
				{
					rowsRemaining--;

					y += GUIStyles.SuggestionButtonHeight + padding;
					x = rect.x;
				}

				var itemRect = new Rect(x, y, button.Width, GUIStyles.SuggestionButtonHeight);
				x += button.Width + padding;

				GUI.Label(itemRect, button.Content, GUIStyles.SuggestionButtonBackgroundStyle);

				if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
				{
					button.Pressed();
				}

				itemCount--;
			}
		}
	}
}