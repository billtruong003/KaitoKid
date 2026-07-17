using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Opencoding.LogHistory;

namespace Opencoding.CommandHandlerSystem
{
	public class ParamInfo
	{
		public string Name;
		public bool IsOptional;
		public Type Type;
		public object DefaultValue;
		public MethodInfo AutoCompleteMethod;
	    public string[] AutoCompleteOptions;
	    public bool IsParamArray;

		public List<string> GetParameterPossibleValues(string parameterValue)
		{
			IEnumerable<string> parameterOptions;

			if (AutoCompleteMethod != null)
			{
				var suggestions = AutoCompleteMethod.Invoke(null, null);
				if (!(suggestions is IEnumerable<string>))
				{
					LogHistory.LogHistory.Instance.LogMessage("Invalid return type from auto complete method", LogHistoryLogType.Error);
					parameterOptions = Utils.GetDefaultParameterPossibleOptions(Type);
				}
				else
				{
					parameterOptions = (IEnumerable<string>)suggestions;
				}
			}
            else if (AutoCompleteOptions != null)
            {
                parameterOptions = AutoCompleteOptions;
            }
			else
			{
                if(IsParamArray)
                    parameterOptions = Utils.GetDefaultParameterPossibleOptions(Type.GetElementType());
                else
                    parameterOptions = Utils.GetDefaultParameterPossibleOptions(Type);
			}

			var parameterOptionsList = parameterOptions.ToList();
			var options =
				parameterOptionsList.Where(x => x.StartsWith(parameterValue, StringComparison.CurrentCultureIgnoreCase)).ToList();
			var partialMatches =
				parameterOptionsList.Where(x => x.IndexOf(parameterValue, StringComparison.CurrentCultureIgnoreCase) >= 0);

			options.AddRange(
				parameterOptionsList.Where(x => !options.Contains(x)).Where(
					x => x.IndexOf(parameterValue, StringComparison.CurrentCultureIgnoreCase) >= 0));

            return options.Union(partialMatches).ToList();
		}
	}
}