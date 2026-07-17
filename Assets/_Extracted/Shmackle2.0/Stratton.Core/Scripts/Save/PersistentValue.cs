using System;

namespace Stratton.Save
{
    public class PersistentValue<T> : IDisposable
    {
        protected T _value;
        protected readonly string _key;
        protected readonly SaveSystem _saveSystem;
        protected readonly SavePattern _savePattern;

        public string Key => _key;

        public PersistentValue(SaveSystem saveSystem, string key, SavePattern savePattern, T baseValue)
        {
            _saveSystem = saveSystem;
            _key = key;
            _savePattern = savePattern;
            Load(baseValue);
        }

        public T Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
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

        public virtual void Load(T baseValue)
        {
            if (_saveSystem.TryGet(_key, out T value))
            {
                _value = value;
            }
            else
            {
                _value = baseValue;
            }
        }

        public virtual void Save()
        {
            _saveSystem.Commit(_key, _value);
        }

        public virtual void Remove()
        {
            _saveSystem.DeleteKey(_key);
            Dispose();
        }

        public virtual void Dispose()
        {
            if (_savePattern == SavePattern.OnInterval)
            {
                _saveSystem.RemoveFromInterval(_key);
            }
            GC.SuppressFinalize(this);
        }

        ~PersistentValue()
        {
            Dispose();
        }
    }
}


