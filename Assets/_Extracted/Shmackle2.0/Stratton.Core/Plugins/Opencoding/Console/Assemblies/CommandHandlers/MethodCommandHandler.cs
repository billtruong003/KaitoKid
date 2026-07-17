using System;
using System.Linq;
using System.Reflection;
using Opencoding.LogHistory;
using Opencoding.Shared.Utils;
using Object = System.Object;

namespace Opencoding.CommandHandlerSystem
{
	public class MethodCommandHandler : CommandHandler
	{
		private readonly MethodInfo _methodInfo;
		private readonly ParamInfo[] _apparentParameters;
        private readonly ParamInfo[] _realParameters;
        private readonly ParamInfo[] _paramParameters; // parameters for methods that end with params object[]
	    
		public override ParamInfo[] Parameters
		{
			get
			{
				return _apparentParameters;
			}
		}

		public MethodCommandHandler(string commandName, string description, Type type, Object obj, MethodInfo methodInfo, bool strongReference)
			: base(commandName, description, type, obj, methodInfo, strongReference)
		{
            _realParameters = GenerateParameterList(methodInfo);
		    _apparentParameters = _realParameters;

            _methodInfo = methodInfo;

			if (methodInfo.DeclaringType != null)
				_methodOrPropertyName = string.Format("{0}.{1}", methodInfo.DeclaringType.FullName, methodInfo.Name);
			else
				_methodOrPropertyName = methodInfo.Name;
		}

	    public MethodCommandHandler(string commandName, string description, Type type, Object obj, MethodInfo methodInfo, Type[] paramsParameterTypes, bool strongReference)
            : base(commandName, description, type, obj, methodInfo, strongReference)
	    {
            _realParameters = GenerateParameterList(methodInfo);

	        var lastParameter = _realParameters[_realParameters.Length - 1];
            if (!lastParameter.IsParamArray || lastParameter.Type != typeof(object[]))
                throw new InvalidOperationException("Cannot specify paramsParameterTypes for a method command handler unless the last parameter is a 'params object[]'");

            _paramParameters = paramsParameterTypes.Select(x => new ParamInfo()
            {
                IsOptional = false,
                Name = x.Name,
                Type = x,
                DefaultValue = null,
                AutoCompleteMethod = null,
                IsParamArray = false
            }).ToArray();

	        _apparentParameters = _realParameters.SubArray(0, _realParameters.Length - 1).Concat(_paramParameters).ToArray();

            _methodInfo = methodInfo;

            if (methodInfo.DeclaringType != null)
                _methodOrPropertyName = string.Format("{0}.{1}", methodInfo.DeclaringType.FullName, methodInfo.Name);
            else
                _methodOrPropertyName = methodInfo.Name;
        }

	    public MethodCommandHandler(string commandName, string description, Type type, Object obj, MethodInfo methodInfo, ParamInfo[] paramsParameterInfos, bool strongReference)
            : base(commandName, description, type, obj, methodInfo, strongReference)
        {
            _realParameters = GenerateParameterList(methodInfo);
            var lastParameter = _realParameters[_realParameters.Length - 1];
            if (!lastParameter.IsParamArray || lastParameter.Type != typeof(object[]))
                throw new InvalidOperationException("Cannot specify paramsParameterInfos for a method command handler unless the last parameter is a 'params object[]'");

	        bool exepectSubsequentToBeOptional = false;
	        foreach (var parameter in paramsParameterInfos)
	        {
                if (exepectSubsequentToBeOptional && !parameter.IsOptional)
                   throw new InvalidOperationException(string.Format("There was one optional parameter specified, but it was followed by the non-optional parameter {0}, which isn't valid.", parameter.Name));

	            if (parameter.IsOptional)
	            {
	                exepectSubsequentToBeOptional = true;
                    if(parameter.Type.IsPrimitive && parameter.DefaultValue == null)
                        throw new InvalidOperationException(string.Format("Parameter {0} is an optional primitive type, but has a default value of null, which isn't valid.", parameter.Name));
	            }
	        }

            _paramParameters = paramsParameterInfos;
            _apparentParameters = _realParameters.SubArray(0, _realParameters.Length - 1).Concat(_paramParameters).ToArray();

            _methodInfo = methodInfo;

            if (methodInfo.DeclaringType != null)
                _methodOrPropertyName = string.Format("{0}.{1}", methodInfo.DeclaringType.FullName, methodInfo.Name);
            else
                _methodOrPropertyName = methodInfo.Name;
        }

	    private static ParamInfo[] GenerateParameterList(MethodInfo methodInfo)
	    {
	        var parameters = methodInfo.GetParameters();
		    var paramInfos = new ParamInfo[parameters.Length];

		    for (int i = 0; i < parameters.Length; ++i)
		    {
			    var parameterInfo = parameters[i];
			    paramInfos[i] = new ParamInfo()
			    {
				    IsOptional = parameterInfo.IsOptional,
				    Name = parameterInfo.Name,
				    Type = parameterInfo.ParameterType,
				    DefaultValue = parameterInfo.DefaultValue,
				    AutoCompleteMethod = GetAutoCompleteMethod(parameterInfo),
				    IsParamArray = parameterInfo.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0
			    };
		    }

		    return paramInfos;
	    }

	    public override bool Equals(object otherObject)
		{
			MethodCommandHandler otherCommandHandler = otherObject as MethodCommandHandler;

			if (otherCommandHandler == null)
				return false;

			if (base.Equals(otherObject) == false)
				return false;

			return (_methodInfo == otherCommandHandler._methodInfo);
		}

		public override int GetHashCode()
		{
			return _methodInfo.GetHashCode() ^ base.GetHashCode();
		}

		public override void Invoke(string[] arguments)
		{
			bool invalidArguments = false;
			Object[] argumentValues;


		    invalidArguments = GetArgumentList(_realParameters, _paramParameters, arguments, out argumentValues);

		    if (invalidArguments)
		    {
		        LogHistory.LogHistory.Instance.LogMessage(
		            "The command " + _commandName + " expects the arguments " + GetMethodParametersAsString(_realParameters, _paramParameters),
		            LogHistoryLogType.Warning);
		        return;
		    }
		    
			_methodInfo.Invoke(GetObjectReference(), argumentValues);
		}

	}
}