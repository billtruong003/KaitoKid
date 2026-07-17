using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Opencoding.CommandHandlerSystem
{
	/// <summary>
	/// Use this attribute to specify what a parameter's auto-complete options are.
	/// You must specify a type and (optionally) a method name for a method that
	/// returns an IEnumerable<string> containing all the possible options. 
	/// These will be filtered based on the user's input by the console itself.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Parameter)]
	public class AutocompleteAttribute : Attribute
	{
		public MethodInfo MethodInfo
		{
			get;
			private set;
		}

		public AutocompleteAttribute(Type type, string methodName = "Autocomplete")
		{
			MethodInfo = type.GetMethod(methodName,
				BindingFlags.Static | BindingFlags.NonPublic |
				BindingFlags.Public, null, new Type[] { }, null);
			if (MethodInfo == null || MethodInfo.ReturnType != typeof(IEnumerable<string>))
			{
				MethodInfo = null;
				Debug.LogWarning(
					string.Format("Parameter has AutoComplete attribute but the type specified doesn't have a method called '{0}' that takes no arguments and returns IEnumerable<string> - ignoring", methodName));
			}
		}
	}
}