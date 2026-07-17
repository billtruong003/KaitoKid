using System;
using System.Collections.Generic;
using System.Linq;

namespace Stratton.Core
{
    public static class DictionaryExtensions
    {
        public static void ClearNullValues<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        {
            List<TKey> keysToRemove = new List<TKey>();
            foreach(var pair in dict)
            {
                if(pair.Value == null)
                    keysToRemove.Add(pair.Key);
            }

            foreach(var key in keysToRemove)
                dict.Remove(key);
        }
	
        public static Dictionary<TKey, TValue> SetIfNotNull<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if(value != null)
                dict[key] = value;
            return dict;
        }
	
        public static Dictionary<TKey, TValue> SetIfCondition<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value, bool condition)
        {
            if(condition)
                dict[key] = value;
            return dict;
        }

        public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dic, Func<TKey, TValue, bool> predicate)
        {
            var keys = dic.Keys.Where(k => predicate(k, dic[k])).ToList();
            foreach (var key in keys)
            {
                dic.Remove(key);
            }
        }
        
        public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dic, Func<TKey, TValue, bool> predicate, Action<TKey> callback)
        {
            var keys = dic.Keys.Where(k => predicate(k, dic[k])).ToList();
            foreach (var key in keys)
            {
                callback?.Invoke(key);
                dic.Remove(key);
            }
        }
    }
}