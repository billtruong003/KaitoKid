using System;
using System.Collections.Generic;
using Opencoding.CommandHandlerSystem;
using UnityEngine;

namespace Opencoding.Console
{
	/// <summary>
	/// This shows you a list of all the commands that have been registered with the game.
	/// Each command is a button that can be pressed to quickly execute that command.
	/// </summary>
	class HelpOverlay : IDisposable
	{
		public bool IsVisible { get; set; }
		private Vector2 _scrollPosition = Vector2.zero;
		private readonly SortedDictionary<string, CommandHandler> _sortedCommandList = new SortedDictionary<string, CommandHandler>();
		private readonly DebugConsole _debugConsole;

		public HelpOverlay(DebugConsole debugConsole)
		{
			_debugConsole = debugConsole;

			CommandHandlers.CommandHandlerAdded += OnCommandHandlerAdded;
			CommandHandlers.CommandHandlerRemoved += OnCommandHandlerRemoved;

			foreach (var item in CommandHandlers.Handlers)
			{
				_sortedCommandList.Add(item.CommandName, item);
			}
		}

		public void OnGUI(Rect rect)
		{
			GUILayout.BeginArea(rect);

			_scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUIStyles.HelpWindowBackgroundStyle);
			GUILayout.BeginVertical();
			GUILayout.Label("This is a complete list of commands you can use:", GUIStyles.HelpWindowHelpStyle);
			float totalLineWidth = 0;
			float areaWidth = rect.width - 20 - GUIStyles.HelpWindowBackgroundStyle.padding.left - GUIStyles.HelpWindowBackgroundStyle.padding.right;
			GUILayout.BeginHorizontal();
			foreach (var commandHandler in _sortedCommandList.Values)
			{
				string buttonLabel = commandHandler.CommandName + (commandHandler.Parameters.Length != 0 ? "..." : "");
				float labelWidth = GUIStyles.SuggestionButtonBackgroundStyle.CalcSize(new GUIContent(buttonLabel)).x;
				if (totalLineWidth + labelWidth > areaWidth && totalLineWidth > 0)
				{
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					totalLineWidth = 0;
				}
				totalLineWidth += labelWidth + GUIStyles.SuggestionButtonBackgroundStyle.margin.left + GUIStyles.SuggestionButtonBackgroundStyle.margin.right;
				GUILayout.Label(buttonLabel, GUIStyles.SuggestionButtonBackgroundStyle, GUILayout.ExpandWidth(false), GUILayout.Height(GUIStyles.SuggestionButtonHeight));

				if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
				{
					HandleButtonPress(commandHandler);
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();
			GUILayout.EndScrollView();
			
			GUILayout.EndArea();
		}

		private void HandleButtonPress(CommandHandler commandHandler)
		{
			if (commandHandler.Parameters.Length == 0)
			{
				CommandHandlers.HandleCommand(commandHandler.CommandName);
			}
			else
			{
				_debugConsole.SetCommandInputTextAsIfTyped(commandHandler.CommandName +
				                                           (commandHandler.Parameters.Length != 0 ? " " : ""));
			}
			Event.current.Use();
			IsVisible = false;
		}

		private void OnCommandHandlerAdded(CommandHandler commandHandler)
		{
			_sortedCommandList.Add(commandHandler.CommandName, commandHandler);
		}

		private void OnCommandHandlerRemoved(CommandHandler commandHandler)
		{
			_sortedCommandList.Remove(commandHandler.CommandName);
		}

        public void Dispose()
        {
            CommandHandlers.CommandHandlerAdded -= OnCommandHandlerAdded;
            CommandHandlers.CommandHandlerRemoved -= OnCommandHandlerRemoved;
        }
    }
}