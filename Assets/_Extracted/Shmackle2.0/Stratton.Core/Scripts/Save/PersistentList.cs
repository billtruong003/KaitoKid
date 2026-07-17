using System.Collections;
using System.Collections.Generic;

namespace Stratton.Save
{
    public class PersistentList<T> : PersistentValue<List<T>>, IEnumerable<T>
    {
        public PersistentList(SaveSystem saveSystem, string key, SavePattern savePatern, IEnumerable<T> initialValues) 
            : base(saveSystem, key, savePatern, new List<T>(initialValues))
        {
        }

        public void Add(T value)
        {
            _value.Add(value);
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

        public void Remove(T item)
        {
            if (_value.Remove(item))
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
            }
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _value.Count)
            {
                _value.RemoveAt(index);
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

        public T this[int index]
        {
            get => _value[index];
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value[index], value))
                {
                    _value[index] = value;
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
        }

        public int Count => _value.Count;

        public IEnumerator<T> GetEnumerator() => _value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}


