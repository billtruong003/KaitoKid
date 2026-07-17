using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Stratton.Core
{
    [Serializable]
    public struct GameStateType : IBaseType
    {
        [SerializeField] [ReadOnly] private string _name;

        public string Name { get => _name; set => _name = value; }
        [JsonIgnore] public bool IsUndefined { get => _name.IsNullOrEmpty(); }

        public GameStateType(string name)
        {
            _name = name;
        }

        public override string ToString()
        {
            return _name;
        }

        public static bool operator ==(GameStateType b1, GameStateType b2)
        {
            return b1.Equals(b2);
        }

        public static bool operator !=(GameStateType b1, GameStateType b2)
        {
            return !(b1 == b2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var b2 = (GameStateType)obj;
            return Name == b2.Name;
        }

        public override int GetHashCode()
        {
            return -1125283371 + EqualityComparer<string>.Default.GetHashCode(_name);
        }
    }
}