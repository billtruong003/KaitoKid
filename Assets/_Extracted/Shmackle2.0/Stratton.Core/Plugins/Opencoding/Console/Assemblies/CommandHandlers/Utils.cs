using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace Opencoding.CommandHandlerSystem
{
	public class Utils
	{
		private static string[] _colorNames =
		{
			"red", "green", "blue", "white", "black", "yellow", "cyan", "magenta", "gray",
			"clear"
		};

		/// <summary>
		/// Takes a string and tries to convert it into a value of the type specified.
		/// This is used for command parsing.
		/// </summary>
		public static Object GetArgumentValueFromString(string argument, Type type)
		{
			if (type == typeof(string))
				return argument;
			
			else if (type == typeof(int))
				return Int32.Parse(argument);
			else if (type == typeof(uint))
				return UInt32.Parse(argument);
			
			else if (type == typeof(short))
				return short.Parse(argument);
			else if (type == typeof(ushort))
				return ushort.Parse(argument);
			
			else if (type == typeof(ulong))
				return ulong.Parse(argument);
			else if (type == typeof(long))
				return long.Parse(argument);
			
			else if (type == typeof(byte))
				return byte.Parse(argument);
			else if (type == typeof(sbyte))
				return sbyte.Parse(argument);
			
			else if (type == typeof(char))
				return char.Parse(argument);
			
			else if (type == typeof(float))
				return Single.Parse(argument);
			else if (type == typeof(bool))
			{
				if (argument == "1")
					return true;
				else if (argument == "0")
					return false;
				return Boolean.Parse(argument);
			}
			else if (type.IsEnum)
				return Enum.Parse(type, argument, true);
			else if (type == typeof(Vector3))
			{
				var parts = argument.Split(',');
				if (parts.Length == 1)
					return new Vector3(Single.Parse(parts[0]), Single.Parse(parts[0]), Single.Parse(parts[0]));
				else if (parts.Length == 3)
					return new Vector3(Single.Parse(parts[0]), Single.Parse(parts[1]), Single.Parse(parts[2]));
				else
					throw new InvalidOperationException("Expected either 1 or 3 arguments.");
			}
			else if (type == typeof(Vector2))
			{
				var parts = argument.Split(',');
				if (parts.Length == 1)
					return new Vector2(Single.Parse(parts[0]), Single.Parse(parts[0]));
				else if (parts.Length == 2)
					return new Vector2(Single.Parse(parts[0]), Single.Parse(parts[1]));
				else
					throw new InvalidOperationException("Expected either 1 or 2 arguments.");
			}
			else if (type == typeof(Color))
			{
				return GetColorFromArgument(argument);
			}
			else if (type == typeof(Color32))
			{
				var color = GetColorFromArgument(argument);
				return new Color32((byte) (color.r * 255), (byte) (color.g * 255), (byte) (color.b * 255), (byte) (color.a * 255));
			}

			throw new InvalidOperationException("Type " + type + " is not supported as a command handler argument.");
		}

		private static Color GetColorFromArgument(string argument)
		{
			switch (argument.ToLower())
			{
				case "red":
					return Color.red;
				case "green":
					return Color.green;
				case "blue":
					return Color.blue;
				case "white":
					return Color.white;
				case "black":
					return Color.black;
				case "yellow":
					return Color.yellow;
				case "cyan":
					return Color.cyan;
				case "magenta":
					return Color.magenta;
				case "gray":
					return Color.gray;
				case "grey":
					return Color.grey;
				case "clear":
					return Color.clear;
			}

			var parts = argument.Split(',');
			if (parts.Length == 1)
				return new Color(Single.Parse(parts[0]), Single.Parse(parts[0]), Single.Parse(parts[0]));
			else if (parts.Length == 3)
				return new Color(Single.Parse(parts[0]), Single.Parse(parts[1]), Single.Parse(parts[2]));
			else if (parts.Length == 4)
				return new Color(Single.Parse(parts[0]), Single.Parse(parts[1]), Single.Parse(parts[2]), Single.Parse(parts[3]));
			else
				throw new InvalidOperationException("Expected either a color name or 1 or 3 or 4 numerical arguments.");
		}

		public static IEnumerable<string> GetDefaultParameterPossibleOptions(Type type)
		{
			var parameterOptions = new List<string>();

			if (type.IsEnum)
			{
				parameterOptions.AddRange(Enum.GetNames(type));
			}
			else if (type == typeof(Boolean))
			{
				parameterOptions.Add("true");
				parameterOptions.Add("false");
			}
			else if (type == typeof (Color) || type == typeof(Color32))
			{
				return _colorNames;
			}

			return parameterOptions;
		}

		public static string GetFriendlyTypeName(Type type)
		{
			if (type == typeof(Single))
				return "float";
			else if (type == typeof(Double))
				return "double";
			else if (type == typeof(Int32) || type == typeof(Int16) || type == typeof(Int64))
				return "integer";
			else if (type == typeof(Color) || type == typeof(Color32))
				return "color";
			return type.Name;
		}

		public static string WrapInQuotesIfNecessary(string s)
		{
			if (s.Contains(" "))
				return String.Format("\"{0}\"", s);
			return s;
		}
	}
}