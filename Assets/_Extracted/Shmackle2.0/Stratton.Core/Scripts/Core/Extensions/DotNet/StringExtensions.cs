using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Stratton.Core
{
    public static class StringExtensions
    {
        #region Fields

        private static readonly string[] _iso8601Formats = new string[]
                                                             {
                                                                 @"yyyy-MM-dd\THH:mm:ss.FFFFFFF\Z",
                                                                 @"yyyy-MM-dd\THH:mm:ss\Z",
                                                                 @"yyyy-MM-dd\THH:mm:ssK"
                                                             };

        #endregion

        #region Public Methods

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static DateTime TryParseExcelDateStringToDateTime(this string value, bool ifNullUseInfinityDate = true)
        {
            if (value.IsNullOrEmpty() || value.ToLower() == "null")
            {
                if (ifNullUseInfinityDate)
                {
                    return new DateTime(9999, 1, 1);
                }
                return DateTime.UtcNow;
            }
            string[] dateSplit = value.Split('.');
            int day = dateSplit[0].TryParseToInt();
            int month = dateSplit[1].TryParseToInt();
            int year = dateSplit[2].TryParseToInt();
            return new DateTime(year, month, day);
        }

        public static string TryParseExcelDateStringToISODateString(this string value, bool ifNullUseInfinityDate = true)
        {
            return value.TryParseExcelDateStringToDateTime(ifNullUseInfinityDate).ToISOString();
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsNotNullOrEmpty(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        public static string Join(this string[] str, string separator)
        {
            return string.Join(separator, str);
        }
        public static string ArrayToString(this string[] arrayStr, string separator)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arrayStr.Length; i++)
            {
                sb.Append(arrayStr[i] + separator);
            }
            return sb.ToString();
        }

        public static float TakeFloatAtIdInSeparated(this string str, char separator, int id, float defaultValue = 0f)
        {
            if (str.IsNullOrEmpty())
            {
                return defaultValue;
            }
            if (id > -1)
            {
                var split = str.Split(';');
                if (split.Length <= id)
                {
                    Debug.LogError("Missing split level " + id + " in " + str);
                    return defaultValue;
                }
                str = split[id];
            }

            float res = 0f;
            if (!float.TryParse(str, out res))
            {
                res = defaultValue;
                Debug.LogError("Unable to parse " + str + " at id " + id + " with separator " + separator);
            }
            return res;
        }

        public static string SetArgs(this string str, params object[] args)
        {
            return String.Format(str, args);
        }

        public static string TrySubstring(this string str, int startId, int length = -1)
        {
            if (length == -1)
            {
                length = str.Length - startId;
            }

            if (startId + length <= str.Length)
            {
                return str.Substring(startId, length);
            }

            length = str.Length - startId;
            if (length <= 0)
            {
                return "";
            }

            return str.Substring(startId, length);
        }

        public static string RemoveWhitespace(this string str)
        {
            return str.Replace(" ","").Replace("\n","").Replace("\r","").Replace("\t",""); 
        }

        public static string GetJsonFromText(this string str)
        {
            int startBracer = str.IndexOf('{');
            int endBracer = str.LastIndexOf('}');
            return str.Substring(startBracer, endBracer - startBracer + 1);
        }

        public static DateTime? TryParseISODateStringToDateTime(this string str, DateTime? defaultVal = null,
                                               string customFormat = null)
        {
            if (str.IsNullOrEmpty())
            {
                return defaultVal;
            }
            DateTime d;
            if (customFormat.IsNotNullOrEmpty())
            {
                if (DateTime.TryParseExact(str, customFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out d))
                {
                    return d.ToUniversalTime();
                }
            }
            if (DateTime.TryParseExact(str, _iso8601Formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out d))
            {
                return d.ToUniversalTime();
            }
            Debug.LogError("Unable to parse date: " + str);
            return defaultVal;
        }

        public static float TryParseToFloat(this string str, float defaultVal = 0f)
        {
            float res = 0;
            if (!float.TryParse(str, out res))
            {
                Debug.LogError("Unable parse to float: " + str);
                res = defaultVal;
            }
            return res;
        }

        public static int TryParseToInt(this string str, int defaultVal = 0)
        {
            int res = 0;
            if (!int.TryParse(str, out res))
            {
                Debug.LogError("Unable parse to int: " + str);
                res = defaultVal;
            }
            return res;
        }

        public static List<int> ParseToIntList(this string str, char splitter)
        {
            string[] split = str.Split(splitter);
            List<int> res = new List<int>(split.Length);
            for(int i = 0; i < split.Length; i++)
            {
                res.Add(split[i].TryParseToInt());
            }

            return res;
        }

        public static List<float> ParseToFloatList(this string str, char splitter)
        {
            string[] split = str.Split(splitter);
            List<float> res = new List<float>(split.Length);
            for(int i = 0; i < split.Length; i++)
            {
                res.Add(split[i].TryParseToFloat());
            }

            return res;
        }

        public static bool TryParseToBool(this string str, bool defaultVal = false)
        {
            bool res = false;
            if (!bool.TryParse(str, out res))
            {
                int intRes = 0;
                if (!int.TryParse(str, out intRes))
                {
                    Debug.LogError("Unable parse to bool: " + str);
                    res = defaultVal;
                }
                else
                {
                    res = intRes != 0;
                }
            }
            return res;
        }

        public static T ParseToEnum<T>(this string str, bool ignoreCase = false)
        {
            return (T)Enum.Parse(typeof(T), str, ignoreCase);
        }

        public static List<String> SplitToList(this string str, char splitter, bool trim = false)
        {
            if(!trim)
            {
                return new List<string>(str.Split(splitter));
            }
            else
            {
                string[] split = str.Split(splitter);
                List<string> res = new List<string>(split.Length);
                for(int i = 0; i < split.Length; i++)
                    res.Add(split[i].Trim());
                return res;
            }
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        #endregion
    }
}