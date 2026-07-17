using System;
using System.Collections.Generic;
using System.Text;

namespace Stratton.Core
{
    public static class ListExtensions
    {
        #region Public Methods
        
        public static IEnumerable<T> Range<T>(this IList<T> list, int start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return list[i + start];
            }
        }

        public static T At<T>(this IList<T> list, int id, T defaultValue)
        {
            if (list == null || list.Count <= id || id < 0)
            {
                return defaultValue;
            }

            return list[id];
        }

        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static bool IsNotNullOrEmpty<T>(this IList<T> list)
        {
            return list != null && list.Count != 0;
        }

        public static string ToStringWithSeparators<T>(this IList<T> list, string separator)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var val in list)
            {
                sb.Append(val + separator);
            }

            return sb.ToString();
        }

        public static void AddAll<T1, T2>(this IList<T1> target, IList<T2> source, Func<T2, T1> caster)
        {
            int c = source.Count;
            for (int i = 0; i < c; i++)
            {
                target.Add(caster(source[i]));
            }
        }

        public static void RemoveIf<T>(this List<T> list, Predicate<T> checker, bool breakAfterFirst = true)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (checker(list[i]))
                {
                    list.RemoveAt(i);
                    if (breakAfterFirst)
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the last element from list and remove it from that list.
        /// </summary>
        /// <typeparam name="T">Type of elements in list.</typeparam>
        /// <param name="list"></param>
        /// <param name="defaultValue">Default value to return, if list is null or empty.</param>
        /// <returns>Last element from list.</returns>
        public static T PopLast<T>(this List<T> list, T defaultValue = default(T))
        {
            if (list.IsNullOrEmpty()) return defaultValue;

            T retValue = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);

            return retValue;
        }

        #endregion
    }
}