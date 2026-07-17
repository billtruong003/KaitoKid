using System;
using System.Collections.Generic;
using System.Linq;

namespace Stratton.Core
{
    public static class EnumerableExtensions
    {
        #region Fields

        private static readonly Random RandomPool = new Random();

        #endregion

        #region Public Methods

        public static bool IsNullOrEmptyEnumerable<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                return true;
            }

            return !source.Any();
        }

        /// <summary>
        ///     Get random element from collection.
        /// </summary>
        /// <typeparam name="T">Type of elements in collection.</typeparam>
        /// <param name="collection">Source collection to get element from.</param>
        /// <returns>Random element from collection. Default value if collection is empty.</returns>
        public static T GetRandom<T>(this IEnumerable<T> collection)
        {
            var count = collection.Count();

            if (count == 0)
                return default(T);

            return collection.ElementAt(UnityEngine.Random.Range(0, count));
        }

        /// <summary>
        ///     Returns new IEnumerable with randomized elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(item => RandomPool.Next());
        }

        /// <summary>
        ///     Returns new List with randomized elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static List<T> RandomizeToList<T>(this IEnumerable<T> source)
        {
            return source.Randomize().ToList();
        }

        /// <summary>
        ///     Get next element in the array, according to a specified one.
        /// </summary>
        /// <typeparam name="T">Type of elements in the array.</typeparam>
        /// <param name="array">Source array to get next element from.</param>
        /// <param name="current">Next element to that element will be returned.</param>
        /// <param name="loop">
        ///     Indicates, whether loop enumeration. When set to TRUE, then the first element of the array will be
        ///     returned, if "current" is the last element. Otherwise will return default value.
        /// </param>
        /// <returns>
        ///     Returns next element in the array, according to a "current". Returns default if there is no more elements and
        ///     loop is disabled.
        /// </returns>
        public static T GetNext<T>(this T[] array, T current, bool loop = false)
        {
            if (array == null || array.Length == 0)
                return current;

            if (current == null)
                return array[0];

            var index = Array.IndexOf(array, current);

            index++;

            if (index >= array.Length)
            {
                if (loop)
                {
                    index = 0;
                }
                else
                {
                    return default(T);
                }
            }

            return array[index];
        }

        /// <summary>
        ///     Get next element in the list, according to a specified one.
        /// </summary>
        /// <typeparam name="T">Type of elements in the list.</typeparam>
        /// <param name="list">Source list to get next element from.</param>
        /// <param name="current">Next element to that element will be returned.</param>
        /// <param name="loop">
        ///     Indicates, whether loop enumeration. When set to TRUE, then the first element of the list will be
        ///     returned, if "current" is the last element. Otherwise will return default value.
        /// </param>
        /// <returns>
        ///     Returns next element in the list, according to a "current". Returns default if there is no more elements and
        ///     loop is disabled.
        /// </returns>
        public static T GetNext<T>(this List<T> list, T current, bool loop = false)
        {
            if (list == null || list.Count == 0)
                return current;

            if (current == null)
                return list[0];

            var index = list.IndexOf(current);

            index++;

            if (index >= list.Count)
            {
                if (loop)
                {
                    index = 0;
                }
                else
                {
                    return default(T);
                }
            }

            return list[index];
        }

        /// <summary>
        ///     Get previous element in the array, according to a specified one.
        /// </summary>
        /// <typeparam name="T">Type of elements in the array.</typeparam>
        /// <param name="array">Source array to get previous element from.</param>
        /// <param name="current">Previous element to that element will be returned.</param>
        /// <param name="loop">
        ///     Indicates, whether loop enumeration. When set to TRUE, then the last element of the array will be
        ///     returned, if "current" is the first element. Otherwise will return default value.
        /// </param>
        /// <returns>
        ///     Returns previous element in the array, according to a "current". Returns default if there is no more elements
        ///     and loop is disabled.
        /// </returns>
        public static T GetPrev<T>(this T[] array, T current, bool loop = false)
        {
            if (array == null || array.Length == 0)
                return current;

            if (current == null)
                return array[0];

            var index = Array.IndexOf(array, current);

            index--;

            if (index < 0)
            {
                if (loop)
                {
                    index = array.Length - 1;
                }
                else
                {
                    return default(T);
                }
            }

            return array[index];
        }

        /// <summary>
        ///     Get previous element in the list, according to a specified one.
        /// </summary>
        /// <typeparam name="T">Type of elements in the list.</typeparam>
        /// <param name="list">Source list to get previous element from.</param>
        /// <param name="current">Previous element to that element will be returned.</param>
        /// <param name="loop">
        ///     Indicates, whether loop enumeration. When set to TRUE, then the last element of the list will be
        ///     returned, if "current" is the first element. Otherwise will return default value.
        /// </param>
        /// <returns>
        ///     Returns previous element in the list, according to a "current". Returns default if there is no more elements
        ///     and loop is disabled.
        /// </returns>
        public static T GetPrev<T>(this List<T> list, T current, bool loop = false)
        {
            if (list == null || list.Count == 0)
                return current;

            if (current == null)
                return list[0];

            var index = list.IndexOf(current);

            index--;

            if (index < 0)
            {
                if (loop)
                {
                    index = list.Count - 1;
                }
                else
                {
                    return default(T);
                }
            }

            return list[index];
        }

        /// <summary>
        ///     Perform the <paramref name="action" /> on each item in the list.
        /// </summary>
        /// <typeparam name="T">Type of elements in collection.</typeparam>
        /// <param name="collection">Collection to iterate through.</param>
        /// <param name="action">Action to perform on each element in collection.</param>
        /// <returns></returns>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            foreach (var e in collection)
            {
                action.Invoke(e);
            }

            collection.GetEnumerator().Reset();

            return collection;
        }

        #endregion
    }
}