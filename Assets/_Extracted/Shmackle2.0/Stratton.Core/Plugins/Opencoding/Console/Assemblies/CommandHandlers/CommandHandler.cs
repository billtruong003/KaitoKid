using System;
using System.Reflection;
using System.Text;
using Object = System.Object;

namespace Opencoding.CommandHandlerSystem
{ 
	/// <summary>
	/// Each command handler registered has one of these to represent it. It basically
	/// wraps up a method/object pair and some help information for the command.
	/// </summary>
	public abstract class CommandHandler
	{
		private string _description;
		private WeakReference _objectReference;
	    private object _strongObjectReference;
		protected Type _type;
		protected string _methodOrPropertyName; // for debug
		protected string _commandName;
		private bool _isStatic;

		public string Description
		{
			get
			{
				return _description;
			}
		}

		public string CommandName
		{
			get
			{
				return _commandName;
			}
		}

		public string MethodOrPropertyName
		{
			get
			{
				return _methodOrPropertyName;
			}
		}

		public WeakReference ObjectReference
		{
			get
			{
				return _objectReference;
			}
		}

		public Type Type
		{
			get
			{
				return _type;
			}
		}

		protected Object GetObjectReference()
		{
			if (_objectReference == null)
				return null;

			if (_objectReference != null && !_objectReference.IsAlive)
			{
				CommandHandlers.UnregisterCommandHandler(CommandName);
				throw new InvalidOperationException("Command handler " + CommandName + " referenced an object that has been garbage collected. Make sure you hold on to a reference to the object somewhere, or unregister the command handler if you no longer want it registered. The command handler has been automatically removed.");	
			}

			return _objectReference.Target;
		}

	    public MemberInfo MemberInfo { get; private set; }

		protected CommandHandler(string commandName, string description, Type type, object obj, MemberInfo memberInfo, bool strongReference)
		{
			_commandName = commandName;
			_description = description;
			_type = type;
            MemberInfo = memberInfo;
		    if (obj != null)
		    {
		        _objectReference = new WeakReference(obj);
		        if (strongReference)
		            _strongObjectReference = obj;
		    }
		    else
				_isStatic = true;
		}

		public override bool Equals(object otherObject)
		{
			CommandHandler otherCommandHandler = otherObject as CommandHandler;

			if (otherCommandHandler == null)
				return false;

			if (_objectReference == null && otherCommandHandler._objectReference == null)
				return true;

			if (_objectReference == null && otherCommandHandler._objectReference != null || _objectReference != null && otherCommandHandler._objectReference == null)
				return false;

			if (_objectReference.Target != otherCommandHandler._objectReference.Target)
				return false;

			return true;
		}

		public override int GetHashCode()
		{
			object objectTarget = _objectReference.Target;

			if (objectTarget == null)
			{
				throw new InvalidOperationException(string.Format("Command handler target of type {0} was found with a null target object. This is caused when an object is destroyed without unregistering its CommandHandlers", _methodOrPropertyName));
			}
			return objectTarget.GetHashCode();
		}

		public abstract ParamInfo[] Parameters
		{
			get;
		}

		public abstract void Invoke(params string[] arguments);

		protected static MethodInfo GetAutoCompleteMethod(ParameterInfo paramInfo)
		{
			var attributes = paramInfo.GetCustomAttributes(typeof (AutocompleteAttribute), true);
			if (attributes.Length == 0)
				return null;

			var autoCompleteAttribute = (AutocompleteAttribute) attributes[0];
			return autoCompleteAttribute == null ? null : autoCompleteAttribute.MethodInfo;
		}

		public bool IsValid()
		{
			if (_isStatic)
				return true;
			return _objectReference != null && _objectReference.IsAlive;
		}

		/// <summary>
		/// Takes a list of strings and tries to turn them into a list of method parameters.
		/// If the list doesn't have values of the right type (or quantity), returns false.
		/// </summary>
		protected bool GetArgumentList(ParamInfo[] paramInfos, ParamInfo[] paramsParamInfos, string[] commandArguments, out object[] argumentValues)
		{
			var methodParameters = paramInfos;

			argumentValues = new Object[methodParameters.Length];
			bool invalidArguments = false;
			for (int i = 0; i < methodParameters.Length; ++i)
			{
				var parameter = methodParameters[i];

				if (commandArguments.Length <= i)
				{
					if (parameter.IsOptional)
					{
						argumentValues[i] = parameter.DefaultValue;
						continue;
					}
                    else if (parameter.IsParamArray)
                    {
                        if (paramsParamInfos == null)
                            argumentValues[i] = Array.CreateInstance(parameter.Type.GetElementType(), 0);
                        else
                        {
                            var array = Array.CreateInstance(parameter.Type.GetElementType(), paramsParamInfos.Length);;
                            argumentValues[i] = array;
                            invalidArguments = !FillArrayWithDefaults(paramsParamInfos, 0, array);
                        }
                        break;
                    }
					else
					{
						invalidArguments = true;
						break;
					}
				}

			    if (parameter.IsParamArray)
			    {                   
			        var arrayElementType = parameter.Type.GetElementType();
                    if(paramsParamInfos != null && arrayElementType != typeof(object))
                        throw new InvalidOperationException("Somehow you've got a params parameter for a command handler with custom ParamInfos but the parameter isn't of type object[], failing.");

			        Array array;
			        
                    if (paramsParamInfos == null)
                        array = Array.CreateInstance(arrayElementType, commandArguments.Length - i);
                    else
                        array = Array.CreateInstance(arrayElementType, paramsParamInfos.Length);
                    argumentValues[i] = array;
                     
                    try
                    {
                        int j = i;
			            for (; j < commandArguments.Length; ++j)
			            {
			                if (paramsParamInfos != null)
			                    arrayElementType = paramsParamInfos[j - i].Type;

                            var argument = commandArguments[j];
                            array.SetValue(Utils.GetArgumentValueFromString(argument, arrayElementType), j-i);
			            }
                        invalidArguments = !FillArrayWithDefaults(paramsParamInfos, j-i, array);
                        if (invalidArguments)
                            break;
                    }
			        catch (Exception)
			        {                   
			            invalidArguments = true;
			            break;
			        }
			    }
			    else
			    {
			        var argument = commandArguments[i];

			        try
			        {
			            argumentValues[i] = Utils.GetArgumentValueFromString(argument, parameter.Type);
			        }
			        catch (Exception)
			        {
			            invalidArguments = true;
			            break;
			        }
			    }
			}
			return invalidArguments;
		}

	    private static bool FillArrayWithDefaults(ParamInfo[] paramsParamInfos, int startingIndex, Array array)
	    {
	        if (paramsParamInfos == null)
	            return true;

	        for (int i = startingIndex; i < paramsParamInfos.Length; ++i)
	        {
	            if (paramsParamInfos[i].IsOptional)
	                array.SetValue(paramsParamInfos[i].DefaultValue, i);
	            else
	            {
                    return false;
	            }
	        }
	        return true;
	    }

	    protected string GetMethodParametersAsString(ParamInfo[] paramInfos, ParamInfo[] paramsParamInfos, bool styled = false)
		{
			bool hasOptionalParameters = false;
			var stringBuilder = new StringBuilder();
			foreach (var parameter in paramInfos)
			{
				if (stringBuilder.Length != 0)
					stringBuilder.Append("  ");

			    if (parameter.IsParamArray && paramsParamInfos != null)
			    {
			        stringBuilder.Append(GetMethodParametersAsString(paramsParamInfos, null, styled));
			        break;
			    }

			    string defaultValue = "";
				if (parameter.IsOptional && !hasOptionalParameters)
				{
					hasOptionalParameters = true;
					stringBuilder.Append("[");
				}
				if (parameter.IsOptional)
				{
					defaultValue = parameter.Type == typeof (string) ? " = \"\"" : " = " + parameter.DefaultValue;
				}
				stringBuilder.AppendFormat(styled ? "{0} <b>{1}</b>{2}" : "{0} {1}{2}", Utils.GetFriendlyTypeName(parameter.Type), parameter.Name, defaultValue);
			}

			if (hasOptionalParameters)
				stringBuilder.Append("]");
			return stringBuilder.ToString();
		}
	}
}