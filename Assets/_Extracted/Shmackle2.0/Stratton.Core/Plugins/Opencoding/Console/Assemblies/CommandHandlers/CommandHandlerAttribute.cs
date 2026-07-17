using System;

namespace Opencoding.CommandHandlerSystem
{
	[System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Property)]
	public class ConsoleCommandAttribute : CommandHandlerAttribute
	{
		
	}
	
	/// <summary>
	/// This attribute should be placed on methods that you want to become Command Handlers.
	/// You should then call DebugConsole.RegisterCommandHandlers passing either the type
	/// or object that you want to add the command handlers for.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Property)]
	public class CommandHandlerAttribute : Attribute
	{
		public string Name
		{
			get;
			set;
		}

		public string Description
		{
			get;
			set;
		}
	}
}