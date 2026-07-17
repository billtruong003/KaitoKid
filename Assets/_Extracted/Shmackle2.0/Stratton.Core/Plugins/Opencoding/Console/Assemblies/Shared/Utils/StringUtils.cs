using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Opencoding.Shared.Utils
{
	public static class StringUtils
	{
		// Borowed gently from Stackoverflow: http://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990
		public static IEnumerable<string> SplitCommandLine(string commandLine)
		{
			bool inQuotes = false;

			return commandLine.Split(c =>
			{
				if (c == '\"')
					inQuotes = !inQuotes;

				return !inQuotes && c == ' ';
			})
				.Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
				.Where(arg => !string.IsNullOrEmpty(arg));
		}

		public static IEnumerable<string> Split(this string str, Func<char, bool> controller)
		{
			int nextPiece = 0;

			for (int c = 0; c < str.Length; c++)
			{
				if (controller(str[c]))
				{
					yield return str.Substring(nextPiece, c - nextPiece);
					nextPiece = c + 1;
				}
			}

			yield return str.Substring(nextPiece);
		}

		public static string TrimMatchingQuotes(this string input, char quote)
		{
			if ((input.Length >= 2) &&
			    (input[0] == quote) && (input[input.Length - 1] == quote))
				return input.Substring(1, input.Length - 2);

			return input;
		}

		public static string Indent(this string input, int tabCount)
		{
			var indentText = new string('\t', tabCount);
			return indentText + input.Replace("\n", "\n" + indentText);
		}

		public static Color GetColorForString(this string input)
		{
			var hashCode = BitConverter.GetBytes(input.GetHashCode());
			for (int i = 0; i < hashCode.Length; ++i)
				if (hashCode[i] < 100)
					hashCode[i] = 100;
		
			return new Color32(hashCode[0], hashCode[1], hashCode[2], 255);
		}

		public static string BetterToString(this Vector3 input)
		{
			return string.Format("({0}, {1}, {2})", input.x, input.y, input.z);
		}

		public static string BetterToString(this Vector2 input)
		{
			return string.Format("({0}, {1})", input.x, input.y);
		}
	}
}