using System;
using System.Collections.Generic;

namespace Stratton.Core
{
    public static class HashUtils
    {
        const int MULT = 57;
        const int MOD = 2085545029;

        private static int HornerHash(string key)
        {
            int hash = 0;
            for(int i = 0; i < key.Length; i++)
            {
                int asciiCode = key[i];
                hash = (hash * MULT + asciiCode) % MOD;
            }

            return hash;
        }

        public static int GetHash(object obj)
        {
            Type type = obj.GetType();

            int hash = -1;
            if(type.Equals(typeof(int)))
            {
                int num = (int)obj;
                string str = num.ToString();

                hash = HornerHash(str);
            }
            if (type.Equals(typeof(long)))
            {
                long num = (long)obj;
                string str = num.ToString();

                hash = HornerHash(str);
            }
            if (type.Equals(typeof(float)))
            {
                float num = (float)obj;
                string str = num.ToString();

                hash = HornerHash(str);
            }
            if (type.Equals(typeof(double)))
            {
                double num = (double)obj;
                string str = num.ToString();

                hash = HornerHash(str);
            }
            if(type.Equals(typeof(string)))
            {
                string str = (string)obj;

                hash = HornerHash(str);
            }

            if(hash == -1)
                UnityEngine.Debug.LogError("Object argument is not valid type");

            return hash;
        }

        public static int GetHash(params object[] args)
        {
            if(args.Length > 0)
            {
                int baseHash = GetHash(args[0]);
                if (baseHash == -1)
                {
                    UnityEngine.Debug.LogError("0 arguments in GetHash(params object[] args)");
                    return -1;
                }

                for (int i = 1; i < args.Length; i++)
                {
                    int hash = GetHash(args[i]);
                    if (hash == -1)
                    {
                        UnityEngine.Debug.LogError("0 arguments in GetHash(params object[] args)");
                        return -1;
                    }
                        

                    baseHash = ConcatHash(baseHash, hash);
                }

                return baseHash;
            }

            UnityEngine.Debug.LogError("0 arguments in GetHash(params object[] args)");
            return -1;
        }

        public static int GetHash(List<object> list, int startIndex, int endIndex)
        {
            int listLength = endIndex - startIndex + 1;

            object obj = list[0];
            if (GetHash(obj) == -1)
            {
                UnityEngine.Debug.LogError("0 arguments in GetHash(List<object>, int startIndex, int endIndex)");
                return -1;
            }

            if (listLength > 1)
            {
                int midIndex = (endIndex - startIndex) / 2;
                int hash_0 = GetHash(list, startIndex, midIndex);
                int hash_1 = GetHash(list, midIndex + 1, endIndex);
                return ConcatHash(hash_0, hash_1);
            }
            else
            {
                return GetHash(list[startIndex]);
            }
        }

        public static int GetHash(Dictionary<object, object> dict)
        {
            int hash = MULT;

            foreach(KeyValuePair<object, object> kvp in dict)
            {
                int hash_0 = GetHash(kvp.Key);
                int hash_1 = GetHash(kvp.Value);

                if (hash_0 == -1 || hash_1 == -1)
                {
                    UnityEngine.Debug.LogError("0 arguments in GetHash(Dictionary<object, object> dict)");
                    return -1;
                }

                hash = ConcatHash(hash, hash_0);
                hash = ConcatHash(hash, hash_1);
            }

            return hash;
        }

        public static int ConcatHash(int hash_0, int hash_1)
        {
            return (hash_0 * MULT + hash_1) % MOD;
        }
    }
}

