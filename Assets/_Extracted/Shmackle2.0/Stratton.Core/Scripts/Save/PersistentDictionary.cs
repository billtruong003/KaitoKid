using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Save
{
    public class PersistentDictionary<TKey, TValue> : PersistentValue<Dictionary<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        public PersistentDictionary(SaveSystem saveSystem, string key, SavePattern savePattern, IDictionary<TKey, TValue> initialValues)
            : base(saveSystem, key, savePattern, new Dictionary<TKey, TValue>(initialValues))
        {
        }

        public TValue this[TKey key]
        {
            get => _value[key];
            set
            {
                _value[key] = value;
                switch (_savePattern)
                {
                    case SavePattern.OnValueChange:
                        _saveSystem.Commit(_key, _value);
                        break;
                    case SavePattern.OnInterval:
                        _saveSystem.AddToNextInterval(_key, _value);
                        break;
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            _value.Add(key, value);
            switch (_savePattern)
            {
                case SavePattern.OnValueChange:
                    _saveSystem.Commit(_key, _value);
                    break;
                case SavePattern.OnInterval:
                    _saveSystem.AddToNextInterval(_key, _value);
                    break;
            }
        }

        public bool Remove(TKey key)
        {
            if (_value.Remove(key))
            {
                switch (_savePattern)
                {
                    case SavePattern.OnValueChange:
                        _saveSystem.Commit(_key, _value);
                        break;
                    case SavePattern.OnInterval:
                        _saveSystem.AddToNextInterval(_key, _value);
                        break;
                }
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _value.TryGetValue(key, out value);
        }

        public bool ContainsKey(TKey key)
        {
            return _value.ContainsKey(key);
        }

        public void Clear()
        {
            _value.Clear();
            switch (_savePattern)
            {
                case SavePattern.OnValueChange:
                    _saveSystem.Commit(_key, _value);
                    break;
                case SavePattern.OnInterval:
                    _saveSystem.AddToNextInterval(_key, _value);
                    break;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}


