using System;
using System.Reflection;
using UnityEngine;
using Object = System.Object;

namespace Opencoding.CommandHandlerSystem
{
	public class PropertyCommandHandler : CommandHandler
	{
	    private readonly MethodInfo _getMethodInfo;
	    private readonly MethodInfo _setMethodInfo;

		public override ParamInfo[] Parameters
		{
			get
			{
				object value = null;
				if (_getMethodInfo != null)
				{
					value = _getMethodInfo.Invoke(GetObjectReference(), null);
				}

				return new ParamInfo[]
				{
					new ParamInfo()
					{IsOptional = true, Name = "value", Type = _getMethodInfo.ReturnType, DefaultValue = value}
				};
			}
		}

		public PropertyCommandHandler(string commandName, string description, Type type, Object obj, PropertyInfo propertyInfo, bool strongReference)
			: base(commandName, description, type, obj, propertyInfo, strongReference)
        { 
		    _getMethodInfo = propertyInfo.GetGetMethod(true);
            _setMethodInfo = propertyInfo.GetSetMethod(true);

            if (propertyInfo.DeclaringType != null)
				_methodOrPropertyName = propertyInfo.DeclaringType.FullName + "." + propertyInfo.Name;
			else
				_methodOrPropertyName = propertyInfo.Name;
		}


        public PropertyCommandHandler(string commandName, string description, Type type, Object obj, MethodInfo getMethodInfo, MethodInfo setMethodInfo, bool strongReference)
            : base(commandName, description, type, obj, null, strongReference)
        {
            _getMethodInfo = getMethodInfo;
            _setMethodInfo = setMethodInfo;

            if (getMethodInfo.DeclaringType != null)
                _methodOrPropertyName = getMethodInfo.DeclaringType.FullName + "." + getMethodInfo.Name;
            else
                _methodOrPropertyName = getMethodInfo.Name;
        }

        public override bool Equals(object otherObject)
		{
			PropertyCommandHandler otherCommandHandler = otherObject as PropertyCommandHandler;

			if (otherCommandHandler == null)
				return false;

			if (base.Equals(otherObject) == false)
				return false;

			return (_getMethodInfo == otherCommandHandler._getMethodInfo && _setMethodInfo == otherCommandHandler._setMethodInfo);
		}

		public override int GetHashCode()
		{
			return _setMethodInfo.GetHashCode() ^ _getMethodInfo.GetHashCode() ^ base.GetHashCode();
		}

		public override void Invoke(string[] arguments)
		{
			if (arguments.Length == 0)
			{
				object value = _getMethodInfo.Invoke(GetObjectReference(), null);

				LogHistory.LogHistory.Instance.LogMessage(_commandName + " = " + value);
			}
			else if (arguments.Length == 1)
			{
				object parameterValue;
				try
				{
					parameterValue = Utils.GetArgumentValueFromString(arguments[0], _getMethodInfo.ReturnType);
				}
				catch (Exception)
				{
					LogHistory.LogHistory.Instance.LogMessage("Invalid argument to property command " + _commandName + " - expected " +
                                                   _getMethodInfo.ReturnType);
					return;
				}

				_setMethodInfo.Invoke(GetObjectReference(), new object[] { parameterValue });

				LogHistory.LogHistory.Instance.LogMessage(_commandName + " set to " + parameterValue);
			}
			else
			{
				LogHistory.LogHistory.Instance.LogMessage("Properties take either zero or one arguments");
			}
		}
	}
}