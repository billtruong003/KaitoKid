using System;
using System.Collections.Generic;
using System.Linq;
using Opencoding.CommandHandlerSystem;
using UnityEngine;

namespace Opencoding.Console
{
	/// <summary>
	/// This class deals with rendering the information about the currently selected command,
	/// just below the console.
	/// It shows a description for the command and the list of parameters that the command takes.
	/// </summary>
	class CommandInformation
	{
		private readonly InputField _inputField;
		private readonly List<ParameterEntry> _parameterEntries = new List<ParameterEntry>();
		private CommandHandler _lastCommand;
		private int _lastCurrentParameterIndex;
		private DebugConsole _debugConsole;

		public CommandInformation(DebugConsole debugConsole, InputField inputField)
		{
			_debugConsole = debugConsole;
			_inputField = inputField;
		}

		public Vector2 OnGUI()
		{
			Vector2 commandTooltipBottomLeft = DrawDescription(new Vector2(_inputField.TextFieldRect.xMin, _debugConsole.ConsoleTop + _inputField.TextFieldRect.yMax + 10));

			var parameterListBottomLeft = DrawParameterList(commandTooltipBottomLeft);

			return new Vector2(_inputField.TextFieldRect.xMin, parameterListBottomLeft.y);
		}

		private Vector2 DrawDescription(Vector2 topLeft)
		{
			if (String.IsNullOrEmpty(_inputField.Input.Trim()))
				return topLeft;

			CommandHandler commandHandler = CommandHandlers.GetCommandHandler(_inputField.Input);

			if (commandHandler == null || String.IsNullOrEmpty(commandHandler.Description))
				return topLeft;

			var commandDescriptionContent = new GUIContent(commandHandler.Description);
			var descriptionHeight = GUIStyles.CommandDescriptionBackgroundStyle.CalcHeight(commandDescriptionContent, _debugConsole.ActualConsoleWidth - topLeft.x * 2);
			Rect commandDescriptionRect = new Rect(topLeft.x, topLeft.y, _debugConsole.ActualConsoleWidth - topLeft.x * 2, descriptionHeight + 5);

			GUI.Label(commandDescriptionRect, commandDescriptionContent, GUIStyles.CommandDescriptionBackgroundStyle);

			return new Vector2(topLeft.x, commandDescriptionRect.yMax + 5);
		}

		/// <summary>
		/// This draws the list of parameters at the position specified. The active parameter is highlighted.
		/// </summary>
		/// <param name="topLeft"></param>
		/// <returns></returns>
		private Vector2 DrawParameterList(Vector2 topLeft)
		{
			if (_inputField.Input == "")
				return topLeft;

			CommandHandler exactlyMatchingCommand = CommandHandlers.GetCommandHandler(_inputField.Input);

			if (exactlyMatchingCommand == null || exactlyMatchingCommand.Parameters.Length == 0)
				return topLeft;

			if (exactlyMatchingCommand is MethodCommandHandler)
			{
				var bottom = DrawMethodCommandParameterList(topLeft, exactlyMatchingCommand);

				return new Vector2(topLeft.x, bottom + 16 * GUIStyles.ScaleFactor);
			}
			else if(exactlyMatchingCommand is PropertyCommandHandler)
			{
				var bottom = DrawPropertyCommandParameterList(topLeft, exactlyMatchingCommand);

				return new Vector2(topLeft.x, bottom + 16 * GUIStyles.ScaleFactor);
			}
			else
			{
				throw new InvalidOperationException("Unrecoginised command type");
			}
		}

		private float DrawPropertyCommandParameterList(Vector2 topLeft, CommandHandler exactlyMatchingCommand)
		{
			float height = GUIStyles.CommandInformationParameterNormalStyle.CalcSize(new GUIContent("]")).y * 1.2f;

			string message = string.Format("<b>{0}</b>  Current value: {1}", CommandHandlerSystem.Utils.GetFriendlyTypeName(exactlyMatchingCommand.Parameters[0].Type), exactlyMatchingCommand.Parameters[0].DefaultValue);
			Vector2 itemSize = GUIStyles.CommandInformationParameterNormalStyle.CalcSize(new GUIContent(message));
			GUI.Label(new Rect(topLeft.x, topLeft.y, itemSize.x, height), message, GUIStyles.CommandInformationParameterNormalStyle);
			return topLeft.y + height;
		}

		private float DrawMethodCommandParameterList(Vector2 topLeft, CommandHandler exactlyMatchingCommand)
		{
			RegenerateMethodCommandParameterList(exactlyMatchingCommand);

			float height = GUIStyles.CommandInformationParameterNormalStyle.CalcSize(new GUIContent("]")).y*1.2f;
			float widthOfASpace = GUIStyles.CommandInformationParameterNormalStyle.CalcSize(new GUIContent("_")).x;

			//modify max width to make it smaller if the keyboard is open, to leave space for the keyboard close button
			float maxWidth = _debugConsole.ActualConsoleWidth - 80*GUIStyles.ScaleFactor;

			var keyboardArea = TouchScreenKeyboardManager.KeyboardArea;
			if (keyboardArea != null && ((Rect)keyboardArea).height > 0)
				maxWidth -= TouchScreenKeyboardManager.GetTouchScreenKeyboardCloseButtonRect().width;

			// Now render the GUIContents that we built up above
			float widthSoFar = 0;
			int entryNumber = 0;
			float top = topLeft.y;
			foreach (var entry in _parameterEntries)
			{
				// Check to see if this parameter will fit on the screen. If not, we go on to a new line.
				if (topLeft.x + widthSoFar + entry._Width + widthOfASpace > maxWidth && top != topLeft.y)
				{
					widthSoFar = 0;
					top += height;
				}
				else if (entryNumber != 0)
				{
					// If we're not on the first parameter, we add a space in before the parameter.
					var itemRect = new Rect(topLeft.x + widthSoFar, top, widthOfASpace, height);
					GUI.Label(itemRect, " ", GUIStyles.CommandInformationParameterNormalStyle);
					widthSoFar += widthOfASpace;
				}

				foreach (var item in entry._Items)
				{
					float itemWidth = item.Value.CalcSize(item.Key).x;

					GUI.Label(new Rect(topLeft.x + widthSoFar, top, itemWidth, height), item.Key, item.Value);

					widthSoFar += itemWidth;
				}

				entryNumber++;
			}
			return top + height;
		}

		/// <summary>
		/// This method generates the objects that will be used to render the parameter list in advance.
		/// This is then cached so that it doesn't need to be done repeatedly.
		/// </summary>
		/// <param name="commandHandler"></param>
		private void RegenerateMethodCommandParameterList(CommandHandler commandHandler)
		{
			if (_lastCommand == commandHandler && _lastCurrentParameterIndex == _inputField.CurrentParameterIndex)
			{
				return;
			}

			_lastCommand = commandHandler;
			_lastCurrentParameterIndex = _inputField.CurrentParameterIndex;

			_parameterEntries.Clear();

			ParamInfo[] commandHandlerParameters = commandHandler.Parameters.ToArray();

			// We iterate over all the parameters, building up a list of GUIContents and styles for each
			bool hasOptionalParameters = false;
			int i = 0;
			foreach (ParamInfo parameter in commandHandlerParameters)
			{
				var parameterEntry = new ParameterEntry();
				_parameterEntries.Add(parameterEntry);

				string defaultValue = "";
				if (parameter.IsOptional && !hasOptionalParameters)
				{
					hasOptionalParameters = true;
					parameterEntry.Add("[", GUIStyles.CommandInformationParameterNormalStyle);
				}

				if (parameter.IsOptional)
				{
					defaultValue = parameter.DefaultValue == null && parameter.Type == typeof(string) ? " = \"\"" : " = " + parameter.DefaultValue;
				}
                
				bool isCurrentParameter = i == _inputField.CurrentParameterIndex || _inputField.CurrentParameterIndex >= commandHandlerParameters.Length && parameter.IsParamArray;

				GUIStyle parameterStyle = isCurrentParameter ? GUIStyles.CommandInformationParameterHighlightedStyle : GUIStyles.CommandInformationParameterNormalStyle;
				string parameterText = string.Format("{0} <b>{1}</b>{2}", CommandHandlerSystem.Utils.GetFriendlyTypeName(parameter.Type), parameter.Name, defaultValue);
                parameterEntry.Add(parameterText, parameterStyle);

				i++;
			}

            if (hasOptionalParameters)
			{
				_parameterEntries.Last().Add("]", GUIStyles.CommandInformationParameterNormalStyle);
			}
		}

		class ParameterEntry
		{
			public List<KeyValuePair<GUIContent, GUIStyle>> _Items = new List<KeyValuePair<GUIContent, GUIStyle>>();
			public float _Width;

			public void Add(string text, GUIStyle style)
			{
				var guiContent = new GUIContent(text);
				_Items.Add(new KeyValuePair<GUIContent, GUIStyle>(guiContent, style));
				_Width += style.CalcSize(guiContent).x;
			}
		}
	}
}