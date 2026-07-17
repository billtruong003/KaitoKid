using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Opencoding.LogHistory;
using Opencoding.Shared.Utils;
using UnityEngine;
using Object = System.Object;

namespace Opencoding.CommandHandlerSystem
{
	/// <summary>
	/// This class is used to register and unregister methods and properties
	/// as command handlers. The key methods to use are RegisterCommandHandler
	/// and UnregisterCommandHandler.
	/// </summary>
	public static partial class CommandHandlers 
	{
		private static readonly Dictionary<string, CommandHandler> _commandHandlers = new Dictionary<string, CommandHandler>();
		private static readonly HashSet<Type> _registeredCommandHandlers = new HashSet<Type>();

		private static Action<CommandHandler> _commandHandlerAdded = delegate { };
		public static event Action<CommandHandler> CommandHandlerAdded
		{
			add { _commandHandlerAdded += value; }
			remove { _commandHandlerAdded -= value; }
		}
		private static Action<CommandHandler> _commandHandlerRemoved = delegate { };
		public static event Action<CommandHandler> CommandHandlerRemoved
		{
			add { _commandHandlerRemoved += value; }
			remove { _commandHandlerRemoved -= value; }
		}
		private static Action<CommandHandler> _commandExecuted = delegate { };
		public static event Action<CommandHandler> CommandExecuted
		{
			add { _commandExecuted += value; }
			remove { _commandExecuted -= value; }
		}

	    public static Func<string, bool> BeforeCommandParsedHook;
		public static Func<CommandHandler, string[], bool> BeforeCommandExecutedHook;
        public static Func<string, string[], bool> DefaultCommandHandler; // return true to specify that you've handled the command

	    public static string CurrentExecutingCommand; // The command that's currently being invoked

        public static IEnumerable<CommandHandler> Handlers
		{
			get
			{
				return _commandHandlers.Values;
			}
		}

		/// <summary>
		/// Handles a command that the user has typed into the console.
		/// </summary>
		public static void HandleCommand(string commandLine)
		{
		    if (BeforeCommandParsedHook != null && BeforeCommandParsedHook(commandLine))
		        return;

			var parts = StringUtils.SplitCommandLine(commandLine).ToArray();

			if (parts.Length == 0)
				return;

			LogHistory.LogHistory.Instance.LogMessage(commandLine, LogHistoryLogType.ConsoleInput);

			var command = parts[0];
			var commandLower = command.ToLower();

            // The arguments are everything in the parts array after the first element.
            var arguments = parts.SubArray(1, parts.Length - 1);

            CommandHandler commandHandler;
			if (!_commandHandlers.TryGetValue(commandLower, out commandHandler))
			{
			    if (DefaultCommandHandler == null || !DefaultCommandHandler(command, arguments))
			    {
			        Debug.LogWarning(string.Format("The command {0} could not be found.", command));
			    }
			    return;
			}

			if (!commandHandler.IsValid())
				throw new InvalidOperationException(string.Format("The command handler {0} has been destroyed without being unregistered.", commandHandler.MethodOrPropertyName));

			var methodParameters = commandHandler.Parameters;

		    bool hasAParamArrayAsLastParameter = methodParameters.Length != 0 && methodParameters[methodParameters.Length - 1].IsParamArray;

            if (arguments.Length > methodParameters.Length && !hasAParamArrayAsLastParameter)
			{
				Debug.LogWarning(string.Format("The command {0} expects at most {1} arguments", command, methodParameters.Length));
				return;
			}

			if (BeforeCommandExecutedHook == null || BeforeCommandExecutedHook(commandHandler, arguments))
			{
			    CurrentExecutingCommand = commandHandler.CommandName;
                commandHandler.Invoke(arguments);
			    CurrentExecutingCommand = null;
                _commandExecuted(commandHandler);
			}
		}

		public static CommandHandler FindClosestMatchingCommand(string partialCommand, int index)
		{
			var matchingCommands = FindMatchingCommands(partialCommand).ToArray();

			if (matchingCommands.Length == 0)
				return null;

			return matchingCommands[index % matchingCommands.Length];
		}

		public static IEnumerable<CommandHandler> FindMatchingCommands(string partialCommand)
		{
			return _commandHandlers.Values.Where(x => x.CommandName.StartsWith(partialCommand, StringComparison.CurrentCultureIgnoreCase)).OrderByDescending(x => x.CommandName.Length).ToArray();
		}

		public static CommandHandler GetCommandHandler(string commandName)
		{
			var commandParts = StringUtils.SplitCommandLine(commandName).ToArray();

			if (commandParts.Length == 0)
				return null;

			CommandHandler commandHandler;
			_commandHandlers.TryGetValue(commandParts[0].ToLower(), out commandHandler);
			return commandHandler;
		}


		/// <summary>
		/// This registers static command handlers in the type specified.
		/// </summary>
		/// <param name="type"></param>
		public static void RegisterCommandHandlers(Type type)
		{
			RegisterCommandHandlers(type, null);
		}

		/// <summary>
		/// Call this to register the command handles in the object you specify. Command handlers are
		/// methods in a class that have a [CommandHandler] attribute on them.
		/// </summary>
		/// <param name="obj"></param>
		public static void RegisterCommandHandlers(Object obj)
		{
			RegisterCommandHandlers(obj.GetType(), obj);
		}

        public static void RegisterCommandHandler(MethodInfo methodInfo, object obj, string commandName = null, string description = null, bool strongReference = false)
	    {
		    if (!methodInfo.IsStatic && obj == null)
			    throw new InvalidOperationException("Attempting to register non static method as command handler without specifying an instance object.");

		    if (methodInfo.IsStatic && obj != null)
			    throw new InvalidOperationException("Attempting to a static method as command handler while specifying an instance object.");

	        RegisterMethodCommandHandler(methodInfo.DeclaringType, obj, methodInfo, description, commandName, strongReference);
	    }

        public static void RegisterCommandHandler(PropertyInfo propertyInfo, object obj, string commandName = null, string description = null, bool strongReference = false)
        {
	        if (!propertyInfo.GetGetMethod().IsStatic && obj == null)
		        throw new InvalidOperationException("Attempting to register non static property as command handler without specifying an instance object.");

	        if (propertyInfo.GetGetMethod().IsStatic && obj != null)
		        throw new InvalidOperationException("Attempting to a static property as command handler while specifying an instance object.");

            RegisterPropertyCommandHandler(propertyInfo.DeclaringType, obj, propertyInfo, description, commandName, strongReference);
        }

        public static void RegisterCommandHandler(MethodInfo getMethodInfo, MethodInfo setMethodInfo, object obj, string commandName, string description = null, bool strongReference = false)
        {
            RegisterPropertyCommandHandler(getMethodInfo.DeclaringType, obj, getMethodInfo, setMethodInfo, description, commandName, strongReference);
        }

        public static void RegisterCommandHandler(MethodInfo methodInfo, string commandName = null, string description = null, bool strongReference = false)
        {
            if(!methodInfo.IsStatic)
                throw new InvalidOperationException("Attempting to register non static method as command handler without specifying an instance object. Use the variant of RegisterCommandHandler that takes an object instead.");
            RegisterMethodCommandHandler(methodInfo.DeclaringType, null, methodInfo, description, commandName, strongReference);
        }

        public static void RegisterCommandHandler(PropertyInfo propertyInfo, string commandName = null, string description = null, bool strongReference = false)
        {
            if (!propertyInfo.GetGetMethod().IsStatic)
                throw new InvalidOperationException("Attempting to register non static property as command handler without specifying an instance object. Use the variant of RegisterCommandHandler that takes an object instead.");
            RegisterPropertyCommandHandler(propertyInfo.DeclaringType, null, propertyInfo, description, commandName, strongReference);
        }

        public static void RegisterCommandHandler(MethodInfo getMethodInfo, MethodInfo setMethodInfo, string commandName, string description = null, bool strongReference = false)
        {
            if (!getMethodInfo.IsStatic || !setMethodInfo.IsStatic)
                throw new InvalidOperationException("Attempting to register non static property as command handler without specifying an instance object. Use the variant of RegisterCommandHandler that takes an object instead.");
            RegisterPropertyCommandHandler(getMethodInfo.DeclaringType, null, getMethodInfo, setMethodInfo, description, commandName, strongReference);
        }

        private static void RegisterCommandHandler_internal(Delegate del, string commandName = null, string description = null, Type[] types = null, bool strongReference = false)
        {
            var method = del.Method;
            if(types != null)
                RegisterMethodCommandHandler(method.DeclaringType, del.Target, method, description, commandName, types, strongReference);
            else
                RegisterMethodCommandHandler(method.DeclaringType, del.Target, method, description, commandName, strongReference);
        }

        private static void RegisterCommandHandler_internal(Delegate del, string commandName, string description, ParamInfo[] paramInfos, bool strongReference = false)
        {
            var method = del.Method;
            RegisterMethodCommandHandler(method.DeclaringType, del.Target, method, description, commandName, paramInfos, strongReference);
        }

        private static void RegisterCommandHandler_internal(Delegate getDelegate, Delegate setDelegate, string commandName, string description = null, bool strongReference = false)
        {
            var getMethod = getDelegate.Method;
            var setMethod = setDelegate.Method;
            if (getDelegate.Target != null || setDelegate.Target != null)
            {
                throw new InvalidOperationException("Command handlers can't be created from get/set delegates when either of these are instances. If this is an important feature to you, please contact support@opencoding.net.");
            }

            RegisterPropertyCommandHandler(getMethod.DeclaringType, null, getMethod, setMethod, description, commandName, strongReference);
        }


        private static void RegisterCommandHandlers(Type type, Object obj)
		{
			if (_registeredCommandHandlers.Contains(type))
				return;

			_registeredCommandHandlers.Add(type);

			RegisterCommandHandlerMethods(type, obj);
			RegisterCommandHandlerProperties(type, obj);
		}

		private static void RegisterCommandHandlerMethods(Type type, object obj, bool strongReference = false)
		{
			MethodInfo[] methods;
			if (obj == null)
			{
				methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			}
			else
			{
				methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			}

			foreach (MethodInfo method in methods)
			{
				object[] attributes = method.GetCustomAttributes(typeof(CommandHandlerAttribute), true);

				if (attributes.Length == 0)
				{
					continue;
				}

				var attribute = (CommandHandlerAttribute)attributes[0];

				var commandName = attribute.Name;

				RegisterMethodCommandHandler(type, obj, method, attribute.Description, commandName, strongReference);
			}
		}

		private static void RegisterCommandHandlerProperties(Type type, object obj, bool strongReference = false)
		{
			PropertyInfo[] properties;
			if (obj == null)
			{
				properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			}
			else
			{
				properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			}

			foreach (PropertyInfo property in properties)
			{
				object[] attributes = property.GetCustomAttributes(typeof(CommandHandlerAttribute), true);

				if (attributes.Length == 0)
				{
					continue;
				}

				var attribute = (CommandHandlerAttribute)attributes[0];

				var commandName = attribute.Name;
				if (String.IsNullOrEmpty(commandName))
				{
					commandName = property.Name;
				}

				RegisterPropertyCommandHandler(type, obj, property, attribute.Description, commandName, strongReference);
			}
		}

        private static void RegisterMethodCommandHandler(Type type, object obj, MethodInfo method, string description, string commandName, ParamInfo[] paramsParameterInfos, bool strongReference)
        {
            if (String.IsNullOrEmpty(commandName))
                commandName = method.Name;

            if (!ValidateCommandHandlerName(commandName, method))
                return;

            var commandHandler = new MethodCommandHandler(commandName, description, type, obj, method, paramsParameterInfos, strongReference);
            _commandHandlers.Add(commandName.ToLower(), commandHandler);

            _commandHandlerAdded(commandHandler);
        }

        private static void RegisterMethodCommandHandler(Type type, object obj, MethodInfo method, string description, string commandName, Type[] paramsParameterTypes, bool strongReference)
        {
            if (String.IsNullOrEmpty(commandName))
                commandName = method.Name;

            if (!ValidateCommandHandlerName(commandName, method))
                return;

            var commandHandler = new MethodCommandHandler(commandName, description, type, obj, method, paramsParameterTypes, strongReference);
            _commandHandlers.Add(commandName.ToLower(), commandHandler);

            _commandHandlerAdded(commandHandler);
        }

        private static void RegisterMethodCommandHandler(Type type, object obj, MethodInfo method, string description, string commandName, bool strongReference)
		{
            if (String.IsNullOrEmpty(commandName))
                commandName = method.Name;

			if (!ValidateCommandHandlerName(commandName, method))
				return;

			var commandHandler = new MethodCommandHandler(commandName, description, type, obj, method, strongReference);
			_commandHandlers.Add(commandName.ToLower(), commandHandler);

			_commandHandlerAdded(commandHandler);
		}

		private static void RegisterPropertyCommandHandler(Type type, object obj, PropertyInfo property, string description, string commandName, bool strongReference)
		{
			if (!ValidateCommandHandlerName(commandName, property)) 
				return;

			var commandHandler = new PropertyCommandHandler(commandName, description, type, obj, property, strongReference);
			_commandHandlers.Add(commandName.ToLower(), commandHandler);

			_commandHandlerAdded(commandHandler);
		}

        private static void RegisterPropertyCommandHandler(Type type, object obj, MethodInfo getMethodInfo, MethodInfo setMethodInfo, string description, string commandName, bool strongReference)
        {
            if (!ValidateCommandHandlerName(commandName, getMethodInfo))
                return;

            var commandHandler = new PropertyCommandHandler(commandName, description, type, obj, getMethodInfo, setMethodInfo, strongReference);
            _commandHandlers.Add(commandName.ToLower(), commandHandler);

            _commandHandlerAdded(commandHandler);
        }

        private static bool ValidateCommandHandlerName(string commandName, MemberInfo memberInfo)
		{
			string lowerCaseCommandName = commandName.ToLower();

			if (lowerCaseCommandName.Contains(" "))
			{
				Debug.LogWarning(string.Format("Invalid command name {0}. Command names cannot contain spaces. Ignoring.", lowerCaseCommandName));
				return false;
			}

			if (_commandHandlers.ContainsKey(lowerCaseCommandName))
			{
				Debug.LogWarning(string.Format("A command handler has already been registered for the command {0}. Ignoring {1}.", commandName, memberInfo.DeclaringType.FullName + "." + memberInfo.Name));
				return false;
			}
			return true;
		}

		/// <summary>
		/// Call this to unregister the command handlers in the object. This should be called
		/// when the object is destroyed.
		/// </summary>
		/// <param name="obj">The instance that the command handler was registered with.</param>
		public static void UnregisterCommandHandlers(Object obj)
		{
			if (!_registeredCommandHandlers.Contains(obj.GetType()))
				return;

			var itemsToRemove =
				_commandHandlers.Where(i => i.Value.ObjectReference != null && i.Value.ObjectReference.Target == obj).Select(
					i => i.Key).ToList();

			foreach (var item in itemsToRemove)
			{
				_commandHandlerRemoved(_commandHandlers[item]);

				_commandHandlers.Remove(item);
			}

			_registeredCommandHandlers.Remove(obj.GetType());
		}

		/// <summary>
		/// Call this to unregister static command handlers in the type specified.
		/// </summary>
		/// <param name="type">The type that the command handler was registered in (if registered as a static command handler)</param>
		public static void UnregisterCommandHandlers(Type type)
		{
			if (!_registeredCommandHandlers.Contains(type))
				return;

			var itemsToRemove = _commandHandlers.Where(i => i.Value.Type == type).Select(i => i.Key).ToList();
			foreach (var item in itemsToRemove)
			{
				_commandHandlerRemoved(_commandHandlers[item]);

				_commandHandlers.Remove(item);
			}

			_registeredCommandHandlers.Remove(type);
		}

        /// <summary>
        /// Remove a specific named command handler.
        /// </summary>
        /// <param name="commandName">The command name, case insensitive.</param>
        /// <returns></returns>
        public static bool UnregisterCommandHandler(string commandName)
        {
            commandName = commandName.ToLower();

            CommandHandler commandHandler;
            _commandHandlers.TryGetValue(commandName, out commandHandler);
            if (commandHandler != null && _commandHandlers.Remove(commandName))
            {
                _commandHandlerRemoved(commandHandler);
                return true;
            }

            return false;
        }
    }
}